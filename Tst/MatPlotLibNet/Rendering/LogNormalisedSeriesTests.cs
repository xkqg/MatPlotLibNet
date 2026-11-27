// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series whose normaliser is the <see cref="LogNormalizer"/> is coloured as matplotlib's <c>LogNorm</c> colours it under the
/// matplotlib themes: <c>log(value / vmin) / log(vmax / vmin)</c> over the range, a value of 0 or below masked, and the layer of a
/// filled contour's band at the geometric mean of its two levels (contour.py <c>_process_layers</c>: <c>sqrt(lower) * sqrt(upper)</c>,
/// with <c>norm.autoscale_None(levels)</c>). The library's own themes keep the documented formula of the <see cref="LogNormalizer"/>.</summary>
public sealed class LogNormalisedSeriesTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(Theme theme, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(new DataTransform(0, 4, 0, 4, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)) { Theme = theme });
        return ctx;
    }

    private static Color Viridis(double fraction) => ColorMaps.Viridis.GetColor(fraction);

    private static List<Color?> Fills(RecordingRenderContext ctx, params Type[] kinds) =>
        [.. ctx.Calls.OfType<ShapeCall>().Where(c => kinds.Contains(c.GetType())).Select(c => c.Fill)];

    // ---- a heatmap ---------------------------------------------------------------------------------------------------------

    private static readonly double[,] Cells = { { 2, 20, 200 } };

    [Fact]
    public void AHeatmapCell_IsColouredByLogNorm_UnderV2()
    {
        var series = new HeatmapSeries(Cells) { Normalizer = new LogNormalizer(), ColorMap = ColorMaps.Viridis };

        var ctx = Render(Theme.MatplotlibV2, c => new HeatmapSeriesRenderer(c).Render(series));

        // log(20 / 2) / log(200 / 2) = 0.5 where the library's formula gives log(19) / log(199) = 0.5563.
        Assert.Equal([Viridis(0), Viridis(0.5), Viridis(1)], Fills(ctx, typeof(DrawRectangleCall)));
    }

    // ---- a filled contour --------------------------------------------------------------------------------------------------

    private static ContourfSeries Decades(double[,]? z = null)
    {
        double[] axis = [0, 1, 2, 3];
        var grid = z ?? new double[,] { { 1, 2, 5, 8 }, { 3, 20, 60, 90 }, { 7, 80, 300, 700 }, { 9, 100, 500, 1000 } };
        return new ContourfSeries(axis, axis, grid) { Normalizer = new LogNormalizer(), ColorMap = ColorMaps.Viridis, ShowLines = false };
    }

    /// <summary>matplotlib 3.11.2 lays the levels 0.1, 1, 10, 100, 1000 and 10000 over values from 1 to 1000 (printed: <c>contourf(..., norm=LogNorm()).levels</c>),
    /// colours by <c>norm.autoscale_None(levels)</c> (printed: vmin 0.1, vmax 10000) and takes a band's colour at the geometric mean of its levels
    /// (printed: <c>cvalues</c> 0.316, 3.16, 31.6, 316, 3162): log10 of the mean over 5 decades from the first level is 0.1, 0.3, 0.5, 0.7 and 0.9. The
    /// band above 1000 holds no value (1000 itself is below its level, as contourpy takes it), so four bands are drawn.</summary>
    [Fact]
    public void TheBandsOfALogContour_AreColouredAtTheGeometricMean_UnderV2()
    {
        var ctx = Render(Theme.MatplotlibV2, c => new ContourfSeriesRenderer(c).Render(Decades()));

        var fills = Fills(ctx, typeof(DrawRectangleCall), typeof(DrawPathCall));
        Assert.Equal([Viridis(0.1), Viridis(0.3), Viridis(0.5), Viridis(0.7)], fills);
    }

    [Fact]
    public void TheBandsOfALogContour_KeepTheArithmeticMiddle_UnderTheLibrarysOwnTheme()
    {
        var ctx = Render(Theme.Default, c => new ContourfSeriesRenderer(c).Render(Decades()));

        double[] levels = [0.1, 1, 10, 100, 1000, 10000];
        var expected = Enumerable.Range(0, 4).Select(b => (Color?)ColorMaps.Viridis.ColorOf(levels[b] + (levels[b + 1] - levels[b]) * 0.5,
            new LogNormalizer(), 0.1, 10000)).ToList();
        Assert.Equal(expected, Fills(ctx, typeof(DrawRectangleCall), typeof(DrawPathCall)));
    }

    /// <summary>A value at 0 or below is masked, not painted in the lowest band: the cells it is a corner of are left empty, as
    /// matplotlib's contour generator leaves them.</summary>
    [Fact]
    public void AValueAtOrBelowZero_IsMasked_UnderV2_AndPaintedInTheLowestBand_UnderTheLibrarysOwnTheme()
    {
        var z = new double[,] { { 1, 2, 5, 8 }, { 3, 20, 60, 90 }, { 7, 80, 0, 700 }, { 9, 100, 500, 1000 } };

        int masked = Fills(Render(Theme.MatplotlibV2, c => new ContourfSeriesRenderer(c).Render(Decades(z))), typeof(DrawRectangleCall)).Count;
        int whole = Fills(Render(Theme.Default, c => new ContourfSeriesRenderer(c).Render(Decades(z))), typeof(DrawRectangleCall)).Count;

        Assert.True(masked > 1, "the lowest band is laid cell by cell around the masked cells");
        Assert.Equal(1, whole);
    }

    /// <summary>A log filled contour whose named levels cannot be drawn (none is a number) has no band to colour: it draws nothing, as a linear one does,
    /// and does not read the first of no levels.</summary>
    [Fact]
    public void ALogFilledContour_WithNoDrawableLevel_DrawsNothing_UnderV2()
    {
        var series = Decades();
        series.LevelValues = [double.NaN];

        var ctx = Render(Theme.MatplotlibV2, c => new ContourfSeriesRenderer(c).Render(series));

        Assert.Empty(Fills(ctx, typeof(DrawRectangleCall), typeof(DrawPathCall)));
    }

    // ---- a triangle contour ------------------------------------------------------------------------------------------------

    private static TricontourSeries Bump(double lowest) => new(
        new Vec([0.0, 1.0, 0.5, 1.0, 0.0, 0.5]), new Vec([0.0, 0.0, 0.5, 1.0, 1.0, 0.25]), new Vec([lowest, 20, 300, 20, 4, 30]))
    {
        Normalizer = new LogNormalizer(),
        ColorMap = ColorMaps.Viridis,
    };

    [Fact]
    public void ALogTriangleContour_ColoursItsLinesByLogNorm_OverTheLevels_UnderV2()
    {
        var ctx = Render(Theme.MatplotlibV2, c => new TricontourSeriesRenderer(c).Render(Bump(2)));

        var colours = ctx.Of<DrawLineCall>().Select(c => c.Color).Distinct().ToList();
        double[] levels = Bump(2).LevelsUnder(Theme.MatplotlibV2)!;
        Assert.NotEmpty(colours);
        Assert.All(colours, colour => Assert.Contains(colour, levels.Select(level =>
            ColorMaps.Viridis.ColorOf(level, LogNorm.Instance, levels[0], levels[^1]))));
    }

    [Fact]
    public void ATriangleWithAVertexAtOrBelowZero_IsMasked_UnderV2()
    {
        int whole = Render(Theme.MatplotlibV2, c => new TricontourSeriesRenderer(c).Render(Bump(2))).Count<DrawLineCall>();
        int masked = Render(Theme.MatplotlibV2, c => new TricontourSeriesRenderer(c).Render(Bump(0))).Count<DrawLineCall>();

        Assert.True(masked < whole, $"a vertex of 0 masks the triangles it is a corner of: {masked} lines of {whole}");
    }

    // ---- every other series that colours its values through a normaliser ---------------------------------------------------------

    [Fact]
    public void AScatterColouredByValue_IsColouredByLogNorm_UnderV2()
    {
        var series = new ScatterSeries([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
        {
            C = [2.0, 20.0, 200.0], ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer(),
        };

        var ctx = Render(Theme.MatplotlibV2, c => new ScatterSeriesRenderer(c).Render(series));

        var fills = ctx.Fills();
        Assert.Contains(Viridis(0.5), fills);
        Assert.DoesNotContain(Viridis(Math.Log(19) / Math.Log(199)), fills);
    }

    [Fact]
    public void ATripcolorTriangle_IsColouredByLogNorm_UnderV2()
    {
        var series = new TripcolorSeries(new Vec([0.0, 1.0, 0.5]), new Vec([0.0, 0.0, 1.0]), new Vec([2.0, 20.0, 200.0]))
        {
            ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer(),
        };

        var ctx = Render(Theme.MatplotlibV2, c => new TripcolorSeriesRenderer(c).Render(series));

        // The one triangle takes the mean of its three values, 74, over 2 to 200.
        Assert.Equal([Viridis(Math.Log(74.0 / 2) / Math.Log(200.0 / 2))], ctx.Fills());
    }

    [Fact]
    public void ATreemapNode_IsColouredByLogNorm_UnderV2()
    {
        var root = new TreeNode
        {
            Label = "root",
            Children =
            [
                new TreeNode { Label = "a", Value = 1, ColorValue = 2 },
                new TreeNode { Label = "b", Value = 1, ColorValue = 8 },
                new TreeNode { Label = "c", Value = 1, ColorValue = 200 },
            ],
        };
        var series = new TreemapSeries(root) { ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer() };

        var ctx = Render(Theme.MatplotlibV2, c => new TreemapSeriesRenderer(c).Render(series));

        // log(8 / 2) / log(200 / 2) = 0.301, where the sibling ramp would give the middle one 0.5 and the library's formula 0.367.
        Assert.Contains(Viridis(Math.Log(4) / Math.Log(100)), ctx.Fills());
    }
}
