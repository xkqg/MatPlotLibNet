// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>One handler for everything a viewer does to a chart. A chart used to offer a callback for each of three
/// gestures (a brush, a hover, a click) and nothing for the rest, so a zoom, a pan, a legend toggle or a reset could not
/// be observed in .NET code at all. <see cref="IChartEventHandler"/> receives every event once, after the callback written
/// for its kind, and a session survives whatever the handler, or the publisher behind it, throws.</summary>
public class ChartEventHandlerTests
{
    private sealed class RecordingHandler : IChartEventHandler
    {
        public ConcurrentQueue<FigureInteractionEvent> Seen { get; } = new();
        public ConcurrentQueue<string> Order { get; } = new();

        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            Seen.Enqueue(evt);
            Order.Enqueue("handler:" + evt.GetType().Name);
            return default;
        }
    }

    private sealed class ThrowingOnZoomHandler : IChartEventHandler
    {
        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context) =>
            evt is ZoomEvent ? throw new InvalidOperationException("the handler failed") : default;
    }

    private sealed class Publisher : IChartPublisher
    {
        public int Calls;
        public bool ThrowOnce;

        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Calls);
            if (ThrowOnce)
            {
                ThrowOnce = false;
                throw new InvalidOperationException("the publish failed");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class Callers : ICallerPublisher
    {
        public ConcurrentQueue<(string ConnectionId, string Html)> Sent { get; } = new();

        public Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default)
        {
            Sent.Enqueue((connectionId, html));
            return Task.CompletedTask;
        }
    }

    private sealed class Rig
    {
        private TaskCompletionSource _reached = new();

        public Rig(IChartEventHandler? handler, Action<ChartSessionOptions>? configure = null)
        {
            var options = new ChartSessionOptions().OnDataCursor(async _ =>
            {
                await Task.Yield();
                _reached.TrySetResult();
            });
            if (handler is not null) options.Handle(handler);
            configure?.Invoke(options);
            Figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
            Session = new ChartSession("c1", Figure, Publisher, Callers, options);
        }

        public Figure Figure { get; }
        public Publisher Publisher { get; } = new();
        public Callers Callers { get; } = new();
        public ChartSession Session { get; }

        public void Send(FigureInteractionEvent evt) => Assert.True(Session.Events.Writer.TryWrite(evt));

        public async Task DrainedAsync()
        {
            _reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Send(new DataCursorEvent("c1", 0, new PinnedAnnotation("marker", 0, 0, 0, 0, 0)));
            await _reached.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TheHandler_SeesEveryEventOnce_MutationsAndNotificationsAlike()
    {
        var handler = new RecordingHandler();
        var rig = new Rig(handler);

        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        rig.Send(new LegendToggleEvent("c1", 0, 0));
        rig.Send(new BrushSelectEvent("c1", 0, 0, 0, 1, 1));
        rig.Send(new ResetEvent("c1", 0, 1, 3, 4, 6));
        await rig.DrainedAsync();

        Assert.Equal(["ZoomEvent", "LegendToggleEvent", "BrushSelectEvent", "ResetEvent", "DataCursorEvent"],
            handler.Seen.Select(e => e.GetType().Name));
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task TheCallbackOfTheKind_RunsBeforeTheHandler()
    {
        var handler = new RecordingHandler();
        var rig = new Rig(handler, o => o.OnBrushSelect(_ =>
        {
            handler.Order.Enqueue("callback:BrushSelectEvent");
            return default;
        }));

        rig.Send(new BrushSelectEvent("c1", 0, 0, 0, 1, 1));
        await rig.DrainedAsync();

        Assert.Equal(["callback:BrushSelectEvent", "handler:BrushSelectEvent"], handler.Order.Take(2));
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task AnEventTheFigureCouldNotTake_IsNotPassedToTheHandler()
    {
        var handler = new RecordingHandler();
        var rig = new Rig(handler);

        rig.Send(new ZoomEvent("c1", 9, 0, 1, 0, 1));   // names a subplot the figure lacks
        await rig.DrainedAsync();

        Assert.DoesNotContain(handler.Seen, e => e is ZoomEvent);
        Assert.Equal(1, rig.Session.DroppedEventCount);
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task AHandlerThatThrows_IsCounted_AndTheNextEventIsStillTaken()
    {
        var rig = new Rig(new ThrowingOnZoomHandler());

        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        rig.Send(new LegendToggleEvent("c1", 0, 0));
        await rig.DrainedAsync();

        Assert.Equal(1, rig.Session.FaultedEventCount);
        Assert.IsType<InvalidOperationException>(rig.Session.LastFault);
        Assert.Equal(0.5, rig.Figure.SubPlots[0].XAxis.Min!.Value, 6);              // the zoom itself was applied before the handler failed
        Assert.False(((MatPlotLibNet.Models.Series.ChartSeries)rig.Figure.SubPlots[0].Series[0]).Visible);   // and the event behind it was taken
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task AFailedPublish_DoesNotEndTheReader()
    {
        var rig = new Rig(handler: null);
        rig.Publisher.ThrowOnce = true;

        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await rig.DrainedAsync();
        Assert.Equal(1, rig.Session.FaultedEventCount);   // the publish failure is counted like any other fault

        rig.Send(new ZoomEvent("c1", 0, 1.0, 2.0, 2, 8));
        await rig.DrainedAsync();

        Assert.Equal(1.0, rig.Figure.SubPlots[0].XAxis.Min!.Value, 6);
        Assert.True(rig.Publisher.Calls >= 2);            // the second zoom was published: the reader was still alive
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task TwoViewersHoveringInOneBurst_EachGetTheirOwnTooltip()
    {
        var rig = new Rig(handler: null, o => o.OnHover(e => ValueTask.FromResult<string?>($"tip for {e.CallerConnectionId}")));

        rig.Send(new HoverEvent("c1", 0, 1, 4, CallerConnectionId: "A"));
        rig.Send(new HoverEvent("c1", 0, 2, 5, CallerConnectionId: "B"));
        await rig.DrainedAsync();

        Assert.Equal(["A", "B"], rig.Callers.Sent.Select(s => s.ConnectionId).Order());
        Assert.Equal("tip for A", rig.Callers.Sent.Single(s => s.ConnectionId == "A").Html);
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task ABurstOfHoversFromOneViewer_IsAnsweredOnce_WithTheLatest()
    {
        var rig = new Rig(handler: null, o => o.OnHover(e => ValueTask.FromResult<string?>($"x={e.X}")));

        rig.Send(new HoverEvent("c1", 0, 1, 4, CallerConnectionId: "A"));
        rig.Send(new HoverEvent("c1", 0, 2, 5, CallerConnectionId: "A"));
        rig.Send(new HoverEvent("c1", 0, 3, 6, CallerConnectionId: "A"));
        await rig.DrainedAsync();

        var only = Assert.Single(rig.Callers.Sent);
        Assert.Equal("x=3", only.Html);
        await rig.Session.DisposeAsync();
    }
}
