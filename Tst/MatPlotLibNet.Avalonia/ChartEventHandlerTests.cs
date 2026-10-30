// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>One <see cref="IChartEventHandler"/> reacts to a chart wherever it is shown: a server session takes it, and so
/// must the control a native application puts on its window. The interaction host has had the handler since it was written;
/// these tests hold that a reader of the control can reach it, and the failures it reports, from the control they hold.</summary>
public class ChartEventHandlerTests
{
    private static (Figure Figure, ChartLayout Layout) Plotted()
    {
        var figure = Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 5.0, 10.0], s => s.Label = "load").Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        return (figure, ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]));
    }

    private static ScrollInputArgs Wheel() => new(100, 50, 0, -120);

    private sealed class RecordingHandler : IChartEventHandler
    {
        public List<string> Seen { get; } = [];
        public Exception? Throw { get; init; }

        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            Seen.Add(evt.GetType().Name);
            return Throw is null ? default : ValueTask.FromException(Throw);
        }
    }

    private static InteractionHost HostOf(object control) =>
        (InteractionHost)control.GetType().GetField("_host", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(control)!;

    [Fact]
    public void TheChartControl_TellsItsHandlerOfAGesture()
    {
        var (figure, layout) = Plotted();
        var handler = new RecordingHandler();
        var control = new MplChartControl { Figure = figure, IsInteractive = true, ChartEventHandler = handler };

        control.Attach(figure, layout).HandleScroll(Wheel());

        Assert.Equal(["ZoomEvent"], handler.Seen);
        Assert.Same(handler, control.ChartEventHandler);
    }

    [Fact]
    public void AHandlerThatFails_IsReportedOnTheChartControl_AndTheGestureStillApplies()
    {
        var (figure, layout) = Plotted();
        var failures = new List<Exception>();
        var control = new MplChartControl
        {
            Figure = figure,
            IsInteractive = true,
            ChartEventHandler = new RecordingHandler { Throw = new InvalidOperationException("boom") },
        };
        control.ChartEventHandlerFaulted += failures.Add;

        var thrown = Record.Exception(() => control.Attach(figure, layout).HandleScroll(Wheel()));

        Assert.Null(thrown);
        Assert.IsType<InvalidOperationException>(Assert.Single(failures));
        Assert.True(figure.SubPlots[0].XAxis.Max < 2);   // the zoom itself was applied
    }

    [Fact]
    public void AHandlerThatFails_WithNobodyListeningForFailures_IsHarmlessOnBothControls()
    {
        var (figure, layout) = Plotted();
        var chart = new MplChartControl
        {
            Figure = figure,
            IsInteractive = true,
            ChartEventHandler = new RecordingHandler { Throw = new InvalidOperationException("boom") },
        };
        using var streaming = new StreamingFigure(figure);
        var streamingControl = new MplStreamingChartControl
        {
            StreamingFigure = streaming,
            IsInteractive = true,
            ChartEventHandler = new RecordingHandler { Throw = new InvalidOperationException("boom") },
        };
        var host = HostOf(streamingControl);
        host.Update(figure, layout);

        Assert.Null(Record.Exception(() => chart.Attach(figure, layout).HandleScroll(Wheel())));
        Assert.Null(Record.Exception(() => host.HandleScroll(Wheel())));
    }

    [Fact]
    public void TheStreamingControl_TellsItsHandlerOfAGesture()
    {
        var (figure, layout) = Plotted();
        using var streaming = new StreamingFigure(figure);
        var handler = new RecordingHandler();
        var control = new MplStreamingChartControl { StreamingFigure = streaming, IsInteractive = true, ChartEventHandler = handler };
        var host = HostOf(control);

        host.Update(figure, layout);
        host.HandleScroll(Wheel());

        Assert.Equal(["ZoomEvent"], handler.Seen);
    }

    [Fact]
    public void AHandlerThatFails_IsReportedOnTheStreamingControl()
    {
        var (figure, layout) = Plotted();
        using var streaming = new StreamingFigure(figure);
        var failures = new List<Exception>();
        var control = new MplStreamingChartControl
        {
            StreamingFigure = streaming,
            IsInteractive = true,
            ChartEventHandler = new RecordingHandler { Throw = new InvalidOperationException("boom") },
        };
        control.ChartEventHandlerFaulted += failures.Add;
        var host = HostOf(control);

        host.Update(figure, layout);
        host.HandleScroll(Wheel());

        Assert.IsType<InvalidOperationException>(Assert.Single(failures));
    }
}
