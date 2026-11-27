// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>The head of a 3-D quiver's arrow is the library's open head (<see cref="ArrowHead.OpenHeadMirrored"/>): after the shaft, two
/// barbs from the tip, each 6 px long and 0.4 rad from the way back along the shaft, the one turned the larger way first. It was worked
/// out in the renderer with the same two numbers; the picture and the order of its two strokes are unchanged.</summary>
public class Quiver3DArrowHeadTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(Quiver3DSeries series)
    {
        var ctx = new RecordingRenderContext();
        var transform = new DataTransform(0, 10, 0, 10, Bounds);
        var projection = new Projection3D(30, -60, Bounds, 0, 10, 0, 10, 0, 10);
        new Quiver3DSeriesRenderer(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)) { Projection3D = projection }).Render(series);
        return ctx;
    }

    [Fact]
    public void AnArrow_IsAShaftAndThenTwoBarbsFromItsTip_TheLeftOneFirst()
    {
        var calls = Render(new Quiver3DSeries(new double[] { 2 }, new double[] { 2 }, new double[] { 2 }, new double[] { 3 }, new double[] { 1 }, new double[] { 2 })).Of<DrawLineCall>();

        Assert.Equal(3, calls.Count);
        var (basePt, tip) = (calls[0].P1, calls[0].P2);
        double length = tip.DistanceTo(basePt);
        double ux = (tip.X - basePt.X) / length, uy = (tip.Y - basePt.Y) / length;
        double cos = Math.Cos(0.4), sin = Math.Sin(0.4);
        // The library's own head: the left barb turned by +0.4 rad from the way back along the shaft, the right by -0.4.
        var left = new Point(tip.X - 6 * (ux * cos - uy * sin), tip.Y - 6 * (uy * cos + ux * sin));
        var right = new Point(tip.X - 6 * (ux * cos + uy * sin), tip.Y - 6 * (uy * cos - ux * sin));
        Assert.Equal(tip, calls[1].P1);
        Assert.Equal(left.X, calls[1].P2.X, 9);
        Assert.Equal(left.Y, calls[1].P2.Y, 9);
        Assert.Equal(tip, calls[2].P1);
        Assert.Equal(right.X, calls[2].P2.X, 9);
        Assert.Equal(right.Y, calls[2].P2.Y, 9);
    }

    [Fact]
    public void AnArrowWhoseEndsStandOnOnePixel_HasNoHead()
    {
        var calls = Render(new Quiver3DSeries(new double[] { 2 }, new double[] { 2 }, new double[] { 2 }, new double[] { 0 }, new double[] { 0 }, new double[] { 0 })).Of<DrawLineCall>();

        Assert.Single(calls);
    }
}
