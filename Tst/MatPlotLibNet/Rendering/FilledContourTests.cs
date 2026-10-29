// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A filled line contour and a Contourf fill over the grid's points, in bands between the levels, and they do it by
/// one code: the fill stands where the grid stands, follows the axes' range and the limits the user set, and its edges are
/// the iso-lines. Worked out by hand on the grid x = 0..4, y = 0..4, z = x + y (0 at a corner, 8 at the opposite one), plotted
/// in the plot (100, 50, 400, 300) under the limits x 1..3 and y 0..4: x maps to 100 + 200 · (x - 1) and y to 350 - 75 · y.</summary>
public class FilledContourTests
{
    private static readonly Rect Plot = new(100, 50, 400, 300);
    private static readonly double[] Grid = [0, 1, 2, 3, 4];

    private static double[,] Sum()
    {
        var z = new double[5, 5];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
                z[r, c] = Grid[c] + Grid[r];
        return z;
    }

    private static double[,] YRamp()
    {
        var z = new double[5, 5];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
                z[r, c] = Grid[r];
        return z;
    }

    private static RecordingRenderContext Render(Action<SeriesRenderContext> draw, DataTransform? transform = null)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(transform ?? new DataTransform(1, 3, 0, 4, Plot), ctx, Colors.Tab10Blue, new RenderArea(Plot, ctx)));
        return ctx;
    }

    private static IEnumerable<Point> Vertices(RecordingRenderContext ctx, string kind) =>
        ctx.OfKind(kind).SelectMany(c => (Point[])((dynamic)c.Args!).points);

    private static bool Same(Point a, Point b) => Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(a.Y - b.Y) < 1e-9;

    // ---- a band of no width ---------------------------------------------------------------------------------------------

    [Fact]
    public void TwoLevelsCloserThanTheSpacingOfDoublesCanTell_BoundABandOfNoWidth_AndNothingIsFilled()
    {
        // 1 and 1 + 1e-14 are two doubles, so the levels are two, and the one band they bound is as wide as nothing: there is no
        // colour to take from it and nothing to paint.
        var ctx = Render(c => new ContourfSeriesRenderer(c).Render(new ContourfSeries(Grid, Grid, Sum()) { LevelValues = [1.0, 1.0 + 1e-14] }));

        Assert.Empty(ctx.OfKind("DrawRectangle"));
        Assert.Empty(ctx.OfKind("DrawPolygon"));
        Assert.Empty(ctx.OfKind("DrawLines"));    // the lines would stand on levels that are one level, in a colour of no range
    }

    // ---- the fill stands where the grid stands --------------------------------------------------------------------------

    [Fact]
    public void TheLowestBandOfAFilledContour_FillsTheGridAndNotThePlot()
    {
        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, Sum()) { Filled = true, Levels = 3 }));

        // The grid runs from x = 0 to 4: 100 + 200 · (0 - 1) = -100 to 100 + 200 · 3 = 700 (clipped by the plot, not by the fill).
        var bottom = Assert.Single(ctx.OfKind("DrawRectangle"));
        Assert.Equal(new Rect(-100, 50, 800, 300), (Rect)((dynamic)bottom.Args!).rect);
    }

    [Fact]
    public void TheLowestBandOfAContourf_FillsTheGridAndNotThePlot_UnderLimitsNarrowerThanTheGrid()
    {
        var ctx = Render(c => new ContourfSeriesRenderer(c).Render(new ContourfSeries(Grid, Grid, Sum()) { Levels = 4 }),
            new DataTransform(0, 10, 0, 4, Plot));

        // The limits run to 10 and the grid to 4: the fill ends at 100 + 400 · 4 / 10 = 260, where the grid ends.
        var bottom = Assert.Single(ctx.OfKind("DrawRectangle"));
        Assert.Equal(new Rect(100, 50, 160, 300), (Rect)((dynamic)bottom.Args!).rect);
    }

    // ---- the fill follows the lines --------------------------------------------------------------------------------------

    [Fact]
    public void EveryVertexOfAnIsoLine_IsAVertexOfTheFill()
    {
        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, Sum()) { Filled = true, Levels = 3 }));

        var fill = Vertices(ctx, "DrawPolygon").ToList();
        var lines = Vertices(ctx, "DrawLines").ToList();
        Assert.NotEmpty(lines);
        Assert.All(lines, vertex => Assert.Contains(fill, f => Same(f, vertex)));
    }

    [Fact]
    public void TheBands_AreBetweenTheLevelsTheLinesAreTracedAt()
    {
        // The levels 2, 4 and 6 of the range 0..8 bound four bands.
        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, Sum()) { Filled = true, Levels = 3 }));

        var fills = ctx.OfKind("DrawPolygon").Select(c => ((dynamic)c.Args!).fill).Distinct().Count();
        Assert.Equal(4, fills);
    }

    [Fact]
    public void TheBands_FollowTheLevelsTheUserNamed()
    {
        // Named 3 and 5 bound three bands: 0..3, 3..5 and 5..8.
        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, Sum()) { Filled = true, LevelValues = [3, 5] }));

        Assert.Equal(3, ctx.OfKind("DrawPolygon").Select(c => ((dynamic)c.Args!).fill).Distinct().Count());
    }

    [Fact]
    public void ALevelOutsideTheRangeOfTheGrid_BoundsNoBand()
    {
        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, Sum()) { Filled = true, LevelValues = [-5, 4, 99] }));

        // Only 4 lies inside 0..8: two bands.
        Assert.Equal(2, ctx.OfKind("DrawPolygon").Select(c => ((dynamic)c.Args!).fill).Distinct().Count());
    }

    // ---- the fill follows the axes ---------------------------------------------------------------------------------------

    [Fact]
    public void TheHighestBand_StandsAtTheTopOfThePlot_WhereTheValuesAreHighest()
    {
        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, YRamp()) { Filled = true, Levels = 2 }));

        // z = y: the levels 4/3 and 8/3 of the range 0..4 bound three bands; the last painted is the highest, y from 8/3 to 4,
        // which is 350 - 75 · 8/3 = 150 up to 50.
        var polygons = ctx.OfKind("DrawPolygon").Select(c => (Point[])((dynamic)c.Args!).points).ToList();
        var highest = polygons[^1];
        Assert.All(highest, p => Assert.InRange(p.Y, 50 - 1e-9, 150 + 1e-9));
    }

    [Fact]
    public void ReversedLimits_FillTheSameGridTheOtherWayRound()
    {
        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, Sum()) { Filled = true, Levels = 3 }),
            new DataTransform(3, 1, 0, 4, Plot));

        // x = 0 maps to 100 + 200 · (0 - 3) / (1 - 3) · ... : with the X axis reversed the grid still fills from its own corners.
        var bottom = (Rect)((dynamic)Assert.Single(ctx.OfKind("DrawRectangle")).Args!).rect;
        double left = new DataTransform(3, 1, 0, 4, Plot).DataToPixel(4, 0).X, right = new DataTransform(3, 1, 0, 4, Plot).DataToPixel(0, 0).X;
        Assert.Equal(left, bottom.X, 9);
        Assert.Equal(right - left, bottom.Width, 9);
    }

    // ---- a missing value -------------------------------------------------------------------------------------------------

    [Fact]
    public void AMissingCell_IsLeftEmpty_NotPaintedWithAColourOfTheMapThatNobodyMeasured()
    {
        var z = Sum();
        z[1, 1] = double.NaN;

        var ctx = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(Grid, Grid, z) { Filled = true, Levels = 3 }));

        // The four cells that have the missing value as a corner are not filled: 16 - 4 = 12 cells of the lowest band.
        Assert.Equal(12, ctx.CountOf("DrawRectangle"));
    }

    // ---- what is written -------------------------------------------------------------------------------------------------

    [Fact]
    public void AFilledContour_ReadBackFromJson_DrawsTheSamePicture()
    {
        var figure = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Contour(Grid, Grid, Sum(), s => { s.Filled = true; s.Levels = 3; })
            .SetXLim(1, 3)).Build();
        var serializer = new ChartSerializer();

        Assert.Equal(figure.ToSvg(), serializer.FromJson(serializer.ToJson(figure)).ToSvg());
    }
}
