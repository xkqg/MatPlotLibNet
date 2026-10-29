// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A session owns ONE reader that drains every interaction event of a chart, so what one event does to that
/// reader it does to the chart for good: the events keep being accepted by the channel and none is ever applied again, and
/// no error surfaces anywhere. These tests hold the loss to the event that caused it.</summary>
public class ChartSessionTests
{
    /// <summary>A user-defined event whose application fails.</summary>
    private sealed record ThrowingEvent(string ChartId, int AxesIndex, Exception Failure)
        : FigureInteractionEvent(ChartId, AxesIndex)
    {
        public override void ApplyTo(Figure figure) => throw Failure;
    }

    /// <summary>An event written the way subclasses are written against the protected <c>TargetAxes</c>, addressed to a
    /// subplot the figure does not have: it throws out of its own <c>ApplyTo</c>, as it always did.</summary>
    private sealed record SubclassEvent(string ChartId, int AxesIndex) : FigureInteractionEvent(ChartId, AxesIndex)
    {
        public override void ApplyTo(Figure figure) => TargetAxes(figure).XAxis.Min = -1;
    }

    /// <summary>A session on a one-line figure, with a recording publisher and a way to wait until the reader has dealt with
    /// everything written so far: a data-cursor event with a handler that signals. It goes down the notification path, so it
    /// neither changes the figure nor causes a publish, and events are taken in order, so everything before it is done once
    /// it is reached.</summary>
    private sealed class Rig
    {
        private TaskCompletionSource _reached = new();

        public Rig(Action<ChartSessionOptions>? configure = null)
        {
            var options = new ChartSessionOptions().OnDataCursor(async _ =>
            {
                await Task.Yield();
                _reached.TrySetResult();
            });
            configure?.Invoke(options);
            Figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
            Session = new ChartSession("c1", Figure, Publisher, new NullCallerPublisher(), options);
        }

        public Figure Figure { get; }

        public Axes Axes => Figure.SubPlots[0];

        public RecordingPublisher Publisher { get; } = new();

        public ChartSession Session { get; }

        public void Send(FigureInteractionEvent evt) => Assert.True(Session.Events.Writer.TryWrite(evt));

        public async Task DrainedAsync()
        {
            _reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Send(new DataCursorEvent("c1", 0, new PinnedAnnotation("marker", 0, 0, 0, 0, 0)));
            await _reached.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
    }

    /// <summary>One event that throws — a user-defined event, a subclass naming a subplot the figure lacks — must not end the
    /// reader: the events behind it are applied and published, and the session shuts down cleanly afterwards.</summary>
    [Fact]
    public async Task AMalformedEvent_DoesNotEndTheDrain()
    {
        var rig = new Rig();

        rig.Send(new ThrowingEvent("c1", 0, new InvalidOperationException("a failing event")));
        rig.Send(new SubclassEvent("c1", 9));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await rig.DrainedAsync();

        Assert.Equal(0.5, rig.Axes.XAxis.Min);
        Assert.Equal(2.5, rig.Axes.XAxis.Max);
        Assert.Equal(1.0, rig.Axes.YAxis.Min);
        Assert.Equal(9.0, rig.Axes.YAxis.Max);
        await rig.Publisher.WaitForAtLeastAsync(1);
        Assert.NotEmpty(rig.Publisher.Calls);

        // Still alive: an event sent after the failures is applied too.
        rig.Send(new ZoomEvent("c1", 0, 1.5, 3.5, 2, 8));
        await rig.DrainedAsync();
        Assert.Equal(1.5, rig.Axes.XAxis.Min);

        await rig.Session.DisposeAsync();
    }

    /// <summary>An event that fails is counted, once each, and the count says nothing about the events that were fine. The last
    /// failure is kept for whoever looks at the count.</summary>
    [Fact]
    public async Task AnEventThatThrows_IsCounted()
    {
        var rig = new Rig();

        rig.Send(new ThrowingEvent("c1", 0, new InvalidOperationException("a failing event")));
        rig.Send(new SubclassEvent("c1", 9));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await rig.DrainedAsync();

        Assert.Equal(2, rig.Session.FaultedEventCount);
        Assert.Equal(0, rig.Session.DroppedEventCount);
        Assert.IsType<ArgumentOutOfRangeException>(rig.Session.LastFault);

        await rig.Session.DisposeAsync();
    }

    /// <summary>The loss is bounded by the events that caused it: of six events, the two naming a subplot the figure lacks (one
    /// below 0, one past the end) are dropped and counted, the four that name a real one are applied in order, and the
    /// session is still alive for the next.</summary>
    [Fact]
    public async Task AnEventForAMissingSubplot_IsCountedAsDropped_AndEveryOtherEventIsApplied()
    {
        var rig = new Rig();

        rig.Send(new ZoomEvent("c1", 7, 100, 200, 100, 200));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        rig.Send(new PanEvent("c1", -1, 5, 5));
        rig.Send(new PanEvent("c1", 0, 1, 1));
        rig.Send(new LegendToggleEvent("c1", 0, 0));
        rig.Send(new ZoomEvent("c1", 0, 1.5, 3.5, 2, 8));
        await rig.DrainedAsync();

        Assert.Equal(1.5, rig.Axes.XAxis.Min);
        Assert.Equal(3.5, rig.Axes.XAxis.Max);
        Assert.Equal(2.0, rig.Axes.YAxis.Min);
        Assert.Equal(8.0, rig.Axes.YAxis.Max);
        Assert.False(((ChartSeries)rig.Axes.Series[0]).Visible);
        Assert.Equal(2, rig.Session.DroppedEventCount);
        Assert.Equal(0, rig.Session.FaultedEventCount);
        Assert.Null(rig.Session.LastFault);

        rig.Send(new ZoomEvent("c1", 0, 4, 5, 4, 5));
        await rig.DrainedAsync();
        Assert.Equal(4.0, rig.Axes.XAxis.Min);
        Assert.Equal(2, rig.Session.DroppedEventCount);

        await rig.Session.DisposeAsync();
    }

    /// <summary>A burst made only of events that name no subplot publishes nothing: nothing in the figure changed, so there is
    /// nothing to send the chart's subscribers.</summary>
    [Fact]
    public async Task ABurstOfDroppedEvents_PublishesNothing()
    {
        var rig = new Rig();

        rig.Send(new ZoomEvent("c1", 3, 100, 200, 100, 200));
        rig.Send(new PanEvent("c1", -4, 5, 5));
        await rig.DrainedAsync();

        Assert.Equal(2, rig.Session.DroppedEventCount);
        Assert.Empty(rig.Publisher.Calls);

        await rig.Session.DisposeAsync();
    }

    /// <summary>The failure of a user's handler is a failure of that one event too: the brush-select handler throws, the zoom
    /// behind it is still applied.</summary>
    [Fact]
    public async Task AHandlerThatThrows_DoesNotEndTheDrain()
    {
        var rig = new Rig(options => options.OnBrushSelect(_ => throw new InvalidOperationException("a failing handler")));

        rig.Send(new BrushSelectEvent("c1", 0, 0, 0, 1, 1));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await rig.DrainedAsync();

        Assert.Equal(0.5, rig.Axes.XAxis.Min);
        Assert.Equal(1, rig.Session.FaultedEventCount);

        await rig.Session.DisposeAsync();
    }

    /// <summary>A cancellation is not a failure of an event: it is how a session shuts down, and it still ends the drain. It is
    /// not counted as a fault, nothing behind it is applied, and the session disposes cleanly.</summary>
    [Fact]
    public async Task ACancellation_StillEndsTheDrain_AndIsNotCounted()
    {
        var rig = new Rig();

        rig.Send(new ThrowingEvent("c1", 0, new OperationCanceledException()));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await Task.Delay(150, TestContext.Current.CancellationToken);

        Assert.Equal(0, rig.Session.FaultedEventCount);
        Assert.Null(rig.Axes.XAxis.Min);
        Assert.Empty(rig.Publisher.Calls);

        await rig.Session.DisposeAsync();
    }

    /// <summary>A handler that takes its time holds the reader: the brush-select handler is awaited, and the zoom behind it is
    /// not applied until the handler has finished, then it is.</summary>
    [Fact]
    public async Task ASlowBrushHandler_IsAwaited_BeforeTheNextEventIsTaken()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rig = new Rig(options => options.OnBrushSelect(async _ => await release.Task));

        rig.Send(new BrushSelectEvent("c1", 0, 0, 0, 1, 1));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await Task.Delay(150, TestContext.Current.CancellationToken);
        Assert.Null(rig.Axes.XAxis.Min);

        release.SetResult();
        await rig.DrainedAsync();
        Assert.Equal(0.5, rig.Axes.XAxis.Min);
        Assert.Equal(0, rig.Session.FaultedEventCount);

        await rig.Session.DisposeAsync();
    }

    /// <summary>The same for the hover handler: its tooltip is awaited before the next event is taken.</summary>
    [Fact]
    public async Task ASlowHoverHandler_IsAwaited_BeforeTheNextEventIsTaken()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rig = new Rig(options => options.OnHover(async _ =>
        {
            await release.Task;
            return (string?)null;
        }));

        rig.Send(new HoverEvent("c1", 0, 1, 4));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await Task.Delay(150, TestContext.Current.CancellationToken);
        Assert.Null(rig.Axes.XAxis.Min);

        release.SetResult();
        await rig.DrainedAsync();
        Assert.Equal(0.5, rig.Axes.XAxis.Min);
        Assert.Equal(0, rig.Session.FaultedEventCount);

        await rig.Session.DisposeAsync();
    }

    /// <summary>A closed channel is the other way a session shuts down, and it ends the drain just as a cancellation does: it is
    /// not counted as a fault, nothing behind it is applied, and the session disposes cleanly.</summary>
    [Fact]
    public async Task AClosedChannel_StillEndsTheDrain_AndIsNotCounted()
    {
        var rig = new Rig();

        rig.Send(new ThrowingEvent("c1", 0, new System.Threading.Channels.ChannelClosedException()));
        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await Task.Delay(150, TestContext.Current.CancellationToken);

        Assert.Equal(0, rig.Session.FaultedEventCount);
        Assert.Null(rig.Axes.XAxis.Min);
        Assert.Empty(rig.Publisher.Calls);

        await rig.Session.DisposeAsync();
    }

    private sealed class NullCallerPublisher : ICallerPublisher
    {
        public Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class RecordingPublisher : IChartPublisher
    {
        public ConcurrentQueue<string> Calls { get; } = new();

        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            Calls.Enqueue(chartId);
            return Task.CompletedTask;
        }

        public async Task WaitForAtLeastAsync(int n)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (Calls.Count < n && DateTime.UtcNow < deadline)
                await Task.Delay(10);
        }
    }
}
