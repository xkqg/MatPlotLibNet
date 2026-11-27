// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>How a streamplot is drawn from its layout (the layout facts below). matplotlib 3.11.2's <c>streamplot</c> adds the
/// streamlines as one <c>LineCollection</c> at <c>lines.linewidth</c> (printed: 1.5 pt under the default style, 1.0 pt under the classic one),
/// then one <c>FancyArrowPatch</c> for each, from its tail to its head, with <c>arrowstyle='-|>'</c> and <c>mutation_scale = 10 * arrowsize</c>:
/// a filled head 0.4 of the scale long and 0.2 of it wide on each side, both ends of the arrow pulled in by <c>shrinkA = shrinkB = 2</c> pt
/// and the tip pulled back half the line width over the sine of the head's half angle, so its mitred edge ends where the path does
/// (printed for the first arrow under the default style: tail (388.576, 211.260), head (378.611, 209.316), the head's corners
/// (389.605, 208.631), (383.621, 210.294), (388.542, 214.084)). Every part is clipped to the axes, and the lines' sticky edges are the
/// grid's, so the axes span the grid with no margin (printed: <c>xlim (-3.0, 3.0)</c>).</summary>
public class StreamplotSeriesRendererTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>The streamlines the renderer lays out for <paramref name="series"/>, before anything is drawn.</summary>
    private static IReadOnlyList<StreamplotSeriesRenderer.Streamline> Streamlines(StreamplotSeries series)
    {
        var ctx = new RecordingRenderContext();
        return new StreamplotSeriesRenderer(new SeriesRenderContext(new DataTransform(-3, 3, -3, 3, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)))
            .StreamlinesOf(series);
    }

    private static RecordingRenderContext Render(StreamplotSeries series, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        var transform = new DataTransform(-3, 3, -3, 3, Bounds);
        new StreamplotSeriesRenderer(new SeriesRenderContext(transform, ctx, theme.CycleColors[0], new RenderArea(Bounds, ctx)) { Theme = theme }).Render(series);
        return ctx;
    }

    [Theory]
    [InlineData("v2", 1.5)]
    [InlineData("classic", 1.0)]
    public void UnderAMatplotlibTheme_EveryStreamlineIsALineAsWideAsTheThemesLines(string theme, double points)
    {
        var lines = Render(SampleSeries.RotationalField(), theme.AsTheme()).Of<DrawLinesCall>();

        Assert.Equal(71, lines.Count);
        Assert.All(lines, line => Assert.Equal(points.PointsInPixels, line.Thickness, 9));
    }

    /// <summary>Each arrow is matplotlib's <c>-|></c> head on the streamline from its tail to its head: the tip 2 pt plus the mitre's pad short
    /// of the head (5.104 px at a line of 1.5 pt, as printed), the head 4 pt long and 4 pt across (5.556 px), filled and edged in the
    /// streamline's colour at its width.</summary>
    [Fact]
    public void UnderAMatplotlibTheme_EveryArrowIsMatplotlibsFilledHead()
    {
        var series = SampleSeries.RotationalField();
        var ctx = Render(series, Theme.MatplotlibV2);
        var transform = new DataTransform(-3, 3, -3, 3, Bounds);
        var first = Streamlines(series)[0];
        var tail = transform.DataToPixel(first.ArrowTail.X, first.ArrowTail.Y);
        var head = transform.DataToPixel(first.ArrowHead.X, first.ArrowHead.Y);

        var arrows = ctx.Of<DrawPolygonCall>();
        Assert.Equal(71, arrows.Count);
        var corners = arrows[0].Points;
        Assert.Equal(3, corners.Length);
        double lineWidth = 1.5.PointsInPixels;
        double pad = 0.5 * lineWidth / (2 / Math.Sqrt(20));
        Assert.Equal(2.0.PointsInPixels + pad, corners[1].DistanceTo(head), 6);
        Assert.Equal(tail.DistanceTo(head) - 2.0.PointsInPixels - pad, corners[1].DistanceTo(tail), 6);
        Assert.Equal(4.0.PointsInPixels, corners[0].DistanceTo(corners[2]), 6);
        Assert.Equal(corners[0].DistanceTo(corners[1]), corners[2].DistanceTo(corners[1]), 6);
        Assert.Equal(Math.Sqrt(16 + 4) * 1.0.PointsInPixels, corners[0].DistanceTo(corners[1]), 6);
        Assert.Equal(Theme.MatplotlibV2.CycleColors[0], arrows[0].Fill);
        Assert.Equal(Theme.MatplotlibV2.CycleColors[0], arrows[0].Stroke);
        Assert.Equal(lineWidth, arrows[0].StrokeThickness, 9);
    }

    [Fact]
    public void TheArrowSize_ScalesTheHead()
    {
        var series = SampleSeries.RotationalField();
        series.ArrowSize = 2;

        var corners = (Render(series, Theme.MatplotlibV2).Of<DrawPolygonCall>()[0]).Points;

        Assert.Equal(8.0.PointsInPixels, corners[0].DistanceTo(corners[2]), 6);
    }

    /// <summary>The lines are one collection drawn before the arrows, and all of it inside one clip of the plot area, which the series
    /// pass pushes (the one clip rule: the arrow of a line stands past the grid, so the library's own themes clip it too); the renderer
    /// pushes none of its own.</summary>
    [Theory]
    [MemberData(nameof(BothKindsOfTheme))]
    public void TheStreamlinesAreDrawnFirst_ThenTheArrows_InsideOneClipOfThePlot(Theme theme)
    {
        Assert.DoesNotContain(Render(SampleSeries.RotationalField(), theme).Calls, c => c is PushClipCall);

        var ctx = new RecordingRenderContext();
        var series = SampleSeries.RotationalField();
        var axes = new Axes();
        axes.AddSeries(series);
        var host = new TestLayerHost([0]);
        SeriesPass.OverPlot(axes, new RenderPass(Bounds, ctx, theme), new DataTransform(-3, 3, -3, 3, Bounds))
            .Draw(ref host);
        var kinds = ctx.Calls.Select(c => c.Kind).ToList();

        Assert.Equal("PushClip", kinds[0]);
        Assert.Equal("PopClip", kinds[^1]);
        Assert.Equal(1, kinds.Count(k => k == "PushClip"));
        int lines = kinds.Count(k => k == "DrawLines");
        Assert.Equal(71, lines);
        Assert.Equal(Enumerable.Repeat("DrawLines", 71), kinds.Skip(1).Take(71));
    }

    public static TheoryData<Theme> BothKindsOfTheme => new() { Theme.MatplotlibV2, Theme.Default };

    /// <summary>The library's own themes keep their look on the same layout: lines 1 pixel wide and an open head of two strokes 6 pixels long.</summary>
    [Fact]
    public void TheLibrarysThemes_DrawThinLinesAndOpenHeads()
    {
        var ctx = Render(SampleSeries.RotationalField(), Theme.Default);

        Assert.All(ctx.Of<DrawLinesCall>(), line => Assert.Equal(1.0, line.Thickness));
        var strokes = ctx.Of<DrawLineCall>();
        Assert.Equal(2 * 71, strokes.Count);
        Assert.All(strokes, stroke => Assert.Equal(6, stroke.P1.DistanceTo(stroke.P2), 9));
        Assert.Equal(0, ctx.Count<DrawPolygonCall>());
    }

    /// <summary>An arrow shorter than the 2 pt matplotlib pulls in at each end is not cut (<c>split_path_inout</c> finds no crossing and
    /// leaves the path whole): drawn into a plot 40 pixels wide, every arrow's tip stands the mitre's pad short of its head.</summary>
    [Fact]
    public void AnArrowTooShortToBeCut_KeepsItsWholePath()
    {
        var series = SampleSeries.RotationalField();
        var small = new Rect(0, 0, 40, 30);
        var ctx = new RecordingRenderContext();
        var transform = new DataTransform(-3, 3, -3, 3, small);
        new StreamplotSeriesRenderer(new SeriesRenderContext(transform, ctx, Colors.Blue, new RenderArea(small, ctx)) { Theme = Theme.MatplotlibV2 }).Render(series);
        var first = Streamlines(series)[0];
        var head = transform.DataToPixel(first.ArrowHead.X, first.ArrowHead.Y);

        var tip = ctx.Of<DrawPolygonCall>()[0].Points[1];

        Assert.Equal(0.5 * 1.5.PointsInPixels / (2 / Math.Sqrt(20)), tip.DistanceTo(head), 6);
    }

    [Fact]
    public void AFieldWithNoStreamlines_DrawsNothing()
    {
        Assert.Empty(Render(new StreamplotSeries([0.0, 1, 2], [0.0, 1, 2], new double[3, 3], new double[3, 3]), Theme.MatplotlibV2).Calls);
    }

    /// <summary><c>lc.sticky_edges.x[:] = [grid.x_origin, grid.x_origin + grid.width]</c>, and the same for Y.</summary>
    [Fact]
    public void TheGridsEdges_AreStickyEdges()
    {
        var range = SampleSeries.RotationalField().ComputeDataRange(null!);

        Assert.Equal(-3, range.StickyXMin);
        Assert.Equal(3, range.StickyXMax);
        Assert.Equal(-3, range.StickyYMin);
        Assert.Equal(3, range.StickyYMax);
    }

    [Fact]
    public void AStreamplot_ReadsTheRestingWidth_DrawsTheThemesWidth_AndWritesAGivenWidthOnly()
    {
        var resting = SampleSeries.RotationalField();
        var given = SampleSeries.RotationalField();
        given.LineWidth = 1;

        Assert.Equal(1, resting.LineWidth);
        Assert.Equal(1.5.PointsInPixels, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(resting.GivenLineWidth, LineWidthRule.OnePixelCollection), 9);
        Assert.Equal(1, Theme.Default.SeriesSlot(0).LineWidthOf(resting.GivenLineWidth, LineWidthRule.OnePixelCollection));
        Assert.Equal(1, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(given.GivenLineWidth, LineWidthRule.OnePixelCollection));
        Assert.Null(resting.ToSeriesDto().LineWidth);
        Assert.Equal(1, given.ToSeriesDto().LineWidth);
    }

    // ── The layout: matplotlib 3.11.2's streamplot (StreamlinesOf) ──

    [Fact]
    public void TheFieldOfTheFidelityCheck_HasMatplotlibsSeventyOneStreamlines_OfMatplotlibsLengths()
    {
        var lines = Streamlines(SampleSeries.RotationalField());

        Assert.Equal(
            [36, 8, 16, 6, 12, 18, 22, 11, 21, 10, 17, 14, 9, 11, 8, 6, 39, 104, 18, 17, 10, 26, 10, 18, 10, 7, 14, 6, 9, 7, 7, 7, 6, 10, 6, 8, 11,
             14, 8, 7, 10, 14, 12, 22, 27, 23, 19, 16, 14, 12, 10, 6, 24, 9, 8, 19, 17, 9, 21, 10, 6, 5, 5, 6, 5, 5, 8, 14, 6, 15, 13],
            lines.Select(line => line.X.Length));
    }

    [Theory]
    [InlineData(0, 2.9999999999999956, -1.5287634997554376, -3.0, -3.0, -0.20733026040114266, -1.1135043123448845, -0.30376678491930953, -1.1387505435724017)]
    [InlineData(1, -1.7149263657061993, -2.175533254781561, -2.586206896551724, -3.0, -2.1558374611491167, -2.582118561171339, -2.2280429737509984, -2.6512633153923235)]
    [InlineData(2, -0.22923484309370545, -1.2463168682041195, -2.3793103448275867, -3.0, -1.388413071750397, -2.0121591770661063, -1.4621302878413878, -2.079659391943668)]
    [InlineData(35, 1.1379310344827562, 2.9999999999999956, 1.1444313492160285, 2.004265328758409, 1.1691425223173333, 2.6013587816758728, 1.171771317110184, 2.501484273678228)]
    [InlineData(70, 1.042005666215772, -0.5519253515123044, -0.18478780486810642, 0.8233456195131601, 0.44409182534605174, -0.025708047698353997, 0.3814750431688805, 0.052151302376870934)]
    public void AStreamline_StartsEndsAndCarriesItsArrowWhereMatplotlibsDoes(
        int index, double x0, double y0, double x1, double y1, double tailX, double tailY, double headX, double headY)
    {
        var line = Streamlines(SampleSeries.RotationalField())[index];

        Assert.Equal(x0, line.X[0], 9);
        Assert.Equal(y0, line.Y[0], 9);
        Assert.Equal(x1, line.X[^1], 9);
        Assert.Equal(y1, line.Y[^1], 9);
        Assert.Equal(tailX, line.ArrowTail.X, 9);
        Assert.Equal(tailY, line.ArrowTail.Y, 9);
        Assert.Equal(headX, line.ArrowHead.X, 9);
        Assert.Equal(headY, line.ArrowHead.Y, 9);
    }

    /// <summary>The density scales the mask: printed, 150 streamlines of 3,233 points at a density of 2, 38 of 486 at 0.5.</summary>
    [Theory]
    [InlineData(2, 150, 3233)]
    [InlineData(0.5, 38, 486)]
    public void TheDensity_ScalesTheMask(double density, int count, int points)
    {
        var lines = Streamlines(SampleSeries.RotationalField(density));

        Assert.Equal(count, lines.Count);
        Assert.Equal(points, lines.Sum(line => line.X.Length));
    }

    /// <summary>A mask of fewer than two cells a side has no spacing (matplotlib divides by its size less one and fails), and a grid of
    /// fewer than two points a side has no cells: nothing is laid out.</summary>
    [Fact]
    public void AMaskOrAGridTooSmallToHaveCells_LaysOutNothing()
    {
        Assert.Empty(Streamlines(SampleSeries.RotationalField(1.0 / 30)));
        Assert.Empty(Streamlines(new StreamplotSeries([0.0], [0.0], new double[1, 1], new double[1, 1])));
    }

    /// <summary>A field whose components do not match its grid, which only a series built by hand can have (the verb refuses it), lays out
    /// nothing rather than reading past the grid.</summary>
    [Theory]
    [InlineData(3, 2, 2, 3)]
    [InlineData(2, 2, 2, 3)]
    [InlineData(2, 3, 3, 3)]
    [InlineData(2, 3, 2, 2)]
    public void AFieldThatDoesNotMatchItsGrid_LaysOutNothing(int uRows, int uColumns, int vRows, int vColumns)
    {
        Assert.Empty(Streamlines(new StreamplotSeries([0.0, 1, 2], [0.0, 1], new double[uRows, uColumns], new double[vRows, vColumns])));
    }

    /// <summary>A grid whose first step is no step, or not a number, has no cells to map the field onto; a density that is not a number has
    /// no mask.</summary>
    [Theory]
    [InlineData(0.0, 0.0, 0.0, 1.0, 1)]
    [InlineData(0.0, 1.0, 0.0, 0.0, 1)]
    [InlineData(double.NaN, 1.0, 0.0, 1.0, 1)]
    [InlineData(0.0, 1.0, double.NaN, 1.0, 1)]
    [InlineData(0.0, double.PositiveInfinity, 0.0, 1.0, 1)]
    [InlineData(0.0, 1.0, 0.0, double.PositiveInfinity, 1)]
    [InlineData(0.0, 1.0, 0.0, 1.0, double.NaN)]
    public void AGridWithNoFirstStep_OrNoDensity_LaysOutNothing(double x0, double x1, double y0, double y1, double density)
    {
        var u = new double[3, 3];
        var v = new double[3, 3];
        for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++) { u[r, c] = 1; v[r, c] = 1; }

        Assert.Empty(Streamlines(new StreamplotSeries([x0, x1, 2], [y0, y1, 2], u, v) { Density = density }));
    }

    /// <summary>A grid that runs from high to low (a negative step) maps the field the other way round: every streamline of a field that
    /// blows towards higher X runs towards higher X, in the grid's own data units.</summary>
    [Fact]
    public void AGridThatRunsDownward_IsFollowedInItsOwnDirection()
    {
        double[] x = [.. Enumerable.Range(0, 10).Select(i => 9.0 - i)];
        var u = new double[10, 10];
        var v = new double[10, 10];
        for (int r = 0; r < 10; r++) for (int c = 0; c < 10; c++) u[r, c] = 1;

        var lines = Streamlines(new StreamplotSeries(x, [.. Enumerable.Range(0, 10).Select(i => (double)i)], u, v));

        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.True(line.X[^1] > line.X[0], $"{line.X[0]} -> {line.X[^1]}"));
        Assert.All(lines, line => Assert.True(line.ArrowHead.X > line.ArrowTail.X));
    }

    /// <summary>A field that blows straight up (no X part) reaches the top of the grid by the Euler step along Y alone, and one that blows
    /// to the left (negative X) reaches the left edge: both end exactly on the grid's edge.</summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 0)]
    public void AStreamlineEndsExactlyOnTheEdgeOfTheGrid(double u0, double v0)
    {
        var u = new double[10, 10];
        var v = new double[10, 10];
        for (int r = 0; r < 10; r++) for (int c = 0; c < 10; c++) { u[r, c] = u0; v[r, c] = v0; }
        double[] axis = [.. Enumerable.Range(0, 10).Select(i => (double)i)];

        var lines = Streamlines(new StreamplotSeries(axis, axis, u, v));

        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Equal(v0 > 0 ? 9 : 0, v0 > 0 ? line.Y[^1] : line.X[^1], 9));
    }

    /// <summary>matplotlib masks a field that is not a number (<c>np.ma.masked_invalid</c>), and <c>interpgrid</c> ends a streamline that
    /// reaches a masked value: a streamline may end in a cell next to the missing one (the point where it stopped), but none runs through
    /// such a cell, and every point is a number.</summary>
    [Fact]
    public void AMissingValueInTheField_EndsTheStreamlinesThatReachIt()
    {
        var series = SampleSeries.RotationalField();
        series.U[20, 20] = double.NaN;
        var axis = series.X;
        bool NextToIt(double x, double y) => x > axis[19] && x < axis[21] && y > axis[19] && y < axis[21];

        var lines = Streamlines(series);

        Assert.NotEmpty(lines);
        Assert.All(lines, line =>
        {
            line.X.Concat(line.Y).AssertAllFinite();
            for (int i = 1; i < line.X.Length - 1; i++)
                Assert.False(NextToIt(line.X[i], line.Y[i]), $"({line.X[i]}, {line.Y[i]}) runs through a cell that reads the missing value");
        });
    }

    /// <summary>On a grid of 4 points a side the mask's last column of seeds lands a hair past the grid (29 * (1 / (29 / 3)) is
    /// 3.0000000000000004), where matplotlib's integration finds the seed out of bounds and takes no step: those seeds draw nothing.
    /// Printed for <c>U = 1</c>, <c>V = 0.5</c> on <c>np.arange(4.0)</c>: 74 streamlines of 882 points, none past X = 3.</summary>
    [Fact]
    public void ASeedThatLandsJustPastTheGrid_DrawsNothing_AsMatplotlibsDoes()
    {
        double[] axis = [0.0, 1, 2, 3];
        var u = new double[4, 4];
        var v = new double[4, 4];
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) { u[r, c] = 1; v[r, c] = 0.5; }

        var lines = Streamlines(new StreamplotSeries(axis, axis, u, v));

        Assert.Equal(74, lines.Count);
        Assert.Equal(882, lines.Sum(line => line.X.Length));
        Assert.Equal(3.0, lines.Max(line => line.X.Max()), 12);
    }

    /// <summary>A field that is zero everywhere has no direction to follow: matplotlib's <c>forward_time</c> ends every streamline at its
    /// seed, each is shorter than <c>minlength</c>, and nothing is drawn.</summary>
    [Fact]
    public void AStillField_LaysOutNothing()
    {
        Assert.Empty(Streamlines(new StreamplotSeries([0.0, 1, 2], [0.0, 1, 2], new double[3, 3], new double[3, 3])));
    }
}
