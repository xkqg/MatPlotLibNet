// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>
/// An application that moves a playback marker or swaps the chart for a fresh one assigns a new
/// <see cref="Figure"/> to an interactive <see cref="MplChartControl"/>. Pan and zoom must then act on the
/// figure the reader is looking at, not on the first one the control ever drew. Raised while answering
/// discussion #22, where a replay application rebuilds its timeline figure on every tick.
/// </summary>
public class MplChartControlTests
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

    private static Figure LineFigure() =>
        Plt.Create().Plot([0.0, 1, 2, 3, 4], [0.0, 1, 4, 9, 16]).Build();

    /// <summary>Paints the control once, the way WPF does, so the interaction controller gets built or re-aimed.</summary>
    private static void Paint(MplChartControl control)
    {
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var paint = typeof(MplChartControl).GetMethod("OnPaintSurface", BindingFlags.NonPublic | BindingFlags.Instance)!;
        paint.Invoke(control, [new SKPaintSurfaceEventArgs(surface, new SKImageInfo(800, 600))]);
    }

    private static InteractionController ControllerOf(MplChartControl control) =>
        (InteractionController)typeof(MplChartControl)
            .GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(control)!;

    [Fact]
    public void ZoomOnTheFigureTheControlDrew_ActsOnIt()
    {
        // The control for the replacement test below: without it, a red there could just mean the zoom never
        // reaches a figure in this harness at all.
        OnStaThread(() =>
        {
            var figure = LineFigure();
            var control = new MplChartControl { Figure = figure, IsInteractive = true };
            Paint(control);

            ControllerOf(control).HandleScroll(new ScrollInputArgs(400, 300, 0, 3.0, MatPlotLibNet.Interaction.ModifierKeys.None));

            Assert.NotNull(figure.SubPlots[0].XAxis.Min);
        });
    }

    [Fact]
    public void ZoomAfterTheFigureIsReplaced_ActsOnTheNewFigure()
    {
        OnStaThread(() =>
        {
            var first = LineFigure();
            var second = LineFigure();
            var control = new MplChartControl { Figure = first, IsInteractive = true };
            Paint(control);

            control.Figure = second;
            Paint(control);
            ControllerOf(control).HandleScroll(new ScrollInputArgs(400, 300, 0, 3.0, MatPlotLibNet.Interaction.ModifierKeys.None));

            Assert.NotNull(second.SubPlots[0].XAxis.Min);
            Assert.Null(first.SubPlots[0].XAxis.Min);
        });
    }

    /// <summary>Two stacked lanes on one time axis, the way a replay application lays out its categories.</summary>
    private static Figure TwoLanes() => Plt.Create()
        .AddSubPlot(2, 1, 1, ax => ax.StateTimeline([new StateSegment(0, 50, "Speech", Colors.Blue)]).SetXLim(0, 100), key: "time")
        .AddSubPlot(2, 1, 2, ax => ax.StateTimeline([new StateSegment(0, 50, "Motion", Colors.Orange)]).SetXLim(0, 100).ShareX("time"))
        .Build();

    /// <summary>The pixel at a data X inside lane <paramref name="lane"/>, from the same layout the control builds.</summary>
    private static Point PixelAt(Figure figure, int lane, double dataX)
    {
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var ctx = new SkiaRenderContext(surface.Canvas);
        var layout = ChartLayout.Create(figure, ChartServices.Renderer.ComputeLayout(figure, ctx));
        var area = layout.GetPlotArea(lane);
        var range = layout.GetDataRange(lane);
        return new Point(area.X + (dataX - range.XMin) / (range.XMax - range.XMin) * area.Width, area.Y + area.Height / 2);
    }

    [Fact]
    public void AClickOnASegmentInTheSecondLane_ReachesTheControlsEventWithTheLaneAndTheSegment()
    {
        OnStaThread(() =>
        {
            var figure = TwoLanes();
            var control = new MplChartControl { Figure = figure, IsInteractive = true };
            Paint(control);
            var clicks = new List<PinnedAnnotation>();
            control.DataPointClicked += clicks.Add;
            var at = PixelAt(figure, lane: 1, dataX: 25);

            var controller = ControllerOf(control);
            controller.HandlePointerPressed(new PointerInputArgs(at.X, at.Y, PointerButton.Left, MatPlotLibNet.Interaction.ModifierKeys.None));
            controller.HandlePointerReleased(new PointerInputArgs(at.X, at.Y, PointerButton.Left, MatPlotLibNet.Interaction.ModifierKeys.None));

            var click = Assert.Single(clicks);
            Assert.Equal(1, click.AxesIndex);
            Assert.Equal(0, click.PointIndex);
        });
    }

    [Fact]
    public void DraggingAcrossOneLane_MovesTheLanesThatShareItsTime()
    {
        OnStaThread(() =>
        {
            var figure = TwoLanes();
            var control = new MplChartControl { Figure = figure, IsInteractive = true };
            Paint(control);
            var from = PixelAt(figure, lane: 0, dataX: 25);
            var to = PixelAt(figure, lane: 0, dataX: 15);

            var controller = ControllerOf(control);
            var none = MatPlotLibNet.Interaction.ModifierKeys.None;
            controller.HandlePointerPressed(new PointerInputArgs(from.X, from.Y, PointerButton.Left, none));
            controller.HandlePointerMoved(new PointerInputArgs(to.X, to.Y, PointerButton.Left, none));
            controller.HandlePointerReleased(new PointerInputArgs(to.X, to.Y, PointerButton.Left, none));

            Assert.Equal(10.0, figure.SubPlots[0].XAxis.Min!.Value, precision: 3);
            Assert.Equal(figure.SubPlots[0].XAxis.Min, figure.SubPlots[1].XAxis.Min);
        });
    }
}
