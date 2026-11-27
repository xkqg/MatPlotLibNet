// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>What the chart control and the streaming control are alike is their <see cref="MplChartSurface"/>'s: the handler, its
/// failures and the clicked point reach the reader from the control they hold, and either control is wired to a server by the one
/// route. What the host does with a gesture is held by the core's <c>InteractionHostTests</c>; here only what the surface passes on.</summary>
public class MplChartSurfaceTests
{
    /// <summary>A line through (0, 0), (1, 5), (2, 10) on limits 0..2 and 0..10, aimed at as if a 200 by 100 paint had drawn it: the
    /// point (1, 5) sits at pixel (100, 50).</summary>
    private static void Aim(MplChartSurface control, Figure figure)
    {
        var axes = figure.SubPlots[0];
        axes.XAxis.Min = 0; axes.XAxis.Max = 2;
        axes.YAxis.Min = 0; axes.YAxis.Max = 10;
        control.Host.Update(figure, ChartLayout.Create(figure, [new MatPlotLibNet.Rendering.Rect(0, 0, 200, 100)]));
    }

    private static Figure Line() => Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 5.0, 10.0], s => s.Label = "load").Build();

    [Fact]
    public void TheHandler_ItsFailures_AndTheClickedPoint_ReachTheReaderFromTheControl() => new Action(() =>
    {
        using var stream = new StreamingFigure(Line());
        var handler = new RecordingChartEventHandler { Throw = new InvalidOperationException("boom") };
        int made = 0;
        InteractionControllerFactory factory = context => { made++; return InteractionController.Create(context); };
        var control = new MplStreamingChartControl
        {
            StreamingFigure = stream, IsInteractive = true, ChartEventHandler = handler, ControllerFactory = factory,
        };
        var failures = new List<Exception>();
        var clicks = new List<PinnedAnnotation>();
        control.ChartEventHandlerFaulted += failures.Add;
        control.DataPointClicked += clicks.Add;
        Aim(control, stream.Figure);

        control.Host.HandlePointerPressed(new PointerInputArgs(100, 50, PointerButton.Left, ModifierKeys.None));

        Assert.Same(handler, control.ChartEventHandler);
        Assert.Same(factory, control.ControllerFactory);   // the control's factory made the controller the gestures went through
        Assert.Equal(1, made);
        Assert.NotEmpty(handler.Names);
        Assert.All(failures, failure => Assert.IsType<InvalidOperationException>(failure));
        Assert.NotEmpty(failures);
        var click = Assert.Single(clicks);
        Assert.Equal("load", click.SeriesLabel);
        Assert.Equal(5.0, click.DataY);
    }).OnStaThread();

    [Fact]
    public void WithServerInteraction_MakesEitherControlInteractive_SendsItsGesturesThroughTheDelegate_AndHandsItBack() => new Action(() =>
    {
        var probe = new ServerInteractionProbe();
        var chart = new MplChartControl();
        var streaming = new MplStreamingChartControl();
        Assert.Same(chart, chart.WithServerInteraction(probe.Send));
        Assert.Same(streaming, streaming.WithServerInteraction(probe.Send));
        probe.AssertEveryGestureWasSent(new(chart.IsInteractive, chart.Host), new(streaming.IsInteractive, streaming.Host));
    }).OnStaThread();
}
