// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Benchmarks.MatplotlibThemes;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks.Tests;

/// <summary>The benchmark rows that hold the matplotlib themes: every step of the release that changes how these themes draw is
/// measured against the same figures, so the rows must exist, must be named by a benchmark method, and must be figures that draw.
/// A row that is not in a benchmark class would never be run, and a figure that cannot be drawn would end the whole run.</summary>
public partial class MatplotlibThemeRowsTests
{
    private static readonly string[] SvgRows =
    [
        "V2Grid3x3", "V2MinorGridNoTicks3x3", "ClassicGrid3x3", "V2LogGrid3x3", "V2HeatmapColorBarExtend", "V2InsetZoom10k", "V2Patches20", "V2SevenPanelsFigLegend",
        "ClassicContour", "V2Interactive3D", "V2ConstrainedColorBar", "V2Stem", "V2Barbs", "V2Streamplot", "V2Swarm",
    ];

    private static readonly string[] PngRows = ["V2Grid3x3", "ClassicGrid3x3"];

    private static readonly string[] PdfRows = ["V2Grid3x3Pdf", "ClassicGrid3x3Pdf"];

    /// <summary>Rows of the Skia class that ask the pixel-grid interface of a render context directly: no figure is drawn.</summary>
    private static readonly string[] AskRows = ["PixelGridAskNotSnapping", "PixelGridAskSnapping"];

    [GeneratedRegex(@"\[Benchmark[^\]]*\]\s*public\s+(?<type>[\w\[\]]+)\s+(?<name>\w+)\s*\(\)")]
    private static partial Regex BenchmarkMethod();

    private static (string Type, string Name)[] BenchmarksOf(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "BenchmarkSources", file);
        return [.. BenchmarkMethod().Matches(File.ReadAllText(path)).Select(m => (m.Groups["type"].Value, m.Groups["name"].Value))];
    }

    [Fact]
    public void TheRowsAreTheFifteenSvgRowsTheTwoPictureRowsAndTheTwoPdfRows()
    {
        var rows = new MatplotlibThemeFigures().Rows;

        Assert.Equal(SvgRows, rows.Where(r => r.Output == MatplotlibThemeOutput.Svg).Select(r => r.Name));
        Assert.Equal(PngRows, rows.Where(r => r.Output == MatplotlibThemeOutput.Png).Select(r => r.Name));
        Assert.Equal(PdfRows, rows.Where(r => r.Output == MatplotlibThemeOutput.Pdf).Select(r => r.Name));
    }

    [Fact]
    public void EverySvgRowIsABenchmarkMethodOfTheSvgClassThatReturnsTheSvg()
    {
        var methods = BenchmarksOf("MatplotlibThemeSvgBenchmarks.cs");

        foreach (var name in SvgRows)
            Assert.Contains(("string", name), methods);
    }

    [Fact]
    public void EveryPictureAndPdfRowIsABenchmarkMethodOfTheSkiaClassThatReturnsTheBytes()
    {
        var methods = BenchmarksOf("MatplotlibThemeSkiaBenchmarks.cs");

        foreach (var name in PngRows.Concat(PdfRows))
            Assert.Contains(("byte[]", name), methods);
    }

    [Fact]
    public void TheSkiaClassAlsoTimesTheAskOfTheRenderContextToPutAMarkOnThePixelGrid_WithAndWithoutSnapping()
    {
        var methods = BenchmarksOf("MatplotlibThemeSkiaBenchmarks.cs");

        foreach (var name in AskRows)
            Assert.Contains(("double", name), methods);
    }

    [Fact]
    public void TheSkiaClassHoldsNoSvgRowBecauseItsBenchmarksLoadSkiaWhichChangesHowSvgTextIsWritten()
    {
        var methods = BenchmarksOf("MatplotlibThemeSkiaBenchmarks.cs");

        Assert.DoesNotContain(methods, m => m.Type == "string");
    }

    /// <summary>The grid row without minor ticks is what a figure that asks for a minor grid alone draws: under a matplotlib theme
    /// the grid then stands on the major ticks only, and the row with minor ticks on draws the minor lines besides.</summary>
    [Fact]
    public void TheNoMinorTicksGridRowDrawsFewerGridLinesThanTheMinorTicksRow()
    {
        var figures = new MatplotlibThemeFigures();

        var withTicks = figures.Of("V2Grid3x3", MatplotlibThemeOutput.Svg).ToSvg();
        var without = figures.Of("V2MinorGridNoTicks3x3", MatplotlibThemeOutput.Svg).ToSvg();

        Assert.True(without.Length < withTicks.Length, $"{without.Length} against {withTicks.Length}");
        Assert.All(figures.Of("V2MinorGridNoTicks3x3", MatplotlibThemeOutput.Svg).SubPlots,
            subplot => Assert.False(subplot.XAxis.MinorTicks.Visible || subplot.YAxis.MinorTicks.Visible));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheInteractiveRowCarriesTheScriptThatTurnsItInTheBrowser", () => new MatplotlibThemeFigures().Of("V2Interactive3D", MatplotlibThemeOutput.Svg).ToSvg(), "<script"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void TheMatplotlibThemeRowsStandUnderTheThemeTheirNameSays()
    {
        var figures = new MatplotlibThemeFigures();

        foreach (var row in figures.Rows)
            Assert.Equal(row.Name.StartsWith("Classic", StringComparison.Ordinal) ? Theme.MatplotlibClassic : Theme.MatplotlibV2, row.Figure.Theme);
    }

    [Fact]
    public void TheFigureOfAnUnknownRowIsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => new MatplotlibThemeFigures().Of("V2Patches20", MatplotlibThemeOutput.Png));
    }

    [Theory]
    [InlineData("V2Grid3x3", 9)]
    [InlineData("V2MinorGridNoTicks3x3", 9)]
    [InlineData("V2LogGrid3x3", 9)]
    [InlineData("V2SevenPanelsFigLegend", 7)]
    [InlineData("V2Patches20", 1)]
    public void ChangingTheFigureChangesEverySubplotSoTheWholeFigureIsDrawnAndNoPanelIsHandedBackAsItWas(string row, int subplots)
    {
        var figure = new MatplotlibThemeFigures().Of(row, MatplotlibThemeOutput.Svg);

        figure.ChangedFor(0);

        Assert.Equal(subplots, figure.SubPlots.Count);
        Assert.All(figure.SubPlots, subplot => Assert.Equal("tick", subplot.Title));
    }

    [Fact]
    public void ChangingTheFigureBetweenDrawsMakesEveryDrawARealOne()
    {
        var figure = new MatplotlibThemeFigures().Of("V2Grid3x3", MatplotlibThemeOutput.Svg);

        var first = figure.ChangedFor(0).ToSvg();
        var second = figure.ChangedFor(1).ToSvg();

        Assert.NotEqual(first, second);
        Assert.Equal(first, figure.ChangedFor(2).ToSvg());
    }
}
