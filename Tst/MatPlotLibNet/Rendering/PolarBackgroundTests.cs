// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A polar axes fills its background on its circle, as matplotlib's polar axes draws its patch
/// (<c>PolarAxes._gen_axes_patch</c>, a full wedge): under the classic style the figure's grey shows in the corners of the
/// cell, where ours filled the whole cell white.</summary>
public class PolarBackgroundTests
{
    private static readonly Rect Cell = new(100, 60, 620, 480);

    [Fact]
    public void TheAxesBackground_FillsTheCircle_NotTheCell()
    {
        var ctx = new RecordingRenderContext();
        var theme = Theme.MatplotlibClassic;
        new PolarAxesRenderer(new Axes { CoordinateSystem = CoordinateSystem.Polar }, Cell, ctx, theme).Render();

        Assert.DoesNotContain(ctx.Of<DrawRectangleCall>(), call => call.Fill == theme.AxesBackground);
        var background = ctx.Of<DrawCircleCall>()
            .Single(args => args.Fill == theme.AxesBackground);
        var transform = new PolarTransform(Cell, 1);
        Assert.Equal(new Point(transform.CenterX, transform.CenterY), background.Center);
        Assert.Equal(transform.MaxRadius, background.Radius, 9);
        Assert.Null(background.Stroke);
    }

    [Fact]
    public void TheBackground_IsTheFirstThingDrawn()
    {
        var ctx = new RecordingRenderContext();
        new PolarAxesRenderer(new Axes { CoordinateSystem = CoordinateSystem.Polar }, Cell, ctx, Theme.MatplotlibV2).Render();

        Assert.IsType<DrawCircleCall>(ctx.Calls[0]);
    }
}
