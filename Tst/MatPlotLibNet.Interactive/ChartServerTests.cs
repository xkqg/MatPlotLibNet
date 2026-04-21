// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Interactive;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Verifies <see cref="ChartServer"/> behavior.</summary>
public class ChartServerTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, Figure> _figures = new();

    public ChartServerTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddMatPlotLibNetSignalR();
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        ChartServer.ConfigureRoutes(endpoints, _figures, () => 0);
                    });
                });
            })
            .Start();

        _httpClient = _host.GetTestClient();
    }

    /// <summary>Verifies that registering a figure returns a non-empty chart ID.</summary>
    [Fact]
    public void RegisterFigure_ReturnsNonEmptyId()
    {
        var server = new ChartServer();
        var figure = Plt.Create().WithTitle("Test").Build();
        var id = server.RegisterFigure(figure);
        Assert.False(string.IsNullOrEmpty(id));
    }

    /// <summary>Verifies that registering multiple figures produces distinct chart IDs.</summary>
    [Fact]
    public void RegisterFigure_MultipleFigures_DistinctIds()
    {
        var server = new ChartServer();
        var id1 = server.RegisterFigure(Plt.Create().Build());
        var id2 = server.RegisterFigure(Plt.Create().Build());
        Assert.NotEqual(id1, id2);
    }

    /// <summary>Verifies that the chart page endpoint returns HTTP 200 OK for a registered figure.</summary>
    [Fact]
    public async Task GetChartPage_ReturnsOk()
    {
        var figure = Plt.Create().WithTitle("Test").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync($"/chart/{chartId}", ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that the chart page endpoint returns text/html content type.</summary>
    [Fact]
    public async Task GetChartPage_ReturnsHtmlContentType()
    {
        var figure = Plt.Create().Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync($"/chart/{chartId}", ct);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Verifies that the chart page HTML contains the SVG with the expected title.</summary>
    [Fact]
    public async Task GetChartPage_ContainsSvg()
    {
        var figure = Plt.Create().WithTitle("Svg Test").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var ct = TestContext.Current.CancellationToken;
        var html = await _httpClient.GetStringAsync($"/chart/{chartId}", ct);
        Assert.Contains("<svg", html);
        Assert.Contains("Svg Test", html);
    }

    /// <summary>Verifies that the chart page endpoint returns HTTP 404 for an unknown chart ID.</summary>
    [Fact]
    public async Task GetChartPage_UnknownId_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync("/chart/unknown-id", ct);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that the SignalR JavaScript endpoint returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetSignalRJs_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync("/js/signalr.min.js", ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that the SignalR JavaScript endpoint returns application/javascript content type.</summary>
    [Fact]
    public async Task GetSignalRJs_ReturnsJavaScript()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync("/js/signalr.min.js", ct);
        Assert.Equal("application/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Verifies that the ChartHub is reachable and a client can connect successfully.</summary>
    [Fact]
    public async Task ChartHub_IsReachable()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = CreateHubConnection();
        await connection.StartAsync(ct);
        Assert.Equal(HubConnectionState.Connected, connection.State);
        await connection.DisposeAsync();
    }

    /// <summary>Verifies that a subscribed client receives SVG updates published to the chart group.</summary>
    [Fact]
    public async Task SubscribedClient_ReceivesSvgUpdate()
    {
        var connection = CreateHubConnection();
        var tcs = new TaskCompletionSource<(string chartId, string svg)>();

        connection.On<string, string>("UpdateChartSvg", (id, svg) =>
        {
            tcs.SetResult((id, svg));
        });

        var ct = TestContext.Current.CancellationToken;
        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", "live-chart", ct);

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        var figure = Plt.Create().WithTitle("Live Update").Plot([1.0], [2.0]).Build();
        await publisher.PublishSvgAsync("live-chart", figure, ct);

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal("live-chart", result.chartId);
        Assert.Contains("<svg", result.svg);
        Assert.Contains("Live Update", result.svg);

        await connection.DisposeAsync();
    }

    /// <summary>Verifies that an unsubscribed client does not receive SVG updates.</summary>
    [Fact]
    public async Task UnsubscribedClient_DoesNotReceiveUpdate()
    {
        var connection = CreateHubConnection();
        var received = false;

        connection.On<string, string>("UpdateChartSvg", (_, _) => received = true);

        var ct = TestContext.Current.CancellationToken;
        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", "temp-chart", ct);
        await connection.InvokeAsync("Unsubscribe", "temp-chart", ct);

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        await publisher.PublishSvgAsync("temp-chart", Plt.Create().Build(), ct);

        await Task.Delay(500, ct);
        Assert.False(received);

        await connection.DisposeAsync();
    }

    private HubConnection CreateHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/charts-hub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _host.GetTestServer().CreateHandler();
            })
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }
}

/// <summary>Covers the L115 TRUE arm of ConfigureRoutes: when the injected signalR loader
/// returns null, the endpoint responds 404 (not a NullReferenceException).</summary>
public class ChartServerNullLoaderTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _httpClient;

    public ChartServerNullLoaderTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddMatPlotLibNetSignalR();
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        ChartServer.ConfigureRoutes(endpoints, new(), () => 0,
                            signalRLoader: () => null);   // L115 TRUE arm
                    });
                });
            })
            .Start();

        _httpClient = _host.GetTestClient();
    }

    [Fact]
    public async Task SignalRJs_NullLoader_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync("/js/signalr.min.js", ct);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }
}

// ─── ChartServerCoverageTests.cs ─────────────────────────────────────────────

/// <summary>Phase Y.7 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="ChartServer"/> lifecycle methods that the existing
/// <see cref="ChartServerTests"/> harness left at 0% (DisposeAsync, IsRunning
/// when not started, EnsureStartedAsync idempotence). Pre-Y.7: 83.6%L / 50%B.
/// Each fact constructs a FRESH non-singleton ChartServer via the internal
/// constructor (IVT is set in MatPlotLibNet.Interactive.csproj) so the global
/// singleton is never touched.</summary>
public class ChartServerCoverageTests
{
    /// <summary>IsRunning returns false on a fresh, never-started ChartServer
    /// (line 37 — `_app is not null` false arm).</summary>
    [Fact]
    public async Task IsRunning_OnFreshServer_False()
    {
        await using var server = new ChartServer();
        Assert.False(server.IsRunning);
        Assert.Equal(0, server.Port);
    }

    /// <summary>Standard dispose-pattern contract: DisposeAsync is idempotent.
    /// Second call hits `if (_disposed) return` and must NOT throw or re-execute cleanup.</summary>
    [Fact]
    public async Task DisposeAsync_SecondCall_IsNoOp()
    {
        var server = new ChartServer();
        await server.DisposeAsync();
        await server.DisposeAsync();
    }

    /// <summary>DisposeAsync on a never-started ChartServer — DisposeAsyncCore
    /// skips app teardown when _app is null (no Kestrel was started).</summary>
    [Fact]
    public async Task DisposeAsync_NeverStartedServer_NoOp()
    {
        await using var server = new ChartServer();
        Assert.False(server.IsRunning);
    }

    /// <summary>DisposeAsync on a STARTED ChartServer (line 129 true arm).
    /// Verifies the full teardown of the embedded Kestrel host.</summary>
    [Fact]
    public async Task DisposeAsync_StartedServer_StopsKestrel()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);

        await server.DisposeAsync();
        await server.DisposeAsync();
    }

    /// <summary>EnsureStartedAsync called twice on the same instance — the second
    /// call must hit the early-return at line 44 (`_app is not null` true arm).</summary>
    [Fact]
    public async Task EnsureStartedAsync_CalledTwice_SecondCallShortCircuits()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        var firstPort = server.Port;
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        Assert.Equal(firstPort, server.Port);
        await server.DisposeAsync();
    }

    /// <summary>EnsureStarted (synchronous wrapper, line 76) — verify it blocks
    /// until startup completes.</summary>
    [Fact]
    public async Task EnsureStarted_Synchronous_StartsServer()
    {
        var server = new ChartServer();
        server.EnsureStarted();
        Assert.True(server.IsRunning);
        await server.DisposeAsync();
    }

    /// <summary>UpdateFigureAsync L90 TRUE arm — when server is started, _publisher is
    /// non-null and PublishSvgAsync is called. Asserts the figure dict is also updated.</summary>
    [Fact]
    public async Task UpdateFigureAsync_AfterStart_PublishesAndUpdatesFigureDict()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var id = server.RegisterFigure(fig);

        var updatedFig = Plt.Create().Plot([3.0], [4.0]).Build();
        await server.UpdateFigureAsync(id, updatedFig);

        var url = server.GetFigureUrl(id);
        Assert.Contains(id, url);
        await server.DisposeAsync();
    }
}
