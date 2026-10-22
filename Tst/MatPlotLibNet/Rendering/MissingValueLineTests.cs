// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A NaN now breaks a line, as in matplotlib ("if plotting with a line, it will be broken there"): a missing
/// value is not there, so nothing is drawn through it. Before, the SVG context dropped the NaN point and joined its
/// neighbours, drawing a value that was never measured, and every other render context was handed the NaN to cope
/// with. The renderer splits the line into its unbroken runs itself, so every render context (SVG, Skia, a native head)
/// receives only points it can draw. A step line and an area follow the same rule.</summary>
public class MissingValueLineTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static SeriesRenderContext ContextOn(RecordingRenderContext ctx) =>
        new(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx));

    private static Point[] PointsOf(CallRecord call) => (Point[])call.Args!.GetType().GetProperty("points")!.GetValue(call.Args)!;

    private static List<Point[]> Lines(RecordingRenderContext ctx) => ctx.OfKind("DrawLines").Select(PointsOf).ToList();

    private static List<Point[]> Polygons(RecordingRenderContext ctx) => ctx.OfKind("DrawPolygon").Select(PointsOf).ToList();

    private static void AssertEveryPointDrawable(IEnumerable<Point[]> shapes) =>
        Assert.All(shapes.SelectMany(p => p), p => Assert.True(double.IsFinite(p.X) && double.IsFinite(p.Y), $"a point ({p.X}, {p.Y}) reached the render context"));

    private static RecordingRenderContext DrawLine(LineSeries series)
    {
        var ctx = new RecordingRenderContext();
        new LineSeriesRenderer(ContextOn(ctx)).Render(series);
        return ctx;
    }

    private static RecordingRenderContext DrawArea(AreaSeries series)
    {
        var ctx = new RecordingRenderContext();
        new AreaSeriesRenderer(ContextOn(ctx)).Render(series);
        return ctx;
    }

    // ---- a line -----------------------------------------------------------------------------------------------------

    [Fact]
    public void ALine_BreaksAtAMissingValue()
    {
        var lines = Lines(DrawLine(new LineSeries([0.0, 1, 2, 3, 4], [1, 2, double.NaN, 2, 1])));

        Assert.Equal([2, 2], lines.Select(l => l.Length));
        AssertEveryPointDrawable(lines);
    }

    [Fact]
    public void ALine_BreaksAtAnInfinity()
    {
        var lines = Lines(DrawLine(new LineSeries([0.0, 1, 2, 3, 4], [1, 2, double.PositiveInfinity, 2, 1])));

        Assert.Equal([2, 2], lines.Select(l => l.Length));
        AssertEveryPointDrawable(lines);
    }

    /// <summary>A point with a gap on either side joins nothing, so as a line it draws nothing (its marker is the
    /// marker's business).</summary>
    [Fact]
    public void APointAloneBetweenTwoGaps_DrawsNoLine()
    {
        var lines = Lines(DrawLine(new LineSeries([0.0, 1, 2, 3, 4, 5], [1, double.NaN, 2, double.NaN, 3, 4])));

        var only = Assert.Single(lines);
        Assert.Equal(2, only.Length);
    }

    [Fact]
    public void ALineWithoutAGap_IsOnePolylineThroughEveryPoint() =>
        Assert.Equal(5, Assert.Single(Lines(DrawLine(new LineSeries([0.0, 1, 2, 3, 4], [1, 2, 3, 2, 1])))).Length);

    /// <summary>matplotlib's steps-post: the step of the value before the gap still runs to the gap's x, then the line
    /// stops; it resumes at the next value.</summary>
    [Fact]
    public void ASteppedLine_BreaksAtAMissingValue()
    {
        var lines = Lines(DrawLine(new LineSeries([0.0, 1, 2, 3, 4], [1, 2, double.NaN, 3, 4]) { DrawStyle = DrawStyle.StepsPost }));

        Assert.Equal([4, 3], lines.Select(l => l.Length));
        AssertEveryPointDrawable(lines);
    }

    /// <summary>A smoothed line is smoothed run by run: a NaN in the spline's slopes used to spoil the two intervals on
    /// either side of the gap, widening it.</summary>
    [Fact]
    public void ASmoothedLine_IsSmoothedRunByRun()
    {
        var lines = Lines(DrawLine(new LineSeries([0.0, 1, 2, 3, 4, 5, 6], [0, 1, 4, double.NaN, 1, 0, 2]) { Smooth = true, SmoothResolution = 10 }));

        Assert.Equal([21, 21], lines.Select(l => l.Length));
        AssertEveryPointDrawable(lines);
    }

    [Fact]
    public void AStepSeries_BreaksAtAMissingValue()
    {
        var ctx = new RecordingRenderContext();
        new StepSeriesRenderer(ContextOn(ctx)).Render(new StepSeries([0.0, 1, 2, 3, 4], [1, 2, double.NaN, 3, 4]));

        var lines = Lines(ctx);
        Assert.Equal([4, 3], lines.Select(l => l.Length));
        AssertEveryPointDrawable(lines);
    }

    [Fact]
    public void TheSvgOfALineWithAGap_HoldsTwoPolylines_AndNoNaN()
    {
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2, 3, 4], [1, 2, double.NaN, 2, 1])).ToSvg();

        Assert.Equal(2, Regex.Matches(svg, "<polyline ").Count);
        Assert.DoesNotContain("NaN", svg);
    }

    // ---- an area ----------------------------------------------------------------------------------------------------

    [Fact]
    public void AnArea_IsFilledAndOutlinedRunByRun()
    {
        var ctx = DrawArea(new AreaSeries([0.0, 1, 2, 3, 4], [1, 2, double.NaN, 2, 1]));

        Assert.Equal(2, Polygons(ctx).Count);
        Assert.Equal(2, Lines(ctx).Count);
        AssertEveryPointDrawable(Polygons(ctx).Concat(Lines(ctx)));
    }

    /// <summary>The runs here are [0, 1], [3] and [5, 7]: the gap in the lower line breaks the fill as the gap in the
    /// upper one does. The lone point at 3 is a run of its own, a fill of no width, as matplotlib's fill_between leaves
    /// it.</summary>
    [Fact]
    public void AnAreaBetweenTwoLines_BreaksWhereEitherLineHasAGap()
    {
        var ctx = DrawArea(new AreaSeries([0.0, 1, 2, 3, 4, 5, 6, 7], [3, 3, 3, 3, double.NaN, 3, 3, 3]) { YData2 = [1, 1, double.NaN, 1, 1, 1, 1, 1] });

        Assert.Equal([4, 2, 6], Polygons(ctx).Select(p => p.Length));
        AssertEveryPointDrawable(Polygons(ctx).Concat(Lines(ctx)));
    }

    [Fact]
    public void ASteppedArea_BreaksAtAGap()
    {
        var ctx = DrawArea(new AreaSeries([0.0, 1, 2, 3, 4], [1, 2, double.NaN, 2, 1]) { StepMode = DrawStyle.StepsPost });

        Assert.Equal(2, Polygons(ctx).Count);
        AssertEveryPointDrawable(Polygons(ctx).Concat(Lines(ctx)));
    }

    [Fact]
    public void ASmoothedArea_BreaksAtAGap()
    {
        var ctx = DrawArea(new AreaSeries([0.0, 1, 2, 3, 4, 5, 6], [0, 1, 4, double.NaN, 1, 0, 2]) { Smooth = true });

        Assert.Equal(2, Polygons(ctx).Count);
        AssertEveryPointDrawable(Polygons(ctx).Concat(Lines(ctx)));
    }

    [Fact]
    public void AnAreaWithAWherePredicate_BreaksAtAGapToo()
    {
        var ctx = DrawArea(new AreaSeries([0.0, 1, 2, 3, 4], [1, 2, double.NaN, 2, 1]) { Where = (_, _) => true, YData2 = [0, 0, 0, 0, 0] });

        Assert.Equal(2, Polygons(ctx).Count);
        AssertEveryPointDrawable(Polygons(ctx).Concat(Lines(ctx)));
    }

    [Fact]
    public void AnAreaWithoutAGap_IsOnePolygonAndOneOutline()
    {
        var ctx = DrawArea(new AreaSeries([0.0, 1, 2], [1, 2, 1]));

        Assert.Single(Polygons(ctx));
        Assert.Equal(3, Assert.Single(Lines(ctx)).Length);
    }

    [Fact]
    public void AnAreaWithNothingDrawable_DrawsNothing()
    {
        var ctx = DrawArea(new AreaSeries([0.0, 1], [double.NaN, double.NaN]));

        Assert.Empty(Polygons(ctx));
        Assert.Empty(Lines(ctx));
    }

    // ---- the smoothing itself ---------------------------------------------------------------------------------------

    [Fact]
    public void TheSpline_SmoothsEachRunOnItsOwn_AndKeepsTheGapBetweenThem()
    {
        double[] x = [0, 1, 2, 3, 4, 5, 6], y = [0, 1, 4, double.NaN, 1, 0, 2];

        var curve = MonotoneCubicSpline.Interpolate(x, y, 10);

        var first = MonotoneCubicSpline.Interpolate([0, 1, 2], [0, 1, 4], 10);
        var second = MonotoneCubicSpline.Interpolate([4, 5, 6], [1, 0, 2], 10);
        Assert.Equal(first.Y.Concat([double.NaN]).Concat(second.Y), curve.Y);
        Assert.Equal(first.X.Concat([double.NaN]).Concat(second.X), curve.X);
    }

    /// <summary>A point with a gap on either side is a run of one: it stays as it is, nothing to smooth.</summary>
    [Fact]
    public void TheSpline_KeepsALonePointBetweenTwoGaps()
    {
        var curve = MonotoneCubicSpline.Interpolate([0.0, 1, 2, 3, 4], [double.NaN, 5, double.NaN, 1, 2], 10);

        Assert.Equal(new[] { 5.0, double.NaN }, curve.Y.Take(2));
        Assert.Equal(13, curve.Y.Length);
    }

    [Fact]
    public void TheSpline_OfNothingDrawable_IsEmpty()
    {
        var curve = MonotoneCubicSpline.Interpolate([0.0, 1, 2], [double.NaN, double.NaN, double.NaN], 10);

        Assert.Empty(curve.X);
        Assert.Empty(curve.Y);
    }
}
