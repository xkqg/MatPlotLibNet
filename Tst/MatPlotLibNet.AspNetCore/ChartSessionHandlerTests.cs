// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Covers v1.2.2's per-chart handler routing: brush-select handlers fire (fire-and-forget),
/// hover handlers run and the returned tooltip HTML routes to <see cref="ICallerPublisher"/>
/// (not to the broadcast channel), events with no handler are silent no-ops, and a mutation
/// event interleaved with notifications still triggers one publish per drained batch.</summary>
public class ChartSessionHandlerTests
{
    private static Figure NewFigure() => Plt.Create()
        .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
        .Build();

    [Fact]
    public async Task BrushSelectHandler_Fires_OnEvent_NoRepublish()
    {
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);

        var received = new TaskCompletionSource<BrushSelectEvent>();
        var figure = NewFigure();
        registry.Register("c1", figure, opts =>
            opts.OnBrushSelect(evt => { received.SetResult(evt); return default; }));

        var ok = registry.Publish("c1", new BrushSelectEvent("c1", 0, 1.0, 2.0, 3.0, 4.0));
        Assert.True(ok);

        var evt = await received.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal(1.0, evt.X1);
        Assert.Equal(2.0, evt.Y1);
        Assert.Equal(3.0, evt.X2);
        Assert.Equal(4.0, evt.Y2);

        await Task.Delay(100, TestContext.Current.CancellationToken); // give the reader task time to settle
        Assert.Empty(publisher.Calls);  // notification events never republish
        Assert.Empty(caller.Calls);     // brush-select does not use caller publisher

        await registry.UnregisterAsync("c1");
    }

    [Fact]
    public async Task HoverHandler_ReturnsContent_RoutedToCallerPublisher()
    {
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);

        var figure = NewFigure();
        registry.Register("c1", figure, opts =>
            opts.OnHover(evt => ValueTask.FromResult<string?>(System.FormattableString.Invariant($"<b>x={evt.X},y={evt.Y}</b>"))));

        var ok = registry.Publish("c1",
            new HoverEvent("c1", 0, 1.5, 2.5, CallerConnectionId: "conn-A"));
        Assert.True(ok);

        await caller.WaitForAtLeastAsync(1);
        var call = Assert.Single(caller.Calls);
        Assert.Equal("conn-A", call.ConnectionId);
        Assert.Equal("c1", call.ChartId);
        Assert.Equal("<b>x=1.5,y=2.5</b>", call.Html);
        Assert.Empty(publisher.Calls);  // hover never broadcasts

        await registry.UnregisterAsync("c1");
    }

    [Fact]
    public async Task HoverHandler_ReturnsNull_DoesNotInvokeCaller()
    {
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);

        registry.Register("c1", NewFigure(), opts =>
            opts.OnHover(_ => ValueTask.FromResult<string?>(null)));

        registry.Publish("c1", new HoverEvent("c1", 0, 1, 2, CallerConnectionId: "conn-A"));
        await Task.Delay(150, TestContext.Current.CancellationToken);

        Assert.Empty(caller.Calls);
        await registry.UnregisterAsync("c1");
    }

    [Fact]
    public async Task NotificationEvent_NoHandler_IsSilentNoOp()
    {
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);
        var figure = NewFigure();

        // No options.OnBrushSelect / OnHover registered
        registry.Register("c1", figure);

        registry.Publish("c1", new BrushSelectEvent("c1", 0, 1, 2, 3, 4));
        registry.Publish("c1", new HoverEvent("c1", 0, 5, 6, CallerConnectionId: "A"));
        await Task.Delay(150, TestContext.Current.CancellationToken);

        Assert.Empty(publisher.Calls);
        Assert.Empty(caller.Calls);
        await registry.UnregisterAsync("c1");
    }

    [Fact]
    public async Task MutationAfterNotification_StillPublishes_Once()
    {
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);
        var figure = NewFigure();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;

        var brushFired = new TaskCompletionSource<bool>();
        registry.Register("c1", figure, opts =>
            opts.OnBrushSelect(_ => { brushFired.TrySetResult(true); return default; }));

        registry.Publish("c1", new BrushSelectEvent("c1", 0, 0, 0, 1, 1));
        registry.Publish("c1", new ZoomEvent("c1", 0, 2.0, 8.0, 2.0, 8.0));

        await brushFired.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await publisher.WaitForAtLeastAsync(1);

        Assert.Equal(2.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(8.0, figure.SubPlots[0].XAxis.Max);
        Assert.Single(publisher.Calls);  // one publish per batch, even with mixed events

        await registry.UnregisterAsync("c1");
    }

    [Fact]
    public async Task ExistingRegister_WithoutOptions_BackwardCompatible()
    {
        // v1.2.0 API: Register(chartId, figure) with no options overload
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);
        var figure = NewFigure();

        registry.Register("legacy", figure);
        registry.Publish("legacy", new ZoomEvent("legacy", 0, 1, 2, 1, 2));

        await publisher.WaitForAtLeastAsync(1);
        Assert.Single(publisher.Calls);

        await registry.UnregisterAsync("legacy");
    }

    // ---- Test doubles ----

    private sealed class RecordingPublisher : IChartPublisher
    {
        public ConcurrentQueue<(string ChartId, Figure Figure)> Calls { get; } = new();
        private readonly SemaphoreSlim _signal = new(0);

        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            Calls.Enqueue((chartId, figure));
            _signal.Release();
            return Task.CompletedTask;
        }

        public async Task WaitForAtLeastAsync(int n)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (Calls.Count < n && DateTime.UtcNow < deadline)
                await Task.Delay(10);
        }
    }

    private sealed class RecordingCallerPublisher : ICallerPublisher
    {
        public ConcurrentQueue<(string ConnectionId, string ChartId, string Html)> Calls { get; } = new();

        public Task SendTooltipAsync(string connectionId, string chartId, string html,
            CancellationToken ct = default)
        {
            Calls.Enqueue((connectionId, chartId, html));
            return Task.CompletedTask;
        }

        public async Task WaitForAtLeastAsync(int n)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (Calls.Count < n && DateTime.UtcNow < deadline)
                await Task.Delay(10);
        }
    }

    /// <summary>A clicked point reaches the handler with the point in it, and nothing else happens: no
    /// re-render, no broadcast, no caller round-trip. It is the brush-select shape, not the hover shape,
    /// because there is no answer to send back.</summary>
    [Fact]
    public async Task DataCursorHandler_FiresWithTheClickedPoint_AndNothingIsRepublished()
    {
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);

        var received = new TaskCompletionSource<DataCursorEvent>();
        registry.Register("c1", NewFigure(), opts =>
            opts.OnDataCursor(evt => { received.SetResult(evt); return default; }));

        Assert.True(registry.Publish("c1", new DataCursorEvent("c1", 0,
            new PinnedAnnotation("load", 1.5, 5.5, 100, 50, 0))));

        var evt = await received.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal("load", evt.Annotation.SeriesLabel);
        Assert.Equal(1.5, evt.Annotation.DataX);
        Assert.Equal(5.5, evt.Annotation.DataY);

        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Empty(publisher.Calls);   // a click asks about the data; it never re-renders it
        Assert.Empty(caller.Calls);      // and there is no answer to send back

        await registry.UnregisterAsync("c1");
    }

    /// <summary>With no handler registered the click goes where every unhandled notification goes: nowhere,
    /// quietly. It must not fall through to the mutation arm.</summary>
    [Fact]
    public async Task ADataCursorEvent_WithNoHandler_ChangesNothing()
    {
        var publisher = new RecordingPublisher();
        var caller    = new RecordingCallerPublisher();
        var registry  = new FigureRegistry(publisher, caller);

        registry.Register("c1", NewFigure());
        registry.Publish("c1", new DataCursorEvent("c1", 0, new PinnedAnnotation("load", 1.5, 5.5, 100, 50, 0)));
        await Task.Delay(150, TestContext.Current.CancellationToken);

        Assert.Empty(publisher.Calls);
        Assert.Empty(caller.Calls);
        await registry.UnregisterAsync("c1");
    }
}
