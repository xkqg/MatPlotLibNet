// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A step series draws the corners matplotlib's <c>step(where=...)</c> draws, which are the corners of a line drawn in the
/// draw style its position names (<c>drawstyle="steps-" + where</c>): one stepping for both.</summary>
public class StepSeriesRendererTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    // Data 0..10 on 400 x 300 pixels: x = 40 * value, y = 300 - 30 * value.
    private static readonly DataTransform Transform = new(0, 10, 0, 10, Bounds);

    private static readonly double[] X = [0, 1, 2, 3];
    private static readonly double[] Y = [1, 3, 2, 4];

    private static Point[] Drawn(Action<SeriesRenderContext> render)
    {
        var ctx = new RecordingRenderContext();
        render(new SeriesRenderContext(Transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        var line = Assert.Single(ctx.Of<DrawLinesCall>());
        return line.Points;
    }

    /// <summary>matplotlib 3.11.2 <c>cbook.STEP_LOOKUP_MAP["steps-" + where]([0, 1, 2, 3], [1, 3, 2, 4])</c>, printed by
    /// <c>python -I</c>; the step series and the line in the same style draw those corners, pixel for pixel.</summary>
    [Theory]
    [InlineData(StepPosition.Pre, DrawStyle.StepsPre, new[] { 0.0, 0, 1, 1, 2, 2, 3 }, new[] { 1.0, 3, 3, 2, 2, 4, 4 })]
    [InlineData(StepPosition.Mid, DrawStyle.StepsMid, new[] { 0.0, 0.5, 0.5, 1.5, 1.5, 2.5, 2.5, 3 }, new[] { 1.0, 1, 3, 3, 2, 2, 4, 4 })]
    [InlineData(StepPosition.Post, DrawStyle.StepsPost, new[] { 0.0, 1, 1, 2, 2, 3, 3 }, new[] { 1.0, 1, 3, 3, 2, 2, 4 })]
    public void EachStepPosition_DrawsMatplotlibsCorners_AsTheLineInItsDrawStyle(StepPosition position, DrawStyle style, double[] x, double[] y)
    {
        var want = x.Select((v, i) => new Point(40 * v, 300 - (30 * y[i]))).Corners();

        var step = Drawn(c => new StepSeriesRenderer(c).Render(new StepSeries(X, Y) { StepPosition = position })).Corners();
        var line = Drawn(c => new LineSeriesRenderer(c).Render(new LineSeries(X, Y) { DrawStyle = style })).Corners();

        Assert.Equal(want.Count, step.Count);
        for (int i = 0; i < want.Count; i++)
        {
            Assert.Equal(want[i].X, step[i].X, 9);
            Assert.Equal(want[i].Y, step[i].Y, 9);
        }
        Assert.Equal(step, line);
    }

    /// <summary>A step of one point has no corner and draws no line, in every position (a one-point Line2D draws none either).</summary>
    [Theory]
    [InlineData(StepPosition.Pre)]
    [InlineData(StepPosition.Mid)]
    [InlineData(StepPosition.Post)]
    public void AStepOfOnePoint_DrawsNoLine(StepPosition position)
    {
        var ctx = new RecordingRenderContext();
        new StepSeriesRenderer(new SeriesRenderContext(Transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)))
            .Render(new StepSeries([2.0], [2.0]) { StepPosition = position });

        Assert.Empty(ctx.Of<DrawLinesCall>());
    }

    [Fact]
    public void Step_VeryLargeInput_RendersFinitePolyline()
    {
        var svg = Plt.Create().WithSize(800, 600).AddSubPlot(1, 1, 1, ax => ax.Step(EdgeCaseData.Ramp(1000), EdgeCaseData.Sin(1000))).ToSvg();
        var pts = svg.PolylinePoints();
        Assert.NotEmpty(pts);
        // No off-canvas pixels (allow generous canvas including margin)
        SvgMarkupExtensions.AssertPointsInCanvas(pts, -50, 850, -50, 650, "Step");
    }
}
