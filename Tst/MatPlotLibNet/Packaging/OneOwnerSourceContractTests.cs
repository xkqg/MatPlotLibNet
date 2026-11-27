// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Tests.Packaging.OwnedRules;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A rule that the library works out in one place stays in that place. Each area of <c>OwnedRules</c> holds a row per rule:
/// the rule, the pattern that spells it in source, the only files allowed to spell it, and the lines it must find and must pass. The
/// scan reads the code of every file under <c>Src</c>, its Razor components included (for a rule read under <c>Tst</c>, the tests),
/// refuses a second spelling and a row whose owner no longer spells the rule, and reads each row's probes in a file of its own, so a
/// row can neither outlive the mechanism it guards nor drift away from the spelling it was written for and go green by matching
/// nothing. A step that gives a rule one owner adds its row to its area in the same increment; the scan holds the census at zero for
/// good. The scan itself is held by <see cref="OwnedRuleTests"/>.</summary>
public class OneOwnerSourceContractTests
{
    /// <summary>Every rule of every area.</summary>
    private static readonly OwnedRule[] Rules =
    [
        .. BuilderRules.All, .. ColourRules.All, .. DocumentationRules.All, .. GeoRules.All, .. GeometryRules.All, .. IndicatorRules.All, .. InteractionRules.All, .. LineAndShapeRules.All,
        .. MathTextRules.All, .. NumericsRules.All, .. RangeRules.All, .. RefusalRules.All, .. RenderingRules.All, .. RestingValueRules.All,
        .. ScriptAndSvgRules.All, .. SerializationRules.All, .. SeriesRules.All, .. SurfaceRules.All, .. TestCodeRules.All, .. TestReadRules.All, .. TextRules.All,
        .. ThemeRules.All, .. ThreeDRules.All, .. TickAndScaleRules.All,
    ];

    /// <summary>The rows, by rule, for the Theory below.</summary>
    public static TheoryData<string> RuleNames { get; } = new(Rules.Select(rule => rule.Rule));

    [Theory]
    [MemberData(nameof(RuleNames))]
    public void ARule_ReadsItsProbesAsTheySay_AndIsSpelledInItsOwnersAndNowhereElse(string name)
    {
        var rule = Rules.Single(r => r.Rule == name);
        var tree = rule.Under.StartsWith("Tst/", StringComparison.Ordinal)
            ? RepositoryTree.TestFiles()
            : RepositoryTree.SourceFiles().Concat(RepositoryTree.ComponentFiles());

        var findings = rule.MisreadProbes().Concat(rule.Violations(tree)).ToList();

        Assert.True(findings.Count == 0, $"{name}:\n" + string.Join('\n', findings));
    }

    /// <summary>Every rule shows a line it refuses: a rule no spelling is shown to break may match nothing at all, and then it passes on
    /// every tree.</summary>
    [Fact]
    public void EveryRule_CarriesALineItRefuses()
    {
        var unprobed = Rules.Where(rule => rule.Finds.Count == 0).Select(rule => rule.Rule).ToList();

        Assert.True(unprobed.Count == 0, $"{unprobed.Count} rules carry no line of Finds:\n" + string.Join('\n', unprobed));
    }

    [Fact]
    public void TheRules_AreNamedOnce()
    {
        var repeated = Rules.GroupBy(r => r.Rule, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key);

        Assert.Empty(repeated);
    }

    [Fact]
    public void TheScan_ReadsTheTestsOfEveryTestProject()
    {
        var files = RepositoryTree.TestFiles().ToList();

        Assert.Contains(files, f => f.Path == "Tst/MatPlotLibNet/Packaging/RepositoryRoot.cs");
        Assert.Contains(files, f => f.Path.StartsWith("Tst/MatPlotLibNet.Wiki/", StringComparison.Ordinal));
    }

    [Fact]
    public void TheScan_ReadsTheSourceOfTheLibrary()
    {
        var files = RepositoryTree.SourceFiles().ToList();

        Assert.Contains(files, f => f.Path == "Src/MatPlotLibNet/Styling/Themes/MatplotlibThemeFactory.cs");
    }

    [Fact]
    public void TheDocumentsOfToday_AreEveryDocumentButTheChangelog()
    {
        var paths = RepositoryTree.CurrentStateDocuments().Select(f => f.Path).ToList();

        Assert.Contains("README.md", paths);
        Assert.DoesNotContain("CHANGELOG.md", paths);
        Assert.Equal(RepositoryTree.Documents().Count() - 1, paths.Count);
    }

    /// <summary>Every role of the theme's fonts is made from one base: the file builds one font and no more.</summary>
    [Fact]
    public void TheThemeFontRoles_AreMadeFromOneBaseFont()
    {
        var file = RepositoryTree.SourceFiles().Single(f => f.Path == "Src/MatPlotLibNet/Rendering/ThemeFontExtensions.cs");

        Assert.Equal(1, Regex.Count(file.Text, @"\bnew\s*\(\s*\)\s*$", RegexOptions.Multiline) + Regex.Count(file.Text, @"\bnew Font\b"));
    }

    // ── The start of a setting in its documentation, in every file (RestingProse) ─────────────────────────

    /// <summary>The lines that hold a number one of the owners they are read against holds too, kept with the reason the number is
    /// another quantity. Every other line names the start with a cref.</summary>
    private static readonly KeptProse[] AnotherQuantity =
    [
        new("Src/MatPlotLibNet/Models/Series/Categorical/BarSeries.cs", "else 1 pt; a width of 0 draws none",
            "matplotlib's patch.linewidth under the matplotlib themes, not RestingAlpha"),
        new("Src/MatPlotLibNet.DataFrame/DataFrameFigureExtensions.cs", "When null all weights default to 1.0",
            "an edge's weight, GraphEdge's own parameter default with no constant; RestingEdgeThicknessScale turns a weight into a stroke"),
        new("Src/MatPlotLibNet/Builders/DistributionAxesExtensions.cs", "a curve that starts at 1",
            "the survival probability at the first time, a number of the estimate"),
        new("Src/MatPlotLibNet/Rendering/DataTransform.cs", "0 for data that starts at 1",
            "a data value of an example"),
        new("Src/MatPlotLibNet/Rendering/ZOrderExtensions.cs", "else matplotlib's 1",
            "matplotlib's z-order of a patch, a theme's number"),
        new("Src/MatPlotLibNet/Models/Series/Distribution/KdeSeries.cs", "the value this reads",
            "the density fill under the library's look (SeabornVerbLook), not a series' visibility"),
        new("Src/MatPlotLibNet/Models/Series/ThreeD/PlanarBar3DSeries.cs", "else black at 1 pt",
            "matplotlib's patch.linewidth under its classic style, a theme's number"),
    ];

    /// <summary>The start an owner holds (a constant of any name, a static value that rests at one, the literal a property starts from)
    /// is named by its owner in the documentation of every file, the file that declares it included: no sentence types the number again
    /// as a default, a value that is read or included, or an absent field's value.</summary>
    [Fact]
    public void AStartAConstantOwns_IsNamedInTheDocumentationByTheConstant_AlsoInTheFileThatDeclaresIt()
    {
        var findings = new RestingProse(RepositoryTree.SourceFiles(), AnotherQuantity).Violations();

        Assert.True(findings.Count == 0, $"{findings.Count} lines:\n" + string.Join('\n', findings));
    }
}
