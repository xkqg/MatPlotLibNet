// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks.Tests;

/// <summary>The math-label rows. Loading the Skia package makes every later SVG of the process write its text as glyph outlines, so
/// an SVG row in a class that also draws pictures measured text or outlines by whichever row ran before it: the classic theme's SVG
/// row ran after a picture row and wrote outlines (64.7 us), the default theme's ran before one and wrote text (11.6 us), and the two
/// were read as one figure kept under one theme and not under the other. Each class now says which it measures: the SVG class never
/// names a Skia type, and the Skia class loads the package in its setup, so its outlined SVG rows are outlines whatever ran first.</summary>
public partial class MathTextRowsTests
{
    [GeneratedRegex(@"\[Benchmark[^\]]*\]\s*public\s+(?<type>[\w\[\]]+)\s+(?<name>\w+)\s*\(\)")]
    private static partial Regex BenchmarkMethod();

    private static string SourceOf(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "BenchmarkSources", file));

    private static (string Type, string Name)[] BenchmarksOf(string file) =>
        [.. BenchmarkMethod().Matches(SourceOf(file)).Select(m => (m.Groups["type"].Value, m.Groups["name"].Value))];

    [Fact]
    public void TheMathTextClass_NamesNoSkiaType_SoItsSvgRowsWriteTheirTextAsText()
    {
        var source = SourceOf("MathTextBenchmarks.cs");

        Assert.DoesNotContain("MatPlotLibNet.Skia", source, StringComparison.Ordinal);
        Assert.DoesNotContain(BenchmarksOf("MathTextBenchmarks.cs"), m => m.Type == "byte[]");
        Assert.Equal(
            [("int", "ParseFiveLabels"), ("int", "ParseAMathLabel"), ("string", "MathLabelled_ToSvg"), ("string", "MathLabelledClassic_ToSvg")],
            BenchmarksOf("MathTextBenchmarks.cs"));
    }

    [Fact]
    public void TheMathTextSkiaClass_HoldsThePictureRowsAndTheOutlinedSvgRows()
    {
        Assert.Equal(
            [("byte[]", "MathLabelled_ToPng"), ("byte[]", "MathLabelledV2_ToPng"), ("byte[]", "MathLabelledClassic_ToPng"),
             ("string", "MathLabelled_ToOutlinedSvg"), ("string", "MathLabelledClassic_ToOutlinedSvg")],
            BenchmarksOf("MathTextSkiaBenchmarks.cs"));
    }

    /// <summary>An SVG row of a class that names the Skia package measures outlines only once the package is loaded: a class that
    /// loads it in its setup makes that so for every row, also when the row is the only one a run asks for.</summary>
    [Fact]
    public void EveryClassThatNamesTheSkiaPackageAndHoldsAnSvgRow_LoadsThePackageInItsSetup()
    {
        var sources = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "BenchmarkSources"), "*.cs");
        Assert.Contains(sources, s => Path.GetFileName(s) == "SkiaExportBenchmarks.cs");

        var checkedClasses = 0;
        foreach (var path in sources)
        {
            var source = File.ReadAllText(path);
            if (!source.Contains("using MatPlotLibNet.Skia;", StringComparison.Ordinal)) continue;
            if (!BenchmarkMethod().Matches(source).Any(m => m.Groups["type"].Value == "string")) continue;

            var setup = source[source.IndexOf("[GlobalSetup]", StringComparison.Ordinal)..];
            var body = setup[setup.IndexOf('{')..setup.IndexOf('}')];
            Assert.True(body.Contains("FigureSkiaExtensions.Initialize();", StringComparison.Ordinal), $"{Path.GetFileName(path)}'s setup does not load the package");
            checkedClasses++;
        }

        Assert.Equal(2, checkedClasses);
    }

    [Fact]
    public void TheFigures_AreFourPanelsOfMathLabels_UnderAFiguresOwnThemeTheV2InATightLayoutAndTheClassicTheme()
    {
        var figures = new MathLabelledFigures();

        Assert.Equal(new MatPlotLibNet.Models.Figure().Theme, figures.Default.Theme);
        Assert.Equal(Theme.MatplotlibV2, figures.V2.Theme);
        Assert.True(figures.V2.Spacing.TightLayout);
        Assert.False(figures.Default.Spacing.TightLayout);
        Assert.Equal(Theme.MatplotlibClassic, figures.Classic.Theme);
        foreach (var figure in new[] { figures.Default, figures.V2, figures.Classic })
        {
            Assert.Equal(4, figure.SubPlots.Count);
            Assert.Equal(MathLabelledFigures.Labels[1], figure.Title);
            Assert.All(figure.SubPlots, axes => Assert.Contains(axes.Title, MathLabelledFigures.Labels));
        }
    }

    [Fact]
    public void TheV2Figure_MovesEveryPanelsTitleOnToTheNextLabel_SoEveryPanelIsDrawnAgain()
    {
        var figures = new MathLabelledFigures();

        var first = figures.NextV2().SubPlots.Select(a => a.Title).ToArray();
        var second = figures.NextV2().SubPlots.Select(a => a.Title).ToArray();

        Assert.All(first.Zip(second), pair => Assert.NotEqual(pair.First, pair.Second));
        Assert.Equal(first, figures.NextV2().SubPlots.Select(a => a.Title));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AnSvgRowOfTheTextClass_WritesItsLabelsAsText", () => new MathLabelledFigures().Classic.ToSvg(), "<text"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
