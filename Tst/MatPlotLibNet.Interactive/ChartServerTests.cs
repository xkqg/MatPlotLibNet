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
