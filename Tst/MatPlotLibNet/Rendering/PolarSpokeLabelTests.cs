// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A polar axes draws and names eight spokes, every forty-five degrees from 0 at the right counter-clockwise, the same
/// eight texts at every draw. matplotlib 3.11, <c>ThetaLocator.__call__</c>: on a full circle it returns
/// <c>np.deg2rad(min(lim)) + np.arange(8) * 2 * np.pi / 8</c>; <c>ThetaFormatter</c> writes
/// <c>f"{np.rad2deg(x):0.{digits}f}°"</c> with <c>digits = max(-int(np.log10(360) - 1.5), 0) = 0</c>.</summary>
public class PolarSpokeLabelTests
{
    [Fact]
    public void TheEightSpokes_AreNamedByTheirAngle_InTurn()
    {
        var ctx = new RecordingRenderContext();
        new PolarAxesRenderer(new Axes { CoordinateSystem = CoordinateSystem.Polar }, new Rect(0, 0, 400, 400), ctx, Theme.Default).Render();

        var spokes = ctx.Of<DrawTextCall>().Select(c => c.Text).Where(t => t.EndsWith('°')).ToArray();

        Assert.Equal(["0°", "45°", "90°", "135°", "180°", "225°", "270°", "315°"], spokes);
    }

    [Fact]
    public void TheEightSpokes_RunFromTheCentreToTheCircle_EveryFortyFiveDegrees()
    {
        var ctx = new RecordingRenderContext();
        var plot = new Rect(0, 0, 400, 400);
        new PolarAxesRenderer(new Axes { CoordinateSystem = CoordinateSystem.Polar }, plot, ctx, Theme.Default).Render();

        var centre = new Point(200, 200);
        var ends = ctx.Of<DrawLineCall>().Where(a => a.P1 == centre).Select(a => a.P2).ToArray();

        Assert.Equal(8, ends.Length);
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(200 + 200 * Math.Cos(i * Math.PI / 4), ends[i].X, 9);
            Assert.Equal(200 - 200 * Math.Sin(i * Math.PI / 4), ends[i].Y, 9);
        }
    }

    [Fact]
    public void ASecondDraw_NamesTheSpokesWithTheSameTexts()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        var first = new RecordingRenderContext();
        var second = new RecordingRenderContext();
        new PolarAxesRenderer(axes, new Rect(0, 0, 400, 400), first, Theme.Default).Render();
        new PolarAxesRenderer(axes, new Rect(0, 0, 400, 400), second, Theme.Default).Render();

        string[] Spokes(RecordingRenderContext ctx) => [.. ctx.Of<DrawTextCall>().Select(c => c.Text).Where(t => t.EndsWith('°'))];
        Assert.Equal(Spokes(first), Spokes(second));
        Assert.All(Spokes(first).Zip(Spokes(second)), pair => Assert.Same(pair.First, pair.Second));
    }
}
