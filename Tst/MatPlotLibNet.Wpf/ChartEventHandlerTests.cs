// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MatPlotLibNet.Wpf.Tests;

/// <summary>One <see cref="IChartEventHandler"/> reacts to a chart wherever it is shown: a server session takes it, and so
/// must the control a native application puts on its window. The interaction host has had the handler since it was written;
/// these tests hold that a reader of the control can reach it, and the failures it reports, from the control they hold.</summary>
public class ChartEventHandlerTests
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

    private static ScrollInputArgs Wheel() => new(400, 300, 0, 3.0);

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

    /// <summary>Paints the control once, the way WPF does, and returns the host it built.</summary>
    private static InteractionHost Painted(object control)
    {
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        control.GetType().GetMethod("OnPaintSurface", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(control, [new SKPaintSurfaceEventArgs(surface, new SKImageInfo(800, 600))]);
        return (InteractionHost)control.GetType()
            .GetField("_host", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(control)!;
    }

    [Fact]
    public void TheChartControl_TellsItsHandlerOfAGesture()
    {
        OnStaThread(() =>
        {
            var handler = new RecordingHandler();
            var control = new MplChartControl { Figure = LineFigure(), IsInteractive = true, ChartEventHandler = handler };

            Painted(control).HandleScroll(Wheel());

            Assert.Equal(["ZoomEvent"], handler.Seen);
            Assert.Same(handler, control.ChartEventHandler);
        });
    }

    [Fact]
    public void AHandlerThatFails_IsReportedOnTheChartControl()
    {
        OnStaThread(() =>
        {
            var failures = new List<Exception>();
            var control = new MplChartControl
            {
                Figure = LineFigure(),
                IsInteractive = true,
                ChartEventHandler = new RecordingHandler { Throw = new InvalidOperationException("boom") },
            };
            control.ChartEventHandlerFaulted += failures.Add;

            var thrown = Record.Exception(() => Painted(control).HandleScroll(Wheel()));

            Assert.Null(thrown);
            Assert.IsType<InvalidOperationException>(Assert.Single(failures));
        });
    }

    [Fact]
    public void TheStreamingControl_TellsItsHandlerOfAGesture()
    {
        OnStaThread(() =>
        {
            using var streaming = new StreamingFigure(LineFigure());
            var handler = new RecordingHandler();
            var control = new MplStreamingChartControl { StreamingFigure = streaming, IsInteractive = true, ChartEventHandler = handler };

            Painted(control).HandleScroll(Wheel());

            Assert.Equal(["ZoomEvent"], handler.Seen);
        });
    }

    [Fact]
    public void AHandlerThatFails_IsReportedOnTheStreamingControl()
    {
        OnStaThread(() =>
        {
            using var streaming = new StreamingFigure(LineFigure());
            var failures = new List<Exception>();
            var control = new MplStreamingChartControl
            {
                StreamingFigure = streaming,
                IsInteractive = true,
                ChartEventHandler = new RecordingHandler { Throw = new InvalidOperationException("boom") },
            };
            control.ChartEventHandlerFaulted += failures.Add;

            Painted(control).HandleScroll(Wheel());

            Assert.IsType<InvalidOperationException>(Assert.Single(failures));
        });
    }
}
