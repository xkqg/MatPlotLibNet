// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>What several viewers of one chart see of each other: where the others' pointers are, that one of them has
/// moved the shared view and who, and that one has left. A pointer is relayed to the others in the chart, never back to the
/// viewer it belongs to and never through the session that applies the gestures, so a busy pointer cannot queue behind a
/// slow handler.</summary>
public class ViewerPresenceTests : IAsyncDisposable
{
    private readonly IHost _host;

    public ViewerPresenceTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddMatPlotLibNetSignalR();
                    services.AddRouting();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapChartHub());
                });
            })
            .Start();
    }

    private sealed class Client
    {
        public HubConnection Connection { get; init; } = default!;
        public ConcurrentQueue<(string ViewerId, string ViewerName, int Axes, double X, double Y)> Cursors { get; } = new();
        public ConcurrentQueue<(string ViewerId, string ViewerName)> Changes { get; } = new();
        public ConcurrentQueue<string> Left { get; } = new();

        public async Task WaitAsync(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(10);
        }
    }

    private async Task<Client> JoinAsync(string chartId, CancellationToken ct)
    {
        var conn = new HubConnectionBuilder()
            .WithUrl("http://localhost/charts-hub", o => o.HttpMessageHandlerFactory = _ => _host.GetTestServer().CreateHandler())
            .Build();
        var client = new Client { Connection = conn };
        conn.On<string, string, string, int, double, double>("ReceiveCursor",
            (_, id, name, axes, x, y) => client.Cursors.Enqueue((id, name, axes, x, y)));
        conn.On<string, string, string>("ViewChanged", (_, id, name) => client.Changes.Enqueue((id, name)));
        conn.On<string, string>("ViewerLeft", (_, id) => client.Left.Enqueue(id));
        conn.On<string, string>("UpdateChartSvg", (_, _) => { });
        await conn.StartAsync(ct);
        await conn.InvokeAsync("Subscribe", chartId, cancellationToken: ct);
        return client;
    }

    [Fact]
    public async Task APointer_IsRelayedToTheOtherViewers_NotBackToItsOwner()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = await JoinAsync("p-1", ct);
        var second = await JoinAsync("p-1", ct);
        var third = await JoinAsync("p-1", ct);

        await first.Connection.InvokeAsync(nameof(ChartHub.OnCursor), new CursorEventPayload("p-1", 0, 1.5, 2.5), ct);
        await second.WaitAsync(() => !second.Cursors.IsEmpty);
        await third.WaitAsync(() => !third.Cursors.IsEmpty);
        await Task.Delay(150, ct);

        var seen = Assert.Single(second.Cursors);
        Assert.Equal((first.Connection.ConnectionId, "Viewer 1", 0, 1.5, 2.5), seen);
        Assert.Single(third.Cursors);
        Assert.Empty(first.Cursors);

        foreach (var c in new[] { first, second, third }) await c.Connection.DisposeAsync();
    }

    [Fact]
    public async Task APointer_FromAConnectionNotInTheChart_IsNotRelayedIntoIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var inside = await JoinAsync("p-2", ct);
        var stranger = await JoinAsync("p-other", ct);

        await stranger.Connection.InvokeAsync(nameof(ChartHub.OnCursor), new CursorEventPayload("p-2", 0, 1, 1), ct);
        await Task.Delay(200, ct);

        Assert.Empty(inside.Cursors);
        await inside.Connection.DisposeAsync();
        await stranger.Connection.DisposeAsync();
    }

    [Fact]
    public async Task AViewerWhoLeaves_IsToldToTheOthers_SoTheirPointerCanGo()
    {
        var ct = TestContext.Current.CancellationToken;
        var staying = await JoinAsync("p-3", ct);
        var leaving = await JoinAsync("p-3", ct);
        string leavingId = leaving.Connection.ConnectionId!;

        await leaving.Connection.DisposeAsync();
        await staying.WaitAsync(() => !staying.Left.IsEmpty);

        Assert.Equal(leavingId, Assert.Single(staying.Left));
        await staying.Connection.DisposeAsync();
    }

    [Fact]
    public async Task AViewChange_IsPublishedNamingItsAuthor_ToEveryViewerOfTheChart()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        registry.Register("p-4", Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        var author = await JoinAsync("p-4", ct);
        var watcher = await JoinAsync("p-4", ct);

        await author.Connection.InvokeAsync(nameof(ChartHub.OnZoom), new ZoomEvent("p-4", 0, 1, 2, 3, 4), ct);
        await watcher.WaitAsync(() => !watcher.Changes.IsEmpty);

        Assert.Equal((author.Connection.ConnectionId!, "Viewer 1"), Assert.Single(watcher.Changes));
        await author.Connection.DisposeAsync();
        await watcher.Connection.DisposeAsync();
        await registry.UnregisterAsync("p-4");
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
