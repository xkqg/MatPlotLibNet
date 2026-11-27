// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A line contour and a triangle contour that name no levels take the same levels a filled contour takes, matplotlib's
/// (3.11.2, <c>ContourSet._process_contour_level_args</c>: an integer <c>levels</c> asks <c>_ensure_locator_exists(N)</c> for
/// <c>MaxNLocator(N + 1, min_n_ticks=1)</c> and <c>_autolev</c> trims what it lays to one level at or below the lowest value and one
/// at or above the highest; <c>contour</c>, <c>contourf</c>, <c>tricontour</c> and <c>tricontourf</c> share that code), so a contour
/// laid over a filled one with the same count draws its lines on the edges of the bands. Every expected level here is what
/// matplotlib prints for <c>.levels</c> of the same data; the levels of the peaks surface are pinned for a filled contour in
/// <see cref="ContourfAutoLevelsTests"/>.</summary>
public sealed class ContourLineAutoLevelsTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>z = 2x + 0.5 over x = 0..5 (values 0.5 to 10.5), four rows: the iso-line of level L is the vertical line
    /// x = (L - 0.5) / 2. No round level equals the lowest or the highest value, so no line stands on the grid's edge.</summary>
    private static readonly double[] RampX = [0, 1, 2, 3, 4, 5];
    private static readonly double[] RampY = [0, 1, 2, 3];

    private static ContourSeries Ramp(int levels)
    {
        var z = new double[RampY.Length, RampX.Length];
        for (int r = 0; r < RampY.Length; r++)
            for (int c = 0; c < RampX.Length; c++)
                z[r, c] = 2.0 * c + 0.5;
        return new ContourSeries(RampX, RampY, z) { Levels = levels };
    }

    private static ContourSeries Peaks()
    {
        var (x, y, z) = SampleSurface.Peaks();
        return new ContourSeries(x, y, z);
    }

    private static RecordingRenderContext Render(double xMax, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(Bounds.Linear(xMax: xMax), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    /// <summary>The x, in data units, of each line a line contour drew (the iso-lines of a ramp are vertical).</summary>
    private static double[] LineXs(RecordingRenderContext ctx, double xMax)
    {
        var transform = Bounds.Linear(xMax: xMax);
        return [.. ctx.Of<DrawLinesCall>().Select(call =>
        {
            var xs = call.Points.Select(p => Math.Round(transform.PixelToData(p).X, 6)).Distinct().ToArray();
            return Assert.Single(xs);
        })];
    }

    /// <summary>ax.contour(X, Y, 2 * X + 0.5, levels=4).levels is 0, 2, ..., 12; the lines of the five that cross the grid stand
    /// at x = 0.75, 1.75, ..., 4.75. The even spacing the library drew (four lines at 0.5 + 2x = 2.5, 4.5, ...) is gone.</summary>
    [Fact]
    public void ALineContour_NamingNoLevels_DrawsMatplotlibsLevels_ForACount()
    {
        var ctx = Render(5, c => new ContourSeriesRenderer(c).Render(Ramp(4)));

        Assert.Equal([0.75, 1.75, 2.75, 3.75, 4.75], LineXs(ctx, 5));
    }

    /// <summary>ax.contour(X, Y, 2 * X + 0.5, levels=10).levels is 0, 1, ..., 11: ten of them cross the grid.</summary>
    [Fact]
    public void ALineContour_NamingNoLevels_DrawsTenLinesForTheDefaultCount()
    {
        var ctx = Render(5, c => new ContourSeriesRenderer(c).Render(Ramp(10)));

        Assert.Equal(Enumerable.Range(1, 10).Select(level => (level - 0.5) / 2), LineXs(ctx, 5));
    }

    /// <summary>The levels the lines are drawn at are the ones a filled contour of the same count bands at: -0.8 to 1.2 in steps
    /// of 0.2 for <c>levels=10</c> of the peaks surface (ax.contour(X, Y, Z, levels=10).levels), in both styles.</summary>
    [Fact]
    public void ALineContour_AndAFilledContour_OfOneCount_ShareTheirLevels()
    {
        double[] expected = [-0.8, -0.6, -0.4, -0.2, 0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2];
        var peaks = Peaks();
        var filled = new ContourfSeries(peaks.XData, peaks.YData, peaks.ZData);

        AssertLevels(expected, peaks.LevelsUnder(Theme.MatplotlibV2)!);
        AssertLevels(expected, peaks.LevelsUnder(Theme.MatplotlibClassic)!);
        AssertLevels(filled.LevelsUnder(Theme.MatplotlibV2)!, peaks.LevelsUnder(Theme.MatplotlibV2)!);
    }

    /// <summary>The classic theme rounds an axis out to its ticks and matplotlib's locator reads that mode too: three levels asked
    /// of values from 2202.05 to 2412.91 are 2160 to 2460 in steps of 60 under the default style and 2160 to 2480 in steps of 80
    /// under the classic one (the same data as a filled contour's test).</summary>
    [Fact]
    public void ALineContour_UnderTheClassicTheme_TakesTheLargerStep()
    {
        var series = new ContourSeries([0.0, 1], [0.0, 1], new[,] { { 2202.046856090197, 2300 }, { 2350, 2412.9112426911925 } })
        {
            Levels = 3,
        };

        AssertLevels([2160, 2220, 2280, 2340, 2400, 2460], series.LevelsUnder(Theme.MatplotlibV2)!);
        AssertLevels([2160, 2240, 2320, 2400, 2480], series.LevelsUnder(Theme.MatplotlibClassic)!);
    }

    /// <summary>A count below one counts as one, as for a filled contour: ax.contour(X, Y, Z, levels=1).levels is 0, 5, 10 for
    /// the ramp of 0 to 10.</summary>
    [Fact]
    public void ALineContour_CountBelowOne_CountsAsOne()
    {
        var series = new ContourSeries(RampX, RampY, new double[RampY.Length, RampX.Length]);
        for (int r = 0; r < RampY.Length; r++)
            for (int c = 0; c < RampX.Length; c++)
                series.ZData[r, c] = 2.0 * c;

        series.Levels = 0;
        AssertLevels([0, 5, 10], series.LevelsUnder(Theme.MatplotlibV2)!);
        series.Levels = 1;
        AssertLevels([0, 5, 10], series.LevelsUnder(Theme.MatplotlibV2)!);
    }

    /// <summary>Levels that are named keep winning, and so do none that can be drawn.</summary>
    [Fact]
    public void ALineContour_WithNamedLevels_KeepsThem()
    {
        var series = Ramp(10);
        series.LevelValues = [9, 1, 5];

        AssertLevels([1, 5, 9], series.LevelsUnder(Theme.MatplotlibV2)!);

        series.LevelValues = [double.NaN];
        Assert.Empty(series.LevelsUnder(Theme.MatplotlibV2)!);
    }

    /// <summary>A grid whose values differ by less than 1e-12 is flat: no level is laid, as for a filled contour.</summary>
    [Fact]
    public void AFlatGrid_HasNoLevels()
    {
        var series = new ContourSeries([0.0, 1], [0.0, 1], new[,] { { 5.0, 5 }, { 5, 5 } });

        Assert.Null(series.LevelsUnder(Theme.MatplotlibV2));
    }

    /// <summary>ax.tricontour(x, y, x + 0.5, levels=4).levels, for ten points on a 5 x 2 grid, is 0, 0.8, 1.6, ..., 4.8: the five
    /// that cross the surface draw their lines at x = L - 0.5.</summary>
    [Fact]
    public void ATriangleContour_NamingNoLevels_DrawsMatplotlibsLevels()
    {
        double[] x = [0, 1, 2, 3, 4, 0, 1, 2, 3, 4], y = [0, 0, 0, 0, 0, 1, 1, 1, 1, 1];
        double[] z = [.. x.Select(v => v + 0.5)];
        var series = new TricontourSeries(x, y, z) { Levels = 4 };

        var ctx = Render(4, c => new TricontourSeriesRenderer(c).Render(series));

        var transform = Bounds.Linear(xMax: 4);
        var lineXs = ctx.Of<DrawLineCall>()
            .Select(call => Math.Round(transform.PixelToData(call.P1).X, 6))
            .Distinct().Order().ToArray();
        Assert.Equal([0.3, 1.1, 1.9, 2.7, 3.5], lineXs);
        AssertLevels([0, 0.8, 1.6, 2.4, 3.2, 4.0, 4.8], series.LevelsUnder(Theme.MatplotlibV2)!);
    }

    /// <summary>matplotlib colours a line by its level over the levels, the first to the last (<c>ContourSet._process_colors</c>:
    /// "we base the colormapping on the contour levels and layers, not on the actual range of the Z values";
    /// <c>norm.autoscale_None(self.levels)</c>), so the line of level 2 on the ramp of 0.5 to 10.5 asked for 4 levels (0 to 12) is
    /// viridis at 2/12, not at (2 - 0.5)/10. Levels the user names run over the levels too, the first to the last
    /// (<see cref="ContourLevelColourSpanTests"/>).</summary>
    [Fact]
    public void ALineContour_ColoursItsLinesOverTheLevels_ForACount_AndForNamedLevels()
    {
        var counted = Ramp(4);
        counted.ColorMap = ColorMaps.Viridis;
        var ctx = Render(5, c => new ContourSeriesRenderer(c).Render(counted));
        Assert.Equal(ColorMaps.Viridis.ColorOf(2, LinearNormalizer.Instance, 0, 12), ctx.Of<DrawLinesCall>()[0].Color);
        Assert.Equal(ColorMaps.Viridis.ColorOf(10, LinearNormalizer.Instance, 0, 12), ctx.Of<DrawLinesCall>()[^1].Color);

        var named = Ramp(4);
        named.ColorMap = ColorMaps.Viridis;
        named.LevelValues = [2, 4];
        ctx = Render(5, c => new ContourSeriesRenderer(c).Render(named));
        Assert.Equal(ColorMaps.Viridis.ColorOf(2, LinearNormalizer.Instance, 2, 4), ctx.Of<DrawLinesCall>()[0].Color);
    }

    /// <summary>A line contour that is filled and names no levels bands over the levels laid for the count, as a filled contour
    /// does, so the two paint the same bands in the same colours.</summary>
    [Fact]
    public void AFilledLineContour_NamingNoLevels_PaintsTheBandsOfAFilledContour()
    {
        var line = Ramp(4);
        line.Filled = true;
        line.ColorMap = ColorMaps.Viridis;
        var filled = new ContourfSeries(line.XData, line.YData, line.ZData) { Levels = 4, ColorMap = ColorMaps.Viridis, ShowLines = false };

        var fromLine = Render(5, c => new ContourSeriesRenderer(c).Render(line));
        var fromFilled = Render(5, c => new ContourfSeriesRenderer(c).Render(filled));

        static List<Color?> Fills(RecordingRenderContext ctx) =>
            [.. ctx.Of<ShapeCall>().Where(c => c is DrawRectangleCall or DrawPathCall).Select(c => c.Fill)];
        Assert.Equal(Fills(fromFilled), Fills(fromLine));
        Assert.True(Fills(fromLine).Count >= 2);
    }

    /// <summary>The lines of a triangle contour take their colours over the levels, as a contour of a grid does: the line of level 0.8
    /// on the surface of 0.5 to 4.5 asked for 4 levels (0 to 4.8) is viridis at 0.8/4.8.</summary>
    [Fact]
    public void ATriangleContour_ColoursItsLinesOverTheLevels()
    {
        double[] x = [0, 1, 2, 3, 4, 0, 1, 2, 3, 4], y = [0, 0, 0, 0, 0, 1, 1, 1, 1, 1];
        double[] z = [.. x.Select(v => v + 0.5)];
        var series = new TricontourSeries(x, y, z) { Levels = 4, ColorMap = ColorMaps.Viridis };

        var ctx = Render(4, c => new TricontourSeriesRenderer(c).Render(series));

        var transform = Bounds.Linear(xMax: 4);
        var first = ctx.Of<DrawLineCall>().First(call => Math.Round(transform.PixelToData(call.P1).X, 6) < 0.5);
        Assert.Equal(ColorMaps.Viridis.ColorOf(0.8, LinearNormalizer.Instance, 0, 4.8), first.Color);
    }

    /// <summary>A triangle contour whose normaliser is the logarithmic one takes matplotlib's LogLocator levels as well:
    /// <c>tricontour(..., norm=LogNorm())</c> of values from 1 to 1000 asked for 10 levels draws the decades 0.1 to 10000.</summary>
    [Fact]
    public void ALogNormalisedTriangleContour_TakesTheDecades()
    {
        double[] x = [0, 1, 2, 3, 4, 0, 1, 2, 3, 4], y = [0, 0, 0, 0, 0, 1, 1, 1, 1, 1];
        double[] z = [1, 3, 10, 100, 1000, 2, 20, 30, 300, 500];
        var series = new TricontourSeries(x, y, z) { Normalizer = new LogNormalizer() };

        var levels = series.LevelsUnder(Theme.MatplotlibV2)!;

        double[] expected = [0.1, 1, 10, 100, 1000, 10000];
        Assert.Equal(expected.Length, levels.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], levels[i], expected[i] * 1e-12);
    }

    /// <summary>Values that differ by less than 1e-12 are flat: a line contour and a triangle contour that name no level draw
    /// nothing, as a filled contour does.</summary>
    [Fact]
    public void AGridOrSurfaceOfNearlyEqualValues_DrawsNoLine()
    {
        var grid = new ContourSeries([0.0, 1], [0.0, 1], new[,] { { 5.0, 5 + 1e-13 }, { 5, 5 } });
        double[] x = [0, 1, 0, 1], y = [0, 0, 1, 1], z = [5, 5 + 1e-13, 5, 5];
        var triangles = new TricontourSeries(x, y, z);

        Assert.Empty(Render(1, c => new ContourSeriesRenderer(c).Render(grid)).Calls);
        Assert.Empty(Render(1, c => new TricontourSeriesRenderer(c).Render(triangles)).Calls);
    }

    private static void AssertLevels(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], actual[i], 9);
    }
}
