// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Avalonia;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using SkiaSharp;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>The control converts the pointer with the range and the scale the renderer drew: a wheel on an autoscaled
/// chart zooms into the data (it used to zoom into 0..1), hover on a log axis names the point under the pointer, and a
/// drag on a log axis moves it by decades. What runs here is the control's own sequence after a paint — the render and
/// layout pass of <c>MplChartDrawOperation</c>, then <see cref="MplChartControl.Attach"/> — on a Skia surface; the
/// dispatcher hop between them needs a running Avalonia application and is not run.</summary>
public class MplChartControlPointerUnitsTests
{
    /// <summary>What <c>MplChartDrawOperation.Render</c> does: draw on a Skia canvas, then lay the figure out on it.</summary>
    private static ChartLayout PaintAndLayOut(Figure figure)
    {
        using var surface = SKSurface.Create(new SKImageInfo((int)figure.Width, (int)figure.Height));
        var ctx = new SkiaRenderContext(surface.Canvas);
        ChartServices.Renderer.Render(figure, ctx);
        return ChartLayout.Create(figure, ChartServices.Renderer.ComputeLayout(figure, ctx));
    }

    private static InteractionController Attached(Figure figure, out Rect area)
    {
        var layout = PaintAndLayOut(figure);
        area = layout.GetPlotArea(0);
        return new MplChartControl { Figure = figure, IsInteractive = true }.Attach(figure, layout);
    }

    /// <summary>Log X from 1 to 1000 (three decades), Y from 0 to 5; the points sit at 1, 10, 100, 1000.</summary>
    private static Figure LogFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10, 100, 1000], [1.0, 2, 3, 4])
            .SetXScale(AxisScale.Log).SetXLim(1, 1000).SetYLim(0, 5))
        .Build();

    [Fact]
    public void AWheelOnAnAutoscaledChart_ZoomsIntoTheData()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        var controller = Attached(figure, out var area);

        controller.HandleScroll(new ScrollInputArgs(area.X + area.Width / 2, area.Y + area.Height / 2, 0, -1, ModifierKeys.None));

        var x = figure.SubPlots[0].XAxis;
        var y = figure.SubPlots[0].YAxis;
        Assert.InRange(x.Min!.Value, 1.0, 2.0);
        Assert.InRange(x.Max!.Value, 2.0, 3.0);
        Assert.InRange(y.Min!.Value, 8.0, 25.0);
        Assert.InRange(y.Max!.Value, 25.0, 42.0);
    }

    [Fact]
    public void HoverOnALogAxis_NamesThePointUnderThePointer()
    {
        var figure = LogFigure();
        var controller = Attached(figure, out var area);

        // x = 10 is one decade of three from the left edge; y = 2 is two fifths up.
        controller.HandlePointerMoved(new PointerInputArgs(area.X + area.Width / 3, area.Y + area.Height * 3 / 5,
            PointerButton.None, ModifierKeys.None));

        Assert.NotNull(controller.ActiveTooltip);
        Assert.Equal(1, controller.ActiveTooltip!.PointIndex);
    }

    [Fact]
    public void ADragOnALogAxis_MovesTheRangeByDecades()
    {
        var figure = LogFigure();
        var controller = Attached(figure, out var area);
        double y = area.Y + area.Height * 0.9;

        controller.HandlePointerPressed(new PointerInputArgs(area.X + area.Width * 2 / 3, y, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(area.X + area.Width / 3, y, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerReleased(new PointerInputArgs(area.X + area.Width / 3, y, PointerButton.Left, ModifierKeys.None));

        // A third of the width is one decade: 1..1000 becomes 10..10000.
        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Min!.Value, 9);
        Assert.Equal(10000.0, figure.SubPlots[0].XAxis.Max!.Value, 6);
    }
}
