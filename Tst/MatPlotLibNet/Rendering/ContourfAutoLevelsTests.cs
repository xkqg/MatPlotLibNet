// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A filled contour that names no levels takes matplotlib's: <c>contourf(X, Y, Z, levels=10)</c> of the peaks surface
/// bands at -0.8, -0.6, ..., 1.2 (matplotlib 3.11.2 prints <c>cs.levels</c> so, and <c>cs.norm.vmin, cs.norm.vmax</c> -0.8 and 1.2),
/// and its colours and its colour bar run over those levels, not over the values (-0.685 to 1.056): <c>ContourSet._process_colors</c>,
/// "we base the colormapping on the contour levels and layers, not on the actual range of the Z values", each band at the middle
/// of its two levels. The levels themselves are pinned in <see cref="TickLocators.SteppedTicksTests"/>.</summary>
public sealed class ContourfAutoLevelsTests
{
    private static ContourfSeries Peaks()
    {
        var (x, y, z) = SampleSurface.Peaks();
        return new ContourfSeries(x, y, z) { ColorMap = ColorMaps.Viridis };
    }

    [Fact]
    public void TheColourBar_RunsOverTheLevels()
    {
        var (min, max) = Peaks().GetColorBarRange();

        Assert.Equal(-0.8, min, 12);
        Assert.Equal(1.2, max, 12);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // The lowest band, -0.8 to -0.6, is painted in viridis at 0.05 of the way from -0.8 to 1.2; the highest, 1.0 to 1.2, at 0.95.
        new DrawnToken("TheBands_TakeTheirColourFromTheLevels", () => Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(Peaks())).ToSvg(), BandFill(0), BandFill(-2)),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The attribute that paints band <paramref name="band"/> (counted from the end when negative, so -2 is the highest band, whose upper
    /// level is the last one).</summary>
    private static string BandFill(int band)
    {
        var levels = Peaks().LevelsUnder(Theme.MatplotlibV2)!;
        return $"fill=\"{BandColour(levels, band < 0 ? levels.Length + band : band).ToHex()}\"";
    }

    /// <summary>The colour of band <paramref name="band"/>: viridis at the middle of its two levels, from the first level to the
    /// last.</summary>
    private static Color BandColour(double[] levels, int band)
    {
        double mid = levels[band] + (levels[band + 1] - levels[band]) * 0.5;
        return ColorMaps.Viridis.GetColor((mid - levels[0]) / (levels[^1] - levels[0]));
    }

    /// <summary>The classic theme rounds an axis out to its ticks, and matplotlib's locator reads that mode too
    /// (<c>MaxNLocator._raw_ticks</c>: "Classic round_numbers mode may require a larger step"): three levels asked of values
    /// from 2202.05 to 2412.91 are 2160 to 2460 in steps of 60 under the default style and 2160 to 2480 in steps of 80 under the
    /// classic one. The bar's range follows the theme it is drawn under.</summary>
    [Fact]
    public void TheClassicTheme_TakesTheLargerStep_ItsLocatorAsksFor()
    {
        var series = new ContourfSeries([0.0, 1], [0.0, 1], new[,] { { 2202.046856090197, 2300 }, { 2350, 2412.9112426911925 } })
        {
            Levels = 3,
        };
        IColorBarDataProvider provider = series;

        Assert.Equal(new(2160, 2460), provider.ColorBarRangeUnder(Theme.MatplotlibV2));
        Assert.Equal(new(2160, 2480), provider.ColorBarRangeUnder(Theme.MatplotlibClassic));
        Assert.Equal(new(2160, 2460), series.GetColorBarRange());
    }

    /// <summary>Levels named by the author keep winning, and the colours run over them.</summary>
    [Fact]
    public void NamedLevels_KeepWinning()
    {
        var series = Peaks();
        series.LevelValues = [-1, 0, 1, 2];

        Assert.Equal(new(-1, 2), series.GetColorBarRange());
    }

    /// <summary>A flat grid has no levels to lay: the bar keeps the values' range as before, and nothing is filled.</summary>
    [Fact]
    public void AFlatGrid_KeepsTheValuesRange()
    {
        var series = new ContourfSeries([0.0, 1], [0.0, 1], new[,] { { 5.0, 5 }, { 5, 5 } });

        Assert.Equal(new(5, 5), series.GetColorBarRange());
    }

    /// <summary>A filled contour whose normaliser is the logarithmic one takes matplotlib's LogLocator levels, as
    /// <c>contourf(..., norm=LogNorm())</c> does (<c>ContourSet.__init__</c>: <c>logscale</c>, <c>_ensure_locator_exists</c>): values from
    /// 1 to 1000 asked for 10 levels have the decades 0.1 to 10000, and its colour bar runs over them. A value of 0 or below is no
    /// place on a log scale: it does not set the lowest level (matplotlib masks it and takes the lowest of the rest).</summary>
    [Fact]
    public void ALogNormalisedFilledContour_TakesTheDecades()
    {
        var series = new ContourfSeries([0.0, 1, 2], [0.0, 1], new[,] { { 0.0, 1, 10 }, { 100, 1000, 500 } }) { Normalizer = new LogNormalizer() };

        double[] expected = [0.1, 1, 10, 100, 1000, 10000];
        var levels = series.LevelsUnder(Theme.MatplotlibV2)!;
        Assert.Equal(expected.Length, levels.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], levels[i], expected[i] * 1e-12);
        Assert.Equal(new(0.1, 10000), series.GetColorBarRange());

        series.Normalizer = null;
        Assert.NotEqual(expected.Length, series.LevelsUnder(Theme.MatplotlibV2)!.Length);
    }

    /// <summary>The decades are the levels the bands and their lines stand on: z = 10^x over x = 0 to 3 puts the iso-line of level 10 at
    /// x = 1 and that of 100 at x = 2 (levels of a linear locator, 0, 100, 200, ..., put none at x = 1).</summary>
    [Fact]
    public void ALogNormalisedFilledContour_DrawsItsLinesOnTheDecades()
    {
        double[] x = [0, 1, 2, 3], y = [0, 1, 2];
        var z = new double[3, 4];
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 4; c++)
                z[r, c] = Math.Pow(10, c);
        var series = new ContourfSeries(x, y, z) { Normalizer = new LogNormalizer(), ShowLines = true };
        var bounds = new Rect(0, 0, 300, 200);
        var transform = new DataTransform(0, 3, 0, 2, bounds);
        var ctx = new RecordingRenderContext();

        new ContourfSeriesRenderer(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx))).Render(series);

        var lineXs = ctx.Of<DrawLinesCall>()
            .Select(call => Math.Round(transform.PixelToData(call.Points[0]).X, 6))
            .ToList();
        Assert.Contains(1.0, lineXs);
        Assert.Contains(2.0, lineXs);
    }
}
