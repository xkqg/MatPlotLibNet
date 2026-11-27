// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>One verb says one thing at every layer that carries it: the axes verb, the verb of the axes builder, of the figure
/// builder, of a slot builder and of the quick-plot entry point all take the same optional argument at the same default, and where
/// the series the verb makes owns that default (a <c>Resting</c> constant of the series: <c>HistogramSeries.RestingBins</c>) each
/// layer reads the series' constant instead of typing the number again. A layer that types the default again drifts from the series
/// the day one of them changes, and a call through one layer then draws what a call through another does not. The defaults are read
/// from the public surface (compiled in as the constants they are), the spelling from the source.</summary>
public class VerbLayerParityContractTests
{
    /// <summary>One optional argument of one verb at one layer.</summary>
    private readonly record struct Default(string Verb, string Parameter, string Layer, object? Value);

    /// <summary>An optional argument of a verb whose series owns its default: the verb, the argument, the series type and the constant.</summary>
    private readonly record struct Owned(string Verb, string Parameter, Type Series, FieldInfo Constant);

    private static readonly Type[] BuilderLayers =
        [typeof(AxesBuilder), typeof(FigureBuilder), typeof(YAxisSlotBuilder), typeof(XAxisSlotBuilder), typeof(SecondaryXAxisBuilder)];

    private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    /// <summary>Every method of the public surface that carries a verb, with the layer it stands at.</summary>
    private static IReadOnlyList<(MethodBase Method, string Layer, int Skip)> Methods()
    {
        var core = typeof(Axes).Assembly;
        var layers = new List<(MethodBase, string, int)>();

        // The axes verbs: extension blocks on Axes, in the series folders.
        foreach (var method in core.GetExportedTypes().Where(type => type.IsAbstract && type.IsSealed)
                     .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                     .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false) && method.GetParameters()[0].ParameterType == typeof(Axes)))
        {
            layers.Add((method, "Axes", 1));
        }

        // The builders: every call a caller writes on them (their own methods and the extension members the library adds), and the
        // verbs of the composed-chart families that extend the axes builder.
        foreach (var builder in BuilderLayers)
        {
            layers.AddRange(builder.Calls().Select(call => ((MethodBase)call.Method, builder.Name, call.IsExtension ? 1 : 0)));
        }

        layers.AddRange(VerbFamilies.AllVerbs.Select(m => ((MethodBase)m, "AxesBuilder extension", 1)));
        layers.AddRange(typeof(QuickPlot).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Select(m => ((MethodBase)m, nameof(QuickPlot), 0)));

        // The figure factories that stand beside the builders: a template (FigureTemplates) and a list of records' verbs (data.Hist(...)).
        // A verb of the figure builder itself (an extension member on it) is its layer above, not a factory.
        layers.AddRange(core.GetExportedTypes().Where(type => type.IsAbstract && type.IsSealed)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.ReturnType == typeof(FigureBuilder) && !(method.IsDefined(typeof(ExtensionAttribute), inherit: false)
                && method.GetParameters()[0].ParameterType is var receiver && (receiver == typeof(Axes) || receiver == typeof(FigureBuilder))))
            .Select(method => ((MethodBase)method, method.DeclaringType!.Name, method.IsDefined(typeof(ExtensionAttribute), inherit: false) ? 1 : 0)));
        return layers;
    }

    /// <summary>Every optional argument with a default, over every layer of the public surface that carries a verb.</summary>
    private static IReadOnlyList<Default> Surface() =>
    [
        .. Methods().SelectMany(entry => entry.Method.GetParameters().Skip(entry.Skip)
            .Where(parameter => parameter.HasDefaultValue && parameter.DefaultValue is not null && parameter.DefaultValue is not DBNull)
            .Select(parameter => new Default(entry.Method.Name, parameter.Name!, entry.Layer, parameter.DefaultValue))),
    ];

    private static object Normal(object? value) => value switch
    {
        null => "null",
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        Enum e => e.ToString(),
        _ => value,
    };

    /// <summary>The optional arguments that two layers carry at two different defaults, one line each.</summary>
    private static IReadOnlyList<string> Drifts(IEnumerable<Default> defaults) =>
    [
        .. defaults
            .GroupBy(d => (Verb: d.Verb.ToLowerInvariant(), d.Parameter))
            .Select(group => (group.Key, Values: group.GroupBy(d => Normal(d.Value)).ToList()))
            .Where(g => g.Values.Count > 1)
            .Select(g => $"{g.Key.Verb}({g.Key.Parameter}): " + string.Join(" | ", g.Values.Select(v => $"{v.Key} at {string.Join(", ", v.Select(d => d.Layer).Distinct())}")))
            .OrderBy(line => line, StringComparer.Ordinal),
    ];

    /// <summary>The series types a verb makes, by the verb's name: the type a layer returns when it is a series.</summary>
    private static Dictionary<string, HashSet<Type>> SeriesOfVerb() =>
        Methods().Where(entry => entry.Method is MethodInfo { ReturnType: var type } && typeof(ChartSeries).IsAssignableFrom(type))
            .GroupBy(entry => entry.Method.Name.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.Select(entry => ((MethodInfo)entry.Method).ReturnType).ToHashSet());

    /// <summary>Every optional argument whose series states its default as a <c>Resting</c> constant named after the argument, over
    /// every verb the series is made by.</summary>
    private static IReadOnlyList<Owned> OwnedDefaults()
    {
        var seriesOf = SeriesOfVerb();
        return
        [
            .. Surface().Select(d => (d.Verb, d.Parameter)).Distinct()
                .Where(pair => seriesOf.ContainsKey(pair.Verb.ToLowerInvariant()))
                .SelectMany(pair => seriesOf[pair.Verb.ToLowerInvariant()]
                    .Select(series => (pair, series, field: series.GetField("Resting" + char.ToUpperInvariant(pair.Parameter[0]) + pair.Parameter[1..], AnyStatic)))
                    .Where(found => found.field is { IsLiteral: true })
                    .Select(found => new Owned(found.pair.Verb, found.pair.Parameter, found.series, found.field!))),
        ];
    }

    [Fact]
    public void TheLayersOfAVerb_TakeEachOptionalArgumentAtOneDefault()
    {
        var drifts = Drifts(Surface());

        Assert.True(drifts.Count == 0, "A verb takes one optional argument at two different defaults:\n" + string.Join('\n', drifts));
    }

    [Fact]
    public void ADefaultAVerbsSeriesOwns_IsTheSeriesOwnValueAtEveryLayer()
    {
        var owned = OwnedDefaults();
        var apart = owned.SelectMany(o => Surface().Where(d => d.Verb == o.Verb && d.Parameter == o.Parameter)
                .Where(d => !Equals(Normal(d.Value), Normal(o.Constant.GetRawConstantValue())))
                .Select(d => $"{d.Layer}.{d.Verb}({d.Parameter}) = {d.Value}, {o.Series.Name}.{o.Constant.Name} = {o.Constant.GetRawConstantValue()}"))
            .Distinct().ToList();

        Assert.True(apart.Count == 0, "A layer's default differs from the one its series owns:\n" + string.Join('\n', apart));
    }

    [Fact]
    public void ADefaultAVerbsSeriesOwns_IsReadFromTheSeriesAtEveryLayerThatSpellsIt()
    {
        var owned = OwnedDefaults();
        var found = new List<string>();
        foreach (var file in RepositoryTree.SourceFiles().Where(f => f.Path.StartsWith("Src/MatPlotLibNet", StringComparison.Ordinal)))
        {
            foreach (var o in owned)
            {
                foreach (Match call in Regex.Matches(file.Text, $@"\b{o.Verb}\s*\((?<args>[^)]*)\)", RegexOptions.CultureInvariant))
                {
                    var argument = Regex.Match(call.Groups["args"].Value, $@"\b{o.Parameter}\s*=\s*(?<value>[^,)]+)");
                    if (argument.Success && !Regex.IsMatch(argument.Groups["value"].Value.Trim(), @"^(?:\w+\.)*Resting\w+$"))
                    {
                        found.Add($"{file.Path}: {o.Verb}({o.Parameter} = {argument.Groups["value"].Value.Trim()}) types the default again; {o.Series.Name}.{o.Constant.Name} owns it");
                    }
                }
            }
        }

        Assert.True(found.Count == 0, string.Join('\n', found.Distinct()));
    }

    /// <summary>One optional argument of a figure template that a figure class of the same name takes as a setting: the template, the
    /// argument, the class and the setting the argument becomes.</summary>
    private readonly record struct TemplateSetting(MethodInfo Template, ParameterInfo Parameter, Type Figure, PropertyInfo Setting);

    /// <summary>Every optional argument of a <c>FigureTemplates</c> verb that has a figure class (<c>JointPlot</c> and <c>JointPlotFigure</c>)
    /// and a setting of that class it becomes: the argument's own name, or <c>Max</c> before it (<c>cols</c> is <c>MaxCols</c>).</summary>
    private static IReadOnlyList<TemplateSetting> TemplateSettings()
    {
        var figures = typeof(Axes).Assembly.ConcretePublicSubtypesOf(typeof(FacetedFigure))
            .ToDictionary(type => type.Name, StringComparer.Ordinal);
        var found = new List<TemplateSetting>();
        foreach (var template in typeof(FigureTemplates).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (!figures.TryGetValue(template.Name + "Figure", out var figure))
            {
                continue;
            }

            foreach (var parameter in template.GetParameters().Where(p => p.HasDefaultValue && p.DefaultValue is not null))
            {
                var setting = SettingOf(figure, parameter.Name!);
                if (setting is not null)
                {
                    found.Add(new TemplateSetting(template, parameter, figure, setting));
                }
            }
        }

        return found;
    }

    private static PropertyInfo? SettingOf(Type figure, string parameter) =>
        figure.GetProperty(parameter, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
        ?? figure.GetProperty("Max" + parameter, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    /// <summary>A figure class made over empty data, so its settings read as they start.</summary>
    private static object Fresh(Type figure)
    {
        var constructor = figure.GetConstructors().Single();
        return constructor.Invoke([.. constructor.GetParameters().Select(p => p.ParameterType.IsArray ? Array.CreateInstance(p.ParameterType.GetElementType()!, new int[p.ParameterType.GetArrayRank()]) : null)]);
    }

    [Fact]
    public void ATemplateAndItsFigureClass_TakeEachSettingAtOneDefault()
    {
        var settings = TemplateSettings();
        var apart = settings
            .Where(s => !Equals(Normal(s.Parameter.DefaultValue), Normal(s.Setting.GetValue(Fresh(s.Figure)))))
            .Select(s => $"FigureTemplates.{s.Template.Name}({s.Parameter.Name}) = {s.Parameter.DefaultValue}, {s.Figure.Name}.{s.Setting.Name} starts at {s.Setting.GetValue(Fresh(s.Figure))}")
            .ToList();

        Assert.Contains(settings, s => s.Template.Name == "JointPlot" && s.Setting.Name == "Bins");
        Assert.Contains(settings, s => s.Template.Name == "PairPlot" && s.Setting.Name == "Bins");
        Assert.Contains(settings, s => s.Template.Name == "FacetGrid" && s.Setting.Name == "MaxCols");
        Assert.True(apart.Count == 0, "A template takes a setting at another default than its figure class starts it at:\n" + string.Join('\n', apart));
    }

    [Fact]
    public void ATemplateSetting_IsTheFigureClassOwnConstantAtBothLayers()
    {
        var files = RepositoryTree.SourceFiles().ToDictionary(f => f.Path, f => f.Text, StringComparer.Ordinal);
        var found = new List<string>();
        foreach (var s in TemplateSettings())
        {
            var constant = s.Figure.GetField("Resting" + s.Setting.Name, AnyStatic);
            if (constant is not { IsLiteral: true })
            {
                found.Add($"{s.Figure.Name} holds no Resting{s.Setting.Name} constant for {s.Setting.Name}, which FigureTemplates.{s.Template.Name}({s.Parameter.Name}) also takes");
                continue;
            }

            string figureText = files[$"Src/MatPlotLibNet/Builders/{s.Figure.Name}.cs"];
            var start = Regex.Match(figureText, $@"\b{s.Setting.Name}\s*\{{[^}}]*\}}\s*=\s*(?<value>[^;]+);");
            if (!start.Success || !Regex.IsMatch(start.Groups["value"].Value.Trim(), @"^(?:\w+\.)*Resting\w+$"))
            {
                found.Add($"{s.Figure.Name}.{s.Setting.Name} starts at {start.Groups["value"].Value.Trim()}, not at {constant.Name}");
            }

            var call = Regex.Match(files["Src/MatPlotLibNet/FigureTemplates.cs"], $@"\b{s.Template.Name}\((?<args>[^)]*)\)");
            var argument = Regex.Match(call.Groups["args"].Value, $@"\b{s.Parameter.Name}\s*=\s*(?<value>[^,)]+)");
            if (!argument.Success || !Regex.IsMatch(argument.Groups["value"].Value.Trim(), @"^(?:\w+\.)*Resting\w+$"))
            {
                found.Add($"FigureTemplates.{s.Template.Name}({s.Parameter.Name} = {argument.Groups["value"].Value.Trim()}) types the default again; {s.Figure.Name}.{constant.Name} owns it");
            }
        }

        Assert.True(found.Count == 0, string.Join('\n', found));
    }

    /// <summary>The verb a method is, with the axis it names left out: <c>SetXDateFormat</c> and <c>SetYDateFormat</c> are one verb
    /// over two axes (<c>Set_DateFormat</c>), so a default they both take is one default typed twice when it is typed.</summary>
    private static string TwinKey(string verb) => Regex.Replace(verb, "(?<=[a-z])[XYZ](?=[A-Z]|$)", "_").ToLowerInvariant();

    /// <summary>A default that says nothing a constant could own: no value, the zero of its type, or a switch's own "on".</summary>
    private static bool SaysNothing(object? value) => value is null or false or true || (value is IConvertible c && value is not string && value is not Enum
        && Convert.ToDouble(c, CultureInfo.InvariantCulture) == 0);

    /// <summary>Every optional argument two or more declarations take (the layers of one verb, or the twin verbs of two axes) at a
    /// default that says something, by its key.</summary>
    private static IReadOnlyList<IGrouping<(string Verb, string Parameter), (string Name, string Layer, string Parameter, object? Value)>> SharedDefaults() =>
    [
        .. Methods().SelectMany(entry => entry.Method.GetParameters().Skip(entry.Skip)
                .Where(p => p.HasDefaultValue && p.DefaultValue is not DBNull && !SaysNothing(p.DefaultValue))
                .Select(p => (Name: entry.Method.Name, entry.Layer, Parameter: p.Name!, Value: p.DefaultValue)))
            .GroupBy(d => (TwinKey(d.Name), d.Parameter))
            .Where(group => group.Select(d => (d.Name, d.Layer)).Distinct().Count() > 1),
    ];

    /// <summary>The source spellings of the default of <paramref name="parameter"/> in every declaration of <paramref name="verb"/>
    /// under <c>Src</c> that is not a named constant (<c>X.RestingY</c>, <c>X.DefaultY</c>).</summary>
    private static IEnumerable<string> TypedAgain(string verb, string parameter) =>
        RepositoryTree.SourceFiles().Where(f => f.Path.StartsWith("Src/", StringComparison.Ordinal))
            .SelectMany(file => Regex.Matches(file.Text, $@"\b{verb}\s*\((?<args>[^)]*)\)", RegexOptions.CultureInvariant)
                .Select(call => Regex.Match(call.Groups["args"].Value, $@"\b{parameter}\s*=\s*(?<value>[^,)]+)"))
                .Where(argument => argument.Success && !Regex.IsMatch(argument.Groups["value"].Value.Trim(), @"^(?:\w+\.)*(?:Resting|Default)\w*$"))
                .Select(argument => $"{file.Path}: {verb}({parameter} = {argument.Groups["value"].Value.Trim()})"));

    [Fact]
    public void ADefaultTwoDeclarationsTake_IsOneNamedConstant_ReadByEach()
    {
        var found = SharedDefaults()
            .SelectMany(group => group.Select(d => d.Name).Distinct().SelectMany(name => TypedAgain(name, group.Key.Parameter)))
            .Distinct().OrderBy(line => line, StringComparer.Ordinal).ToList();

        Assert.True(found.Count == 0, "A default two declarations take is typed again; name it once (a Resting or Default constant of its owner) and read it at each:\n" + string.Join('\n', found));
    }

    [Fact]
    public void TheSharedDefaults_IncludeTheTwinVerbsOfTwoAxes_AndTheLayersOfOneVerb()
    {
        var keys = SharedDefaults().Select(group => group.Key).ToList();

        Assert.Contains(("set_dateformat", "format"), keys);
        Assert.Contains(("withsymlog_scale", "linthresh"), keys);
        Assert.Contains(("withlighting", "ambient"), keys);
        Assert.Contains(("hist", "bins"), keys);
        Assert.DoesNotContain(keys, key => key.Parameter is "enabled" or "visible");
    }

    [Theory]
    [InlineData("SetXDateFormat", "set_dateformat")]
    [InlineData("WithSymlogYScale", "withsymlog_scale")]
    [InlineData("WithXBreak", "with_break")]
    [InlineData("SetZLim", "set_lim")]
    [InlineData("Xcorr", "xcorr")]
    [InlineData("Hexbin", "hexbin")]
    [InlineData("StreamingPlot", "streamingplot")]
    public void TheTwinKey_LeavesOutTheAxisAVerbNames_AndNothingElse(string verb, string key) => Assert.Equal(key, TwinKey(verb));

    [Fact]
    public void TheScan_FindsTheLayersItIsMeantToCompare()
    {
        var surface = Surface();

        Assert.Contains(surface, d => d.Verb == "Hist" && d.Layer == "Axes");
        Assert.Contains(surface, d => d.Verb == "Hist" && d.Layer == nameof(AxesBuilder));
        Assert.Contains(surface, d => d.Verb == "Hist" && d.Layer == nameof(FigureBuilder));
        Assert.Contains(surface, d => d.Verb == "Hist" && d.Layer == nameof(QuickPlot));
        Assert.Contains(surface, d => d.Verb == "Hist" && d.Layer == "EnumerableFigureExtensions");
        Assert.Contains(surface, d => d.Verb == "StreamingPlot" && d.Layer == nameof(YAxisSlotBuilder));
    }

    [Fact]
    public void TheSeriesOwnedDefaults_IncludeTheOnesTheVerbsOfTheStreamingAndSignalSeriesTake()
    {
        var owned = OwnedDefaults();

        Assert.Contains(owned, o => o is { Verb: "Hist", Parameter: "bins" } && o.Series == typeof(HistogramSeries));
        Assert.Contains(owned, o => o is { Verb: "Signal", Parameter: "sampleRate" } && o.Series == typeof(SignalSeries));
        Assert.Contains(owned, o => o is { Verb: "StreamingPlot", Parameter: "capacity" } && o.Series == typeof(StreamingLineSeries));
        Assert.Contains(owned, o => o is { Verb: "StreamingCandlestick", Parameter: "capacity" } && o.Series == typeof(StreamingCandlestickSeries));
    }

    [Fact]
    public void ADefaultThatTwoLayersCarryApart_IsFound()
    {
        var drifts = Drifts(
        [
            new("Hist", "bins", "A", 10), new("Hist", "bins", "B", 30),
            new("Line", "width", "A", 1.5f), new("Line", "width", "B", 1.5),
            new("Plot", "n", "A", 3), new("plot", "n", "B", 3L),
        ]);

        var drift = Assert.Single(drifts);
        Assert.StartsWith("hist(bins):", drift, StringComparison.Ordinal);
    }
}
