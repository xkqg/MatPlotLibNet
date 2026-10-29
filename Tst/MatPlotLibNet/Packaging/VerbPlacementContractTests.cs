// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>Where a verb lives is a rule written in two documents (ARCHITECTURE.md says what the families are and which
/// verbs each holds, CONTRIBUTING.md says how a new verb is added and tested), so a document that names a verb the code
/// does not have, or leaves out one it has, is a claim nobody would notice drifting. This reads the public API and holds
/// both documents to it, and holds every verb to the shape the rule gives: an extension method on the axes builder, in the
/// library's namespace, with a budget of parameters and a callback that comes last and may be left out.</summary>
public class VerbPlacementContractTests
{
    /// <summary>The number of parameters a verb may take besides the builder it extends. The documents state the same number,
    /// and a test below holds them to it.</summary>
    private const int ParameterBudget = 4;

    private const string LibraryNamespace = "MatPlotLibNet";

    private static readonly string Root = RepoRoot();

    private static readonly Type[] Families =
        [typeof(CategoricalAxesExtensions), typeof(DistributionAxesExtensions), typeof(XYAxesExtensions), typeof(GridAxesExtensions)];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static string Architecture => File.ReadAllText(Path.Combine(Root, "Src", "MatPlotLibNet", "ARCHITECTURE.md"));

    private static string Contributing => File.ReadAllText(Path.Combine(Root, "CONTRIBUTING.md"));

    /// <summary>The names of the public extension methods a family class declares: its verbs.</summary>
    private static MethodInfo[] VerbsOf(Type family) =>
        [.. family.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false))
            .OrderBy(method => method.Name, StringComparer.Ordinal)];

    private static MethodInfo[] AllVerbs => [.. Families.SelectMany(VerbsOf)];

    /// <summary>The types the verbs take or hand to their callbacks that belong to this library and are not series: the named
    /// data (a group, a sample, the limits of a chart) and the parts of a chart. Found through the parameters, so a type a
    /// verb starts taking tomorrow is found without an edit here.</summary>
    private static Type[] TypesTheVerbsIntroduce()
    {
        static IEnumerable<Type> Unwrapped(Type type)
        {
            yield return type;
            if (type.IsArray)
            {
                foreach (var inner in Unwrapped(type.GetElementType()!)) yield return inner;
            }

            foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
            {
                foreach (var inner in Unwrapped(argument)) yield return inner;
            }
        }

        return
        [.. AllVerbs
            .SelectMany(verb => verb.GetParameters().Skip(1))
            .SelectMany(parameter => Unwrapped(parameter.ParameterType))
            .Where(type => type.IsValueType && !type.IsEnum && type.Assembly == typeof(AxesBuilder).Assembly)
            .Distinct()
            .OrderBy(type => type.Name, StringComparer.Ordinal)];
    }

    public static TheoryData<string> FamilyNames { get; } = new(Families.Select(family => family.Name));

    public static TheoryData<string> VerbNames { get; } = new(AllVerbs.Select(verb => verb.Name));

    // ---- the documents ---------------------------------------------------------------------------------------------

    /// <summary>The text of a "## heading" section of a Markdown document, up to the next "## " heading.</summary>
    private static string SectionOf(string markdown, string heading)
    {
        int start = markdown.IndexOf($"\n## {heading}", StringComparison.Ordinal);
        Assert.True(start >= 0, $"The document has no section \"## {heading}\".");
        int next = markdown.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        return next < 0 ? markdown[start..] : markdown[start..next];
    }

    /// <summary>The text of one bullet of a section, wrapped lines joined: from the line that opens with
    /// <paramref name="opening"/> to the line before the next bullet or the first line that is not indented.</summary>
    private static string BulletOf(string section, string opening)
    {
        var lines = section.Split('\n');
        int first = Array.FindIndex(lines, line => line.TrimStart().StartsWith("- " + opening, StringComparison.Ordinal));
        Assert.True(first >= 0, $"No bullet opens with \"{opening}\".");

        var text = new List<string> { lines[first].Trim() };
        for (int i = first + 1; i < lines.Length && lines[i].StartsWith("  ", StringComparison.Ordinal) && !lines[i].TrimStart().StartsWith("- ", StringComparison.Ordinal); i++)
        {
            text.Add(lines[i].Trim());
        }

        return string.Join(' ', text);
    }

    private static string[] CodeSpansAfter(string text, string label)
    {
        int at = text.IndexOf(label, StringComparison.Ordinal);
        Assert.True(at >= 0, $"The text carries no \"{label}\" list: {text}");
        return [.. Regex.Matches(text[(at + label.Length)..], "`([A-Za-z0-9_]+)`").Select(match => match.Groups[1].Value)];
    }

    private static string Show(IEnumerable<string> names) => names.Any() ? string.Join(", ", names) : "(none)";

    [Fact]
    public void TheFourFamiliesAndTheirVerbs_AreFound()
    {
        Assert.Equal(4, Families.Length);
        Assert.Equal(20, AllVerbs.Length);
        Assert.Equal(13, TypesTheVerbsIntroduce().Length);
    }

    [Theory]
    [MemberData(nameof(FamilyNames))]
    public void Architecture_NamesTheVerbsOfEachFamily_AndOnlyThose(string family)
    {
        string section = SectionOf(Architecture, "Where a verb lives");
        string[] named = CodeSpansAfter(BulletOf(section, $"`{family}`"), "Verbs:");
        string[] shipped = [.. VerbsOf(Families.Single(type => type.Name == family)).Select(verb => verb.Name)];

        Assert.True(shipped.Order().SequenceEqual(named.Order()),
            $"{family}: ARCHITECTURE.md lists [{Show(named)}] and the class declares [{Show(shipped)}]; missing [{Show(shipped.Except(named))}], stale [{Show(named.Except(shipped))}].");
    }

    [Fact]
    public void Architecture_NamesThePartsAndTheNamedData_TheVerbsIntroduce()
    {
        string section = SectionOf(Architecture, "Where a verb lives");
        string[] named =
        [
            .. CodeSpansAfter(BulletOf(section, "Parts"), "Parts:"),
            .. CodeSpansAfter(BulletOf(section, "Named data"), "Named data:"),
        ];
        string[] shipped = [.. TypesTheVerbsIntroduce().Select(type => type.Name)];

        Assert.True(shipped.Order().SequenceEqual(named.Order()),
            $"ARCHITECTURE.md names [{Show(named)}] and the verbs take or hand out [{Show(shipped)}]; missing [{Show(shipped.Except(named))}], stale [{Show(named.Except(shipped))}].");
    }

    [Fact]
    public void Architecture_StatesTheNumberOfVerbs_AndTheParameterBudget_TheCodeKeeps()
    {
        string section = SectionOf(Architecture, "Where a verb lives");

        var verbs = Regex.Match(section, @"(\d+) verbs");
        var budget = Regex.Match(section, @"at most (\d+) parameters besides");

        Assert.True(verbs.Success, "ARCHITECTURE.md states no \"N verbs\" count in the section on where a verb lives.");
        Assert.Equal(AllVerbs.Length, int.Parse(verbs.Groups[1].Value));
        Assert.True(budget.Success, "ARCHITECTURE.md states no \"at most N parameters besides\" budget in the section on where a verb lives.");
        Assert.Equal(ParameterBudget, int.Parse(budget.Groups[1].Value));
    }

    [Fact]
    public void Contributing_GivesThePlacementRule_AndNamesEveryFamily()
    {
        string rule = SectionOf(Contributing, "Adding a chart verb");

        var missing = Families.Select(family => family.Name).Where(name => !rule.Contains($"`{name}`", StringComparison.Ordinal)).ToArray();
        var budget = Regex.Match(rule, @"at most (\d+) parameters besides");

        Assert.True(missing.Length == 0, $"CONTRIBUTING.md's rule for adding a verb does not name: {Show(missing)}.");
        Assert.True(budget.Success, "CONTRIBUTING.md's rule for adding a verb states no \"at most N parameters besides\" budget.");
        Assert.Equal(ParameterBudget, int.Parse(budget.Groups[1].Value));
    }

    [Fact]
    public void Contributing_NamesTheFixtureAndTheTestsAVerbIsAddedTo()
    {
        string rule = SectionOf(Contributing, "Adding a chart verb");

        string[] mustName = ["ComposedChartShapes", "ComposedChartRoundTripTests"];
        var missing = mustName.Where(name => !rule.Contains($"`{name}`", StringComparison.Ordinal)).ToArray();

        Assert.True(missing.Length == 0, $"CONTRIBUTING.md's rule for adding a verb does not name: {Show(missing)}.");
    }

    [Fact]
    public void Contributing_ExplainsTheDrawnPropertyContract_ByNamingItsTests()
    {
        string contract = SectionOf(Contributing, "The JSON keeps what is drawn");

        string[] mustName =
        [
            "SeriesDrawnPropertiesRoundTripTests", "AxesDrawnPropertiesRoundTripTests", "DrawnSeriesFixtures", "DrawnAxesFixtures",
        ];
        var missing = mustName.Where(name => !contract.Contains($"`{name}`", StringComparison.Ordinal)).ToArray();

        Assert.True(missing.Length == 0, $"CONTRIBUTING.md's section on the drawn-property contract does not name: {Show(missing)}.");
    }

    [Theory]
    [InlineData("ARCHITECTURE.md")]
    [InlineData("CONTRIBUTING.md")]
    public void EveryTestClassADocumentNames_ExistsInTheTestAssembly(string document)
    {
        string text = document == "ARCHITECTURE.md" ? Architecture : Contributing;
        var exists = typeof(VerbPlacementContractTests).Assembly.GetTypes().Select(type => type.Name.Split('`')[0]).ToHashSet(StringComparer.Ordinal);

        var named = Regex.Matches(text, @"`(\w+(?:Tests|Shapes|Fixtures))`").Select(match => match.Groups[1].Value).Distinct().ToArray();
        var absent = named.Where(name => !exists.Contains(name)).ToArray();

        Assert.True(absent.Length == 0, $"{document} names test classes the test assembly does not have: {Show(absent)}.");
    }

    /// <summary>The documents that describe what the library is today, as the colormap count is held across them: every
    /// Markdown file but the changelog (a record of what each release added) and the path manifests (working records).</summary>
    private static string[] CurrentStateDocuments() =>
        [.. Directory.EnumerateFiles(Root, "*.md", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Root, file).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal)
                        && !path.Contains("/obj/", StringComparison.Ordinal)
                        && !path.Contains("node_modules/", StringComparison.Ordinal)
                        && !path.StartsWith(".path-manifests/", StringComparison.Ordinal)
                        && path != "CHANGELOG.md")
            .OrderBy(path => path, StringComparer.Ordinal)];

    [Fact]
    public void EveryDocumentThatCountsTheVerbs_CountsWhatTheLibraryShips()
    {
        var wrong = new List<string>();
        foreach (string path in CurrentStateDocuments())
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(Path.Combine(Root, path)), @"\b(\d+) (?:chart )?verbs\b"))
            {
                if (int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) != AllVerbs.Length)
                {
                    wrong.Add($"{path} says \"{match.Value}\"");
                }
            }
        }

        Assert.True(wrong.Count == 0, $"The library ships {AllVerbs.Length} verbs; {Show(wrong)}.");
    }

    [Theory]
    [InlineData("Src/MatPlotLibNet/ARCHITECTURE.md")]
    [InlineData("docs/index.md")]
    [InlineData("docs/api/index.md")]
    [InlineData("articles/intro.md")]
    public void TheDocumentsThatIntroduceTheLibrary_SayHowManyChartsTheVerbsDrawInOneCall(string path)
    {
        string text = File.ReadAllText(Path.Combine(Root, path));

        Assert.True(Regex.IsMatch(text, @"\b\d+ (?:chart )?verbs\b"), $"{path} states no \"N verbs\" count, so a reader of the introduction never meets the one-call charts.");
    }

    // ---- the code ---------------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(VerbNames))]
    public void EveryVerb_KeepsTheShapeThePlacementRuleGives(string name)
    {
        var verb = AllVerbs.Single(candidate => candidate.Name == name);

        var violations = verb.ShapeViolations(LibraryNamespace, ParameterBudget);

        Assert.True(violations.Count == 0, $"{verb.DeclaringType!.Name}.{name}: {Show(violations)}.");
    }

    // ---- the container verbs ------------------------------------------------------------------------------------------

    /// <summary>The verbs that add a Y axis to the container: instance methods of the builder (they add an axis, not a
    /// composed chart, so they are not in a family), found through the callback they hand a
    /// <see cref="YAxisSlotBuilder"/> to, so a verb added tomorrow is found without an edit here.</summary>
    private static MethodInfo[] ContainerVerbs =>
        [.. typeof(AxesBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Action<YAxisSlotBuilder>)))
            .OrderBy(method => method.Name, StringComparer.Ordinal)];

    [Fact]
    public void TheContainerVerbs_AreFound()
    {
        Assert.Equal(["WithAddLeftYAxis", "WithAddRightYAxis"], ContainerVerbs.Select(verb => verb.Name));
    }

    [Fact]
    public void TheContainerVerbs_KeepTheShapeThePlacementRuleGives()
    {
        foreach (var verb in ContainerVerbs)
        {
            var violations = verb.ContainerVerbViolations(ParameterBudget);

            Assert.True(violations.Count == 0, $"AxesBuilder.{verb.Name}: {Show(violations)}.");
        }
    }

    [Theory]
    [InlineData(nameof(ContainerVerbViolators.NotPublic), "public")]
    [InlineData(nameof(ContainerVerbViolators.NotFluent), "returns")]
    [InlineData(nameof(ContainerVerbViolators.TooManyParameters), "parameters")]
    [InlineData(nameof(ContainerVerbViolators.NoCallback), "callback")]
    [InlineData(nameof(ContainerVerbViolators.ConfigureNotLast), "callback")]
    [InlineData(nameof(ContainerVerbViolators.WrongBuilder), "callback")]
    [InlineData(nameof(ContainerVerbViolators.ConfigureMisnamed), "configure")]
    public void AContainerVerbRuleBreaker_IsNamedByTheShapeCheck(string violator, string fragment)
    {
        var method = typeof(ContainerVerbViolators).GetMethod(violator, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

        var violations = method.ContainerVerbViolations(ParameterBudget);

        Assert.Contains(violations, violation => violation.Contains(fragment, StringComparison.Ordinal));
    }

    [Fact]
    public void AContainerVerbThatKeepsTheShape_IsNotNamedByTheShapeCheck()
    {
        var method = typeof(ContainerVerbViolators).GetMethod(nameof(ContainerVerbViolators.Fine))!;

        Assert.Empty(method.ContainerVerbViolations(ParameterBudget));
    }

    [Fact]
    public void EveryPartsTypeAVerbHandsOut_CanOnlyBeMadeByTheLibrary()
    {
        var parts = TypesTheVerbsIntroduce().Where(type => type.Name.EndsWith("Parts", StringComparison.Ordinal)).ToArray();

        Assert.Equal(7, parts.Length);
        foreach (var type in parts)
        {
            Assert.True(type.PartsViolations().Count == 0, $"{type.Name}: {Show(type.PartsViolations())}.");
        }
    }

    [Theory]
    [InlineData(nameof(VerbViolators.TooManyParameters), "parameters besides")]
    [InlineData(nameof(VerbViolators.ConfigureNotLast), "configure")]
    [InlineData(nameof(VerbViolators.ConfigureRequired), "configure")]
    [InlineData(nameof(VerbViolators.ConfigureMisnamed), "configure")]
    [InlineData(nameof(VerbViolators.NotFluent), "returns")]
    [InlineData(nameof(VerbViolators.WrongReceiver), "extend")]
    [InlineData(nameof(VerbViolators.PlainStatic), "extension")]
    public void ARuleBreaker_IsNamedByTheShapeCheck(string violator, string fragment)
    {
        var method = typeof(VerbViolators).GetMethod(violator, BindingFlags.Public | BindingFlags.Static)!;

        var violations = method.ShapeViolations(typeof(VerbViolators).Namespace!, ParameterBudget);

        Assert.Contains(violations, violation => violation.Contains(fragment, StringComparison.Ordinal));
    }

    [Fact]
    public void AVerbOutsideTheLibrarysNamespace_IsNamedByTheShapeCheck()
    {
        var method = typeof(VerbViolators).GetMethod(nameof(VerbViolators.Fine), BindingFlags.Public | BindingFlags.Static)!;

        Assert.Empty(method.ShapeViolations(typeof(VerbViolators).Namespace!, ParameterBudget));
        Assert.Contains(method.ShapeViolations(LibraryNamespace, ParameterBudget), violation => violation.Contains("namespace", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(typeof(PartsWithAPublicConstructor), "constructor")]
    [InlineData(typeof(PartsWithASetter), "setter")]
    public void APartsTypeThatCanBeMadeOutsideTheLibrary_IsNamedByTheShapeCheck(Type parts, string fragment)
    {
        Assert.Contains(parts.PartsViolations(), violation => violation.Contains(fragment, StringComparison.Ordinal));
    }
}

/// <summary>The rules a verb is held to, written once: the same checks run over the real verbs and over the rule breakers
/// below, which proves each rule fires.</summary>
internal static class VerbShapeExtensions
{
    internal static IReadOnlyList<string> ShapeViolations(this MethodInfo verb, string expectedNamespace, int parameterBudget)
    {
        var violations = new List<string>();
        var parameters = verb.GetParameters();

        if (!verb.IsDefined(typeof(ExtensionAttribute), inherit: false))
        {
            violations.Add("is not an extension method");
        }

        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(AxesBuilder))
        {
            violations.Add("does not extend AxesBuilder");
        }

        if (verb.DeclaringType!.Namespace != expectedNamespace)
        {
            violations.Add($"is declared in namespace {verb.DeclaringType.Namespace}, not {expectedNamespace}");
        }

        if (verb.ReturnType != typeof(AxesBuilder))
        {
            violations.Add($"returns {verb.ReturnType.Name}, not AxesBuilder, so the chain breaks");
        }

        if (parameters.Length - 1 > parameterBudget)
        {
            violations.Add($"takes {parameters.Length - 1} parameters besides the builder; at most {parameterBudget}");
        }

        for (int i = 1; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            bool callback = parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Action<>);
            if (!callback && parameter.Name == "configure")
            {
                violations.Add("has a configure parameter that is not an Action<T>");
            }

            if (callback && parameter.Name != "configure")
            {
                violations.Add($"hands a callback to a parameter named {parameter.Name}, not configure");
            }

            if (callback && i != parameters.Length - 1)
            {
                violations.Add("has a configure callback that is not the last parameter");
            }

            if (callback && !(parameter.HasDefaultValue && parameter.DefaultValue is null))
            {
                violations.Add("has a configure callback that cannot be left out");
            }
        }

        return violations;
    }

    /// <summary>The shape of a container verb, an instance method of the builder that stacks one Y axis and hands it to a
    /// callback: public, returns the builder so the chain goes on, takes at most <paramref name="parameterBudget"/>
    /// parameters, and ends with the callback, named <c>configure</c> and typed <c>Action&lt;YAxisSlotBuilder&gt;</c>.</summary>
    internal static IReadOnlyList<string> ContainerVerbViolations(this MethodInfo verb, int parameterBudget)
    {
        var violations = new List<string>();
        var parameters = verb.GetParameters();

        if (!verb.IsPublic)
        {
            violations.Add("is not public");
        }

        if (verb.ReturnType != typeof(AxesBuilder))
        {
            violations.Add($"returns {verb.ReturnType.Name}, not AxesBuilder, so the chain breaks");
        }

        if (parameters.Length > parameterBudget)
        {
            violations.Add($"takes {parameters.Length} parameters; at most {parameterBudget}");
        }

        var last = parameters.LastOrDefault();
        if (last is null || last.ParameterType != typeof(Action<YAxisSlotBuilder>))
        {
            violations.Add("does not end with an Action<YAxisSlotBuilder> callback");
        }
        else if (last.Name != "configure")
        {
            violations.Add($"names its trailing callback {last.Name}, not configure");
        }

        return violations;
    }

    internal static IReadOnlyList<string> PartsViolations(this Type parts)
    {
        var violations = new List<string>();

        if (!parts.IsValueType)
        {
            violations.Add("is not a value type");
        }

        if (parts.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
        {
            violations.Add("has a public constructor, so a caller can make one");
        }

        foreach (var property in parts.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanWrite))
        {
            violations.Add($"has a public setter on {property.Name}");
        }

        return violations;
    }
}

/// <summary>Methods that each break one rule of the shape, so the check that names the rule is shown to fire. Not verbs:
/// nothing in the library calls them.</summary>
internal static class VerbViolators
{
    public static AxesBuilder Fine(this AxesBuilder axes, int count = 1, Action<ScatterSeries>? configure = null) => axes;

    public static AxesBuilder TooManyParameters(this AxesBuilder axes, int a, int b, int c, int d, int e) => axes;

    public static AxesBuilder ConfigureNotLast(this AxesBuilder axes, Action<ScatterSeries>? configure, int count = 0) => axes;

    public static AxesBuilder ConfigureRequired(this AxesBuilder axes, Action<ScatterSeries> configure) => axes;

    public static AxesBuilder ConfigureMisnamed(this AxesBuilder axes, Action<ScatterSeries>? style = null) => axes;

    public static void NotFluent(this AxesBuilder axes, int count) { }

    public static AxesBuilder WrongReceiver(this FigureBuilder figure) => throw new NotSupportedException();

    public static AxesBuilder PlainStatic(AxesBuilder axes) => axes;
}

/// <summary>Instance methods that each break one rule of the container verbs' shape, so the check that names the rule is
/// shown to fire. Not verbs: nothing in the library calls them.</summary>
internal sealed class ContainerVerbViolators
{
    public AxesBuilder Fine(int count, Action<YAxisSlotBuilder> configure) => throw new NotSupportedException();

    internal AxesBuilder NotPublic(Action<YAxisSlotBuilder> configure) => throw new NotSupportedException();

    public void NotFluent(Action<YAxisSlotBuilder> configure) { }

    public AxesBuilder TooManyParameters(int a, int b, int c, int d, Action<YAxisSlotBuilder> configure) => throw new NotSupportedException();

    public AxesBuilder NoCallback(int count) => throw new NotSupportedException();

    public AxesBuilder ConfigureNotLast(Action<YAxisSlotBuilder> configure, int count) => throw new NotSupportedException();

    public AxesBuilder WrongBuilder(Action<AxesBuilder> configure) => throw new NotSupportedException();

    public AxesBuilder ConfigureMisnamed(Action<YAxisSlotBuilder> style) => throw new NotSupportedException();
}

internal readonly record struct PartsWithAPublicConstructor
{
    public PartsWithAPublicConstructor(ScatterSeries points) => Points = points;

    public ScatterSeries Points { get; }
}

internal record struct PartsWithASetter
{
    internal PartsWithASetter(ScatterSeries points) => Points = points;

    public ScatterSeries Points { get; set; }
}
