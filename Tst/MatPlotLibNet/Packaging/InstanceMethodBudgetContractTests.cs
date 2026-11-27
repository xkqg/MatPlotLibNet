// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The budget of parameters for a method of a fluent builder. A composed-chart verb (an extension method of
/// <c>AxesBuilder</c> in a family class) is held to four parameters besides the builder by <c>VerbPlacementContractTests</c>; this
/// holds every other call of the builders to the same four, the methods they declare and the extension members the library adds
/// to them (<see cref="BuilderCallExtensions"/>), so a new <c>AddXxx</c> or <c>WithXxx</c> takes a value that groups its inputs (a
/// <c>DataPoint</c>, an options value, the object itself) instead of a fifth and sixth loose argument. A signature that
/// shipped is permanent (package validation rejects a changed one), so the older methods over the limit are named in
/// <see cref="PublishedExceptions"/> with the reason, and the list is pinned both ways: a method over the limit that is not
/// named, and a name for a method that no longer exists or takes another number of parameters, turn the test red.</summary>
public class InstanceMethodBudgetContractTests
{
    /// <summary>The number of parameters a public instance method of a builder may take. ARCHITECTURE.md states the same
    /// number, and a test below holds it to it.</summary>
    private const int ParameterBudget = 4;

    private static readonly string Root = RepositoryRoot.Folder;

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

    /// <summary>The public constructors and static factories that shipped over the limit, each with the number of parameters it was
    /// published with. The budget holds every new one: a constructor of a class or a struct that is not a record (a record's positional
    /// constructor is its shape, one parameter per value it holds), and a static factory (a static method that makes its own type, a
    /// builder, or the product of a <c>*Factory</c>). Nothing is added to this list for a new one.</summary>
    private static readonly PublishedException[] PublishedConstructorsAndFactories =
    [
        new("AxesRenderer", "Create", 5, Published("axes, plot area, context, theme, layout")),
        new("ColorBarRendererFactory", "Create", 8, Published("the colour bar, its range, map, normaliser, plot area, context, theme and orientation")),
        new("DataCursorModifier", ".ctor", 5, Published("chart id, layout, figure, sink, pan")),
        new("DataTransform", ".ctor", 5, Published("the data limits as four numbers and the plot bounds")),
        new("DataTransform", ".ctor", 11, Published("the data limits, the plot bounds, the breaks and the full limits")),
        new("DataTransform", ".ctor", 15, Published("the data limits, the plot bounds, the breaks, the full limits, the scales and the symlog thresholds")),
        new("EasedFigureAnimation", ".ctor", 5, Published("frame count, frame generator, easing, interval, loop")),
        new("FigureTemplates", "FacetGrid", 5, Published("x, y, groups, the panel's configure, cols")),
        new("FigureTemplates", "FinancialDashboard", 9, Published("open, high, low, close, volume, title, and the three panels' configure")),
        new("FigureTemplates", "ScientificPaper", 6, Published("rows, cols, the panels' configure, title, width, height")),
        new("Ichimoku", ".ctor", 5, Published("bars and the four periods")),
        new("Projection3D", ".ctor", 10, Published("elevation, azimuth, plot bounds, the data box as six numbers, distance")),
        new("Quiver3DSeries", ".ctor", 6, Published("x, y, z, u, v, w")),
        new("SubPlotSpacing", "FromFractions", 6, Published("the four margins and the two gaps as fractions")),
    ];

    /// <summary>The public types of the core library.</summary>
    private static IReadOnlyList<Type> PublicTypes { get; } =
        [.. typeof(AxesBuilder).Assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal)];

    private static IReadOnlyList<BudgetOffender> MakersOverTheBudget { get; } =
        [.. PublicTypes.SelectMany(type => type.ConstructorsAndFactoriesOverTheBudget(ParameterBudget))];

    [Fact]
    public void EveryPublicConstructorAndStaticFactory_TakesAtMostTheBudget_OrIsAPublishedException()
    {
        var named = PublishedConstructorsAndFactories.Select(published => published.Key).ToList();
        var unnamed = MakersOverTheBudget.Where(offender => !named.Remove(offender.Key)).Select(offender => offender.Key).ToList();

        Assert.True(unnamed.Count == 0,
            $"A constructor or a static factory takes at most {ParameterBudget} parameters; take a value that groups the inputs. Over the limit and not a published exception: {Show(unnamed)}.");
    }

    [Fact]
    public void EveryPublishedConstructorOrFactory_StillExists_WithTheNumberOfParametersItNames()
    {
        var found = MakersOverTheBudget.Select(offender => offender.Key).ToList();
        var gone = PublishedConstructorsAndFactories.Where(published => !found.Remove(published.Key)).Select(published => published.Key).ToList();

        Assert.True(gone.Count == 0, $"These published exceptions name a constructor or a factory that is not over the limit any more or no longer exists: {Show(gone)}.");
        Assert.All(PublishedConstructorsAndFactories, published => Assert.True(published.ParameterCount > ParameterBudget, $"{published.Key} is within the budget."));
    }

    [Fact]
    public void AConstructorAndAFactoryOverTheBudget_AreNamedByTheCheck_ARecordsShapeAndAMethodThatMakesNothingAreNot()
    {
        Assert.Equal(["BudgetViolators..ctor(5)", "BudgetViolators.Make(5)"],
            typeof(BudgetViolators).ConstructorsAndFactoriesOverTheBudget(ParameterBudget).Select(offender => offender.Key).Order(StringComparer.Ordinal));
        Assert.Empty(typeof(BudgetRecord).ConstructorsAndFactoriesOverTheBudget(ParameterBudget));
    }

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
        string section = SourceFile.SectionOf(Architecture, "Where a verb lives");
        string[] words = ["zero", "one", "two", "three", "four", "five", "six"];

        Assert.Contains($"at most {words[ParameterBudget]} parameters", section, StringComparison.Ordinal);
        Assert.Contains($"`{nameof(InstanceMethodBudgetContractTests)}`", section, StringComparison.Ordinal);
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
        /// <summary>Every call of the builder (<see cref="BuilderCallExtensions"/>: a public instance method it declares itself, not an
        /// accessor, or an extension member the library adds to it) with more than <paramref name="parameterBudget"/> parameters
        /// besides the builder.</summary>
        internal IReadOnlyList<BudgetOffender> MethodsOverTheBudget(int parameterBudget) =>
        [
            .. builder.Calls()
                .Where(call => call.Parameters.Length > parameterBudget)
                .Select(call => new BudgetOffender(builder.Name, call.Method.Name, call.Parameters.Length)),
        ];

        /// <summary>Every public constructor of the type, when it is not a record (a record's positional constructor is its shape), and
        /// every public static method that makes something (its own type, a builder, or anything a <c>*Factory</c> makes), with more
        /// than <paramref name="parameterBudget"/> parameters.</summary>
        internal IReadOnlyList<BudgetOffender> ConstructorsAndFactoriesOverTheBudget(int parameterBudget)
        {
            bool isRecord = builder.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null;
            var constructors = isRecord ? [] : builder.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(constructor => constructor.GetParameters().Length > parameterBudget)
                .Select(constructor => new BudgetOffender(builder.Name, ".ctor", constructor.GetParameters().Length));
            var factories = builder.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName && !method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false)
                    && method.GetParameters().Length > parameterBudget
                    && (method.ReturnType == builder || method.ReturnType.Name.EndsWith("Builder", StringComparison.Ordinal)
                        || builder.Name.EndsWith("Factory", StringComparison.Ordinal)))
                .Select(method => new BudgetOffender(builder.Name, method.Name, method.GetParameters().Length));
            return [.. constructors, .. factories];
        }
    }
}

/// <summary>A record whose positional constructor takes more values than the budget: its shape, not a parameter list.</summary>
internal sealed record BudgetRecord(int A, int B, int C, int D, int E);

/// <summary>Methods that each stand on one side of the rule, so the check that names the rule is shown to fire. Not builders:
/// nothing in the library calls them.</summary>
internal sealed class BudgetViolators
{
    public BudgetViolators() { }

    public BudgetViolators(int a, int b, int c, int d, int e) { }

    public static BudgetViolators Make(int a, int b, int c, int d, int e) => new(a, b, c, d, e);

    public static int Sum(int a, int b, int c, int d, int e) => a + b + c + d + e;

    public void Fine(int a, int b, int c, int d) { }

    public void TooMany(int a, int b, int c, int d, int e) { }

    public void TooMany(int a, int b, int c, int d, int e, int f) { }

    internal void NotPublic(int a, int b, int c, int d, int e) { }

    public static void Static(int a, int b, int c, int d, int e) { }

    public int this[int a, int b, int c, int d, int e] => 0;
}
