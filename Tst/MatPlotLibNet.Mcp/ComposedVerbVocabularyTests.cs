// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A composed-chart verb (<c>Dumbbell</c>, <c>Pareto</c>, <c>Ridgeline</c> ...) is a call that adds several existing
/// series to an axes; the JSON a figure writes carries those series, never the verb. So no verb is a chart type: the list
/// the server serves and the vocabulary it checks a spec against name none of them, and a spec that asks for one by name is
/// refused like any misspelt type. This reads the verbs off the public API, so the next verb joins the check by itself and
/// a verb that was ever registered as a series type (which would advertise a chart no spec carries) turns it red.</summary>
public class ComposedVerbVocabularyTests
{
    private static readonly Type[] Families =
        [typeof(CategoricalAxesExtensions), typeof(DistributionAxesExtensions), typeof(XYAxesExtensions), typeof(GridAxesExtensions)];

    /// <summary>The name of every verb: a public extension method of one of the four family classes.</summary>
    private static readonly string[] Verbs =
        [.. Families
            .SelectMany(family => family.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false))
            .Select(method => method.Name)
            .Distinct()
            .Order(StringComparer.Ordinal)];

    /// <summary>The verb names as a Theory carries them.</summary>
    public static TheoryData<string> VerbNames { get; } = new(Verbs);

    /// <summary>How the three spellings of one name compare: <c>QQPlot</c>, <c>qq_plot</c> and <c>qqplot</c> are one name.</summary>
    private static string Key(string name) => new([.. name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

    private static readonly HashSet<string> VerbKeys = [.. Verbs.Select(Key)];

    /// <summary>Liveness: the reading finds the verbs the pins are about, so they cannot go green by finding nothing.</summary>
    [Fact]
    public void TheVerbsAreFound()
    {
        Assert.Contains("Lollipop", Verbs);
        Assert.Contains("Dumbbell", Verbs);
        Assert.Contains("Ridgeline", Verbs);
        Assert.Contains("QQPlot", Verbs);
        Assert.Contains("StreamGraph", Verbs);
        Assert.Contains("CalendarHeatmap", Verbs);
        Assert.Contains("Spy", Verbs);
        Assert.True(Verbs.Length >= 15, $"expected the fifteen composed verbs, found {Verbs.Length}: {string.Join(", ", Verbs)}");
    }

    [Fact]
    public void TheChartTypesTheServerLists_AreNoVerb()
    {
        var catalog = new ChartTypeCatalog();

        var verbsListed = catalog.Listed.Concat(catalog.Excluded.Keys).Where(type => VerbKeys.Contains(Key(type))).ToArray();

        Assert.True(verbsListed.Length == 0, "The chart type list names a verb, which no spec carries: " + string.Join(", ", verbsListed));
    }

    [Fact]
    public void TheListTheServerServes_IsNoVerb()
    {
        var catalog = new ChartTypeCatalog();
        var reader = new ChartSpecReader(catalog, RenderLimits.Default);
        using var output = new TempDirectory();
        var tools = new ChartTools(new ChartRendering(reader, new ChartSummarizer()), new ChartTabulation(reader, RenderLimits.Default),
            catalog, new ChartSchemaDescription(catalog), new OutputPathResolver(output.Path));

        var lines = tools.ListChartTypes().Split('\n').Select(line => line.Trim().Split(' ')[0]).Where(word => word.Length > 0);

        var verbsServed = lines.Where(word => VerbKeys.Contains(Key(word))).ToArray();
        Assert.True(verbsServed.Length == 0, "list_chart_types names a verb: " + string.Join(", ", verbsServed));
    }

    /// <summary>The vocabulary's fields and the values its enum-valued fields accept, every node kind alike.</summary>
    [Fact]
    public void TheVocabulary_NamesNoVerbAsAFieldOrAValue()
    {
        var rules = (IReadOnlyDictionary<(SpecNodeKind Kind, string Field), SpecVocabulary.Rule>)
            typeof(SpecVocabulary).GetField("Rules", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

        Assert.NotEmpty(rules);
        var named = rules
            .SelectMany(pair => pair.Value.Accepted.Prepend(pair.Key.Field).Select(word => (Where: $"{pair.Key.Kind}.{pair.Key.Field}", Word: word)))
            .Where(entry => VerbKeys.Contains(Key(entry.Word)))
            .Select(entry => $"{entry.Where} = {entry.Word}")
            .ToArray();

        Assert.True(named.Length == 0, "The spec vocabulary names a verb: " + string.Join(", ", named));
    }

    [Theory]
    [MemberData(nameof(VerbNames))]
    public void ASpecThatNamesAVerbAsASeriesType_IsRefused(string verb)
    {
        var reader = new ChartSpecReader(new ChartTypeCatalog(), RenderLimits.Default);
        string spec = $$"""{"width":400,"height":300,"subPlots":[{"series":[{"type":"{{verb.ToLowerInvariant()}}","xData":[1],"yData":[1]}]}]}""";

        var refusal = Assert.ThrowsAny<Exception>(() => reader.Read(new ChartSpec(spec)));

        Assert.Contains("is not a chart type this server lists", refusal.Message);
    }

    /// <summary>A temporary directory, deleted with the test.</summary>
    private sealed class TempDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("mcp-verb-vocabulary");

        public string Path => _directory.FullName;

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
