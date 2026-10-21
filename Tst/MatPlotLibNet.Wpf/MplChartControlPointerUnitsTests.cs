// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>The control converts the pointer with the range and the scale the renderer drew: a wheel on an autoscaled
/// chart zooms into the data (it used to zoom into 0..1), hover on a log axis names the point under the pointer, and a
/// drag on a log axis moves it by decades. The control is painted the way WPF paints it (<c>OnPaintSurface</c> on a
/// Skia surface, which builds its controller) and the pointer goes to that controller; WPF's own mouse events, which
/// only hand the position on, are not raised.</summary>
public class MplChartControlPointerUnitsTests
{
    private static void OnStaThread(Action body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    /// <summary>Paints the control once, as WPF does, and returns the controller it built and the plot area it drew.</summary>
    private static InteractionController Painted(MplChartControl control, out MatPlotLibNet.Rendering.Rect area)
    {
        var figure = control.Figure!;
        using var surface = SKSurface.Create(new SKImageInfo((int)figure.Width, (int)figure.Height));
        var paint = typeof(MplChartControl).GetMethod("OnPaintSurface", BindingFlags.NonPublic | BindingFlags.Instance)!;
        paint.Invoke(control, [new SKPaintSurfaceEventArgs(surface, new SKImageInfo((int)figure.Width, (int)figure.Height))]);
        area = ChartLayout.Create(figure, ChartServices.Renderer.ComputeLayout(figure, new SkiaRenderContext(surface.Canvas)))
            .GetPlotArea(0);
        return (InteractionController)typeof(MplChartControl)
            .GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(control)!;
    }

    /// <summary>Log X from 1 to 1000 (three decades), Y from 0 to 5; the points sit at 1, 10, 100, 1000.</summary>
    private static Figure LogFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10, 100, 1000], [1.0, 2, 3, 4])
            .SetXScale(AxisScale.Log).SetXLim(1, 1000).SetYLim(0, 5))
        .Build();

    [Fact]
    public void AWheelOnAnAutoscaledChart_ZoomsIntoTheData()
    {
        OnStaThread(() =>
        {
            var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
            var controller = Painted(new MplChartControl { Figure = figure, IsInteractive = true }, out var area);

            controller.HandleScroll(new ScrollInputArgs(area.X + area.Width / 2, area.Y + area.Height / 2, 0, -1,
                MatPlotLibNet.Interaction.ModifierKeys.None));

            var x = figure.SubPlots[0].XAxis;
            var y = figure.SubPlots[0].YAxis;
            Assert.InRange(x.Min!.Value, 1.0, 2.0);
            Assert.InRange(x.Max!.Value, 2.0, 3.0);
            Assert.InRange(y.Min!.Value, 8.0, 25.0);
            Assert.InRange(y.Max!.Value, 25.0, 42.0);
        });
    }

    [Fact]
    public void HoverOnALogAxis_NamesThePointUnderThePointer()
    {
        OnStaThread(() =>
        {
            var figure = LogFigure();
            var controller = Painted(new MplChartControl { Figure = figure, IsInteractive = true }, out var area);

            // x = 10 is one decade of three from the left edge; y = 2 is two fifths up.
            controller.HandlePointerMoved(new PointerInputArgs(area.X + area.Width / 3, area.Y + area.Height * 3 / 5,
                PointerButton.None, MatPlotLibNet.Interaction.ModifierKeys.None));

            Assert.NotNull(controller.ActiveTooltip);
            Assert.Equal(1, controller.ActiveTooltip!.PointIndex);
        });
    }

    [Fact]
    public void ADragOnALogAxis_MovesTheRangeByDecades()
    {
        OnStaThread(() =>
        {
            var figure = LogFigure();
            var controller = Painted(new MplChartControl { Figure = figure, IsInteractive = true }, out var area);
            var none = MatPlotLibNet.Interaction.ModifierKeys.None;
            double y = area.Y + area.Height * 0.9;

            controller.HandlePointerPressed(new PointerInputArgs(area.X + area.Width * 2 / 3, y, PointerButton.Left, none));
            controller.HandlePointerMoved(new PointerInputArgs(area.X + area.Width / 3, y, PointerButton.Left, none));
            controller.HandlePointerReleased(new PointerInputArgs(area.X + area.Width / 3, y, PointerButton.Left, none));

            // A third of the width is one decade: 1..1000 becomes 10..10000.
            Assert.Equal(10.0, figure.SubPlots[0].XAxis.Min!.Value, 9);
            Assert.Equal(10000.0, figure.SubPlots[0].XAxis.Max!.Value, 6);
        });
    }
}
