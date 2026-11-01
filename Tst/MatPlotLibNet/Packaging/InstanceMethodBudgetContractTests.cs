// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The budget of parameters for a method of a fluent builder. A verb (an extension method of <c>AxesBuilder</c>) is
/// held to four parameters besides the builder by <c>VerbPlacementContractTests</c>; this holds the instance methods of the
/// builders themselves to the same four, so a new <c>AddXxx</c> or <c>WithXxx</c> takes a value that groups its inputs (a
/// <c>DataPoint</c>, an options value, the object itself) instead of a fifth and sixth loose argument. A signature that
/// shipped is permanent (package validation rejects a changed one), so the older methods over the limit are named in
/// <see cref="PublishedExceptions"/> with the reason, and the list is pinned both ways: a method over the limit that is not
/// named, and a name for a method that no longer exists or takes another number of parameters, turn the test red.</summary>
public class InstanceMethodBudgetContractTests
{
    /// <summary>The number of parameters a public instance method of a builder may take. ARCHITECTURE.md states the same
    /// number, and a test below holds it to it.</summary>
    private const int ParameterBudget = 4;

    private static readonly string Root = RepoRoot();

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

    /// <summary>One method of a builder that shipped before the limit was held, and why it stays: a signature that has been
    /// published is permanent. A method is named by its builder, its name and its number of parameters; two overloads that
    /// share all three are two entries.</summary>
    private readonly record struct PublishedException(string Builder, string Method, int ParameterCount, string Reason)
    {
        internal string Key => $"{Builder}.{Method}({ParameterCount})";
    }

    /// <summary>The reason every entry below has in common, with what the entry's parameters are.</summary>
    private static string Published(string parameters) =>
        $"Published in an earlier release as ({parameters}); a changed signature is a binary break that package validation rejects.";

    /// <summary>The methods that shipped over the limit, each with the number of parameters it was published with (all of them
    /// are in the package the validation compares against). Nothing is added to this list for a new method: a new method keeps
    /// the budget. A method leaves the list when the day comes that a release may break it.</summary>
    private static readonly PublishedException[] PublishedExceptions =
    [
        new("AxesBuilder", "AddInset", 5, Published("x, y, width, height, configure")),
        new("AxesBuilder", "AddTrendline", 5, Published("two points as x1, y1, x2, y2, and configure")),
        new("AxesBuilder", "Annotate", 6, Published("text, the point, the arrow tip as two numbers, and configure")),
        new("AxesBuilder", "Barbs", 5, Published("x, y, speed, direction, configure")),
        new("AxesBuilder", "Bocpd", 5, Published("prices, hazard, priorVariance, maxRunLength, configure")),
        new("AxesBuilder", "Candlestick", 6, Published("open, high, low, close, dateLabels, configure")),
        new("AxesBuilder", "ErrorBar", 5, Published("x, y, the low and the high error, configure")),
        new("AxesBuilder", "KlingerVolumeOscillator", 5, Published("bars, the fast, slow and signal period, configure")),
        new("AxesBuilder", "OhlcBar", 6, Published("open, high, low, close, dateLabels, configure")),
        new("AxesBuilder", "ParabolicSar", 5, Published("high, low, step, max, configure")),
        new("AxesBuilder", "Quiver", 5, Published("x, y, u, v, configure")),
        new("AxesBuilder", "Quiver3D", 7, Published("x, y, z, u, v, w, configure")),
        new("AxesBuilder", "QuiverKey", 5, Published("x, y, u, label, configure")),
        new("AxesBuilder", "SqueezeMomentum", 5, Published("bars, period, bbMult, kcMult, configure")),
        new("AxesBuilder", "Streamplot", 5, Published("x, y, the grids u and v, configure")),
        new("AxesBuilder", "Text3D", 5, Published("x, y, z, text, configure")),
        new("AxesBuilder", "Threshold", 5, Published("value, orientation, breach, color, label")),
        new("AxesBuilder", "TransferEntropy", 5, Published("source, target, bins, lag, configure")),
        new("AxesBuilder", "WithLighting", 5, Published("the light direction as dx, dy, dz, ambient, diffuse")),
        new("FigureBuilder", "AddInset", 6, Published("subplotIndex, x, y, width, height, configure")),
        new("FigureBuilder", "AddSubPlot", 5, Published("rows, cols, index, configure, key")),
        new("FigureBuilder", "Annotate", 6, Published("text, the point, the arrow tip as two numbers, and configure")),
        new("FigureBuilder", "Barbs", 5, Published("x, y, speed, direction, configure")),
        new("FigureBuilder", "ErrorBar", 5, Published("x, y, the low and the high error, configure")),
        new("FigureBuilder", "QuiverKey", 5, Published("x, y, u, label, configure")),
        new("FigureBuilder", "Threshold", 5, Published("value, orientation, breach, color, label")),
        new("FigureBuilder", "WithLighting", 5, Published("the light direction as dx, dy, dz, ambient, diffuse")),
    ];

    /// <summary>The fluent builders of the library: every public class of the core assembly whose name ends in
    /// <c>Builder</c>, so a builder added tomorrow is held to the budget without an edit here.</summary>
    private static IReadOnlyList<Type> Builders { get; } =
    [
        .. typeof(AxesBuilder).Assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Builder", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal),
    ];

    private static IReadOnlyList<BudgetOffender> Offenders { get; } = [.. Builders.SelectMany(builder => builder.MethodsOverTheBudget(ParameterBudget))];

    private static string Show(IEnumerable<string> names) => names.Any() ? string.Join(", ", names) : "(none)";

    [Fact]
    public void TheBuildersInScope_AreFound()
    {
        string[] names = [.. Builders.Select(builder => builder.Name)];

        foreach (string expected in new[] { "AxesBuilder", "FigureBuilder", "YAxisSlotBuilder", "SecondaryXAxisBuilder" })
        {
            Assert.Contains(expected, names);
        }
    }

    [Fact]
    public void EveryPublicInstanceMethodOfABuilder_TakesAtMostTheBudget_OrIsAPublishedException()
    {
        var named = PublishedExceptions.Select(published => published.Key).ToList();
        var unnamed = new List<string>();

        foreach (var offender in Offenders)
        {
            if (!named.Remove(offender.Key))
            {
                unnamed.Add($"{offender.Key} takes {offender.ParameterCount} parameters");
            }
        }

        Assert.True(unnamed.Count == 0,
            $"A builder method takes at most {ParameterBudget} parameters; take a value that groups the inputs instead (a DataPoint, an options value, the object itself). Over the limit and not a published exception: {Show(unnamed)}.");
    }

    [Fact]
    public void EveryPublishedException_StillExists_WithTheNumberOfParametersItNames()
    {
        var found = Offenders.Select(offender => offender.Key).ToList();
        var gone = new List<string>();

        foreach (var published in PublishedExceptions)
        {
            if (!found.Remove(published.Key))
            {
                gone.Add(published.Key);
            }
        }

        Assert.True(gone.Count == 0,
            $"These published exceptions name a method that is not over the limit any more, or no longer exists, or takes another number of parameters; take the name off the list: {Show(gone)}.");
    }

    [Fact]
    public void EveryPublishedException_NamesABuilderOfTheLibrary_AndGivesItsReason()
    {
        var builders = Builders.Select(builder => builder.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var published in PublishedExceptions)
        {
            Assert.True(builders.Contains(published.Builder), $"{published.Key} names a builder the library does not have.");
            Assert.True(published.ParameterCount > ParameterBudget, $"{published.Key} is within the budget of {ParameterBudget}, so it is no exception.");
            Assert.False(string.IsNullOrWhiteSpace(published.Reason), $"{published.Key} gives no reason.");
        }
    }

    [Fact]
    public void Architecture_StatesTheBudgetOfABuilderMethod_AndNamesTheTestThatHoldsIt()
    {
        string section = SectionOf(Architecture, "Where a verb lives");
        string[] words = ["zero", "one", "two", "three", "four", "five", "six"];

        Assert.Contains($"at most {words[ParameterBudget]} parameters", section, StringComparison.Ordinal);
        Assert.Contains($"`{nameof(InstanceMethodBudgetContractTests)}`", section, StringComparison.Ordinal);
    }

    /// <summary>The text of a "## heading" section of a Markdown document, up to the next "## " heading.</summary>
    private static string SectionOf(string markdown, string heading)
    {
        int start = markdown.IndexOf($"\n## {heading}", StringComparison.Ordinal);
        Assert.True(start >= 0, $"The document has no section \"## {heading}\".");
        int next = markdown.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        return next < 0 ? markdown[start..] : markdown[start..next];
    }

    // ---- the check itself ---------------------------------------------------------------------------------------------

    [Fact]
    public void AMethodOverTheBudget_IsNamedByTheCheck()
    {
        var offenders = typeof(BudgetViolators).MethodsOverTheBudget(ParameterBudget);

        Assert.Equal(["BudgetViolators.TooMany(5)", "BudgetViolators.TooMany(6)"], offenders.Select(offender => offender.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AMethodWithinTheBudget_AnAccessorAndAMethodNobodyOutsideCanCall_AreNotNamedByTheCheck()
    {
        var offenders = typeof(BudgetViolators).MethodsOverTheBudget(ParameterBudget);

        Assert.DoesNotContain(offenders, offender => offender.Method is "Fine" or "NotPublic" or "Static" or "Item");
    }

    [Fact]
    public void ARaisedBudget_ShrinksWhatTheCheckNames()
    {
        var offenders = typeof(BudgetViolators).MethodsOverTheBudget(5);

        Assert.Equal(["BudgetViolators.TooMany(6)"], offenders.Select(offender => offender.Key));
    }
}

/// <summary>A public instance method of a builder that takes more parameters than the budget allows, found by reflection.</summary>
internal readonly record struct BudgetOffender(string Builder, string Method, int ParameterCount)
{
    internal string Key => $"{Builder}.{Method}({ParameterCount})";
}

/// <summary>The rule the budget gives, written once: the same check runs over the real builders and over the rule breakers
/// below, which proves the rule fires.</summary>
internal static class BuilderBudgetExtensions
{
    extension(Type builder)
    {
        /// <summary>Every public instance method the class declares itself (not an accessor, not one it inherits) with more than
        /// <paramref name="parameterBudget"/> parameters.</summary>
        internal IReadOnlyList<BudgetOffender> MethodsOverTheBudget(int parameterBudget) =>
        [
            .. builder.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName && method.GetParameters().Length > parameterBudget)
                .Select(method => new BudgetOffender(builder.Name, method.Name, method.GetParameters().Length)),
        ];
    }
}

/// <summary>Methods that each stand on one side of the rule, so the check that names the rule is shown to fire. Not builders:
/// nothing in the library calls them.</summary>
internal sealed class BudgetViolators
{
    public void Fine(int a, int b, int c, int d) { }

    public void TooMany(int a, int b, int c, int d, int e) { }

    public void TooMany(int a, int b, int c, int d, int e, int f) { }

    internal void NotPublic(int a, int b, int c, int d, int e) { }

    public static void Static(int a, int b, int c, int d, int e) { }

    public int this[int a, int b, int c, int d, int e] => 0;
}
