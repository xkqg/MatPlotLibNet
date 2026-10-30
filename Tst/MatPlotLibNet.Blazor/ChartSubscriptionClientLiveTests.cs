// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>What a live chart needs of the one connection its client holds: several components can listen to it at once,
/// the page's script can send its gestures through it, the messages about the other viewers can be handed on to the page,
/// and a connection that was lost and came back is subscribed again (a reconnect is a NEW connection to the server, with
/// no group membership: before this, a chart went silent for good after a network blip).</summary>
public class ChartSubscriptionClientLiveTests : IClassFixture<StreamingHostFixture>
{
    private readonly StreamingHostFixture _fixture;

    public ChartSubscriptionClientLiveTests(StreamingHostFixture fixture) => _fixture = fixture;

    private sealed class Recorder : IChartEventHandler
    {
        public ConcurrentQueue<FigureInteractionEvent> Seen { get; } = new();

        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            Seen.Enqueue(evt);
            return default;
        }
    }

    private static async Task WaitAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(10);
    }

    [Fact]
    public async Task TwoSubscribers_BothReceiveTheSvg_AndDisposingOneStopsIt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var client = new ChartSubscriptionClient();
        var first = new ConcurrentQueue<string>();
        var second = new ConcurrentQueue<string>();
        using var firstToken = client.SubscribeSvgUpdates((id, svg) => { first.Enqueue(id); return Task.CompletedTask; });
        var secondToken = client.SubscribeSvgUpdates((id, svg) => { second.Enqueue(id); return Task.CompletedTask; });
        await client.ConnectAsync(_fixture.HubUrl, ct);
        await client.SubscribeAsync("live-1", ct);
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        await _fixture.Publisher.PublishSvgAsync("live-1", figure, ct);
        await WaitAsync(() => !first.IsEmpty && !second.IsEmpty);
        secondToken.Dispose();
        await _fixture.Publisher.PublishSvgAsync("live-1", figure, ct);
        await WaitAsync(() => first.Count >= 2);
        await Task.Delay(100, ct);

        Assert.Equal(2, first.Count);
        Assert.Single(second);
    }

    [Fact]
    public async Task TheOldSetter_StillReceives_AlongsideTheSubscribers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var client = new ChartSubscriptionClient();
        var setter = new ConcurrentQueue<string>();
        var subscriber = new ConcurrentQueue<string>();
        client.OnSvgUpdated((id, svg) => { setter.Enqueue(id); return Task.CompletedTask; });
        using var token = client.SubscribeSvgUpdates((id, svg) => { subscriber.Enqueue(id); return Task.CompletedTask; });
        await client.ConnectAsync(_fixture.HubUrl, ct);
        await client.SubscribeAsync("live-2", ct);

        await _fixture.Publisher.PublishSvgAsync("live-2", Plt.Create().Plot([1.0], [2.0]).Build(), ct);
        await WaitAsync(() => !setter.IsEmpty && !subscriber.IsEmpty);

        Assert.Single(setter);
        Assert.Single(subscriber);
    }

    [Fact]
    public async Task AGestureTheClientSends_ReachesTheChartsSession()
    {
        var ct = TestContext.Current.CancellationToken;
        var recorder = new Recorder();
        _fixture.Registry.Register("live-3", Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build(), o => o.Handle(recorder));
        await using var client = new ChartSubscriptionClient();
        await client.ConnectAsync(_fixture.HubUrl, ct);
        await client.SubscribeAsync("live-3", ct);

        using var doc = JsonDocument.Parse("{\"chartId\":\"live-3\",\"axesIndex\":0,\"xMin\":1,\"xMax\":2,\"yMin\":3,\"yMax\":4}");
        await ((IChartGestureSender)client).SendGestureAsync("OnZoom", doc.RootElement.Clone(), ct);
        await WaitAsync(() => !recorder.Seen.IsEmpty);

        var zoom = Assert.IsType<ZoomEvent>(Assert.Single(recorder.Seen));
        Assert.Equal(2.0, zoom.XMax);
        Assert.NotNull(zoom.ViewerId);   // the server stamped who sent it
        await _fixture.Registry.UnregisterAsync("live-3");
    }

    [Fact]
    public async Task AGestureSentBeforeTheClientIsConnected_IsDroppedWithoutThrowing()
    {
        await using var client = new ChartSubscriptionClient();
        using var doc = JsonDocument.Parse("{}");

        var thrown = await Record.ExceptionAsync(() =>
            ((IChartGestureSender)client).SendGestureAsync("OnZoom", doc.RootElement.Clone(), TestContext.Current.CancellationToken));

        Assert.Null(thrown);
    }

    [Fact]
    public async Task AfterAReconnect_TheClientIsSubscribedAgain()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var client = new ChartSubscriptionClient();
        await client.ConnectAsync(_fixture.HubUrl, ct);
        await client.SubscribeAsync("live-4", ct);
        Assert.Equal(1, _fixture.Subscriptions.Count("live-4"));

        // A reconnect is a new connection to the server: its groups and its place in the ledger are gone.
        var lost = _fixture.Subscriptions.ChartsOf(await ConnectionIdOf(client, ct));
        Assert.Contains("live-4", lost);
        _fixture.Subscriptions.Disconnected(await ConnectionIdOf(client, ct));
        Assert.Equal(0, _fixture.Subscriptions.Count("live-4"));

        await client.HandleReconnectedAsync();

        Assert.Equal(1, _fixture.Subscriptions.Count("live-4"));
    }

    private static Task<string> ConnectionIdOf(ChartSubscriptionClient client, CancellationToken ct) =>
        Task.FromResult(client.ConnectionId ?? throw new InvalidOperationException("not connected"));

    [Fact]
    public async Task TheServersMessagesAboutOtherViewers_AreHandedToTheMessageSubscribers()
    {
        var ct = TestContext.Current.CancellationToken;
        _fixture.Registry.Register("live-5", Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        await using var watcher = new ChartSubscriptionClient();
        var messages = new ConcurrentQueue<(string Method, string Args)>();
        using var token = ((IChartMessageSource)watcher).SubscribeMessages((method, args) =>
        {
            messages.Enqueue((method, args.GetRawText()));
            return Task.CompletedTask;
        });
        await watcher.ConnectAsync(_fixture.HubUrl, ct);
        await watcher.SubscribeAsync("live-5", ct);
        await using var author = new ChartSubscriptionClient();
        await author.ConnectAsync(_fixture.HubUrl, ct);
        await author.SubscribeAsync("live-5", ct);

        using var doc = JsonDocument.Parse("{\"chartId\":\"live-5\",\"axesIndex\":0,\"xMin\":1,\"xMax\":2,\"yMin\":3,\"yMax\":4}");
        await ((IChartGestureSender)author).SendGestureAsync("OnZoom", doc.RootElement.Clone(), ct);
        await WaitAsync(() => messages.Any(m => m.Method == "ViewChanged"));

        var changed = messages.First(m => m.Method == "ViewChanged");
        Assert.Contains("\"live-5\"", changed.Args);
        Assert.Contains("Viewer 2", changed.Args);   // the author joined second
        await _fixture.Registry.UnregisterAsync("live-5");
    }

    [Fact]
    public async Task TheTooltipTheServerComputes_AndThePointersOfOtherViewers_AreHandedToTheMessageSubscribers()
    {
        var ct = TestContext.Current.CancellationToken;
        _fixture.Registry.Register("live-6", Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build(),
            o => o.OnHover(_ => ValueTask.FromResult<string?>("<b>tip</b>")));
        var seenByWatcher = new ConcurrentQueue<string>();
        var seenByAuthor = new ConcurrentQueue<string>();
        await using var watcher = new ChartSubscriptionClient();
        using var watcherToken = ((IChartMessageSource)watcher).SubscribeMessages((method, _) =>
        {
            seenByWatcher.Enqueue(method);
            return Task.CompletedTask;
        });
        await watcher.ConnectAsync(_fixture.HubUrl, ct);
        await watcher.SubscribeAsync("live-6", ct);
        await using var author = new ChartSubscriptionClient();
        using var authorToken = ((IChartMessageSource)author).SubscribeMessages((method, _) =>
        {
            seenByAuthor.Enqueue(method);
            return Task.CompletedTask;
        });
        await author.ConnectAsync(_fixture.HubUrl, ct);
        await author.SubscribeAsync("live-6", ct);

        using var hover = JsonDocument.Parse("{\"chartId\":\"live-6\",\"axesIndex\":0,\"x\":1.5,\"y\":3.5}");
        using var cursor = JsonDocument.Parse("{\"chartId\":\"live-6\",\"axesIndex\":0,\"x\":1.5,\"y\":3.5}");
        await ((IChartGestureSender)author).SendGestureAsync("OnHover", hover.RootElement.Clone(), ct);
        await ((IChartGestureSender)author).SendGestureAsync("OnCursor", cursor.RootElement.Clone(), ct);
        await WaitAsync(() => seenByAuthor.Contains("ReceiveTooltipContent") && seenByWatcher.Contains("ReceiveCursor"));

        Assert.Contains("ReceiveTooltipContent", seenByAuthor);   // the tooltip goes to the viewer who hovered
        Assert.Contains("ReceiveCursor", seenByWatcher);          // the pointer goes to the others
        await _fixture.Registry.UnregisterAsync("live-6");
    }

    [Fact]
    public async Task AClientThatWasNeverConnected_HasNoConnection_AndNothingToSubscribeAgain()
    {
        await using var client = new ChartSubscriptionClient();

        Assert.Null(client.ConnectionId);
        await client.HandleReconnectedAsync();
    }

    [Fact]
    public async Task ADisposedClient_IsDisposedOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = new ChartSubscriptionClient();
        await client.ConnectAsync(_fixture.HubUrl, ct);

        await client.DisposeAsync();
        var again = await Record.ExceptionAsync(async () => await client.DisposeAsync());

        Assert.Null(again);
        Assert.False(client.IsConnected);
    }
}
