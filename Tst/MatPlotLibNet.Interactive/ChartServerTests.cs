// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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

    [Fact]
    public void RegisterFigure_ReturnsNonEmptyId()
    {
        var server = new ChartServer();
        var figure = Plt.Create().WithTitle("Test").Build();
        var id = server.RegisterFigure(figure);
        Assert.False(string.IsNullOrEmpty(id));
    }

    [Fact]
    public void RegisterFigure_MultipleFigures_DistinctIds()
    {
        var server = new ChartServer();
        var id1 = server.RegisterFigure(Plt.Create().Build());
        var id2 = server.RegisterFigure(Plt.Create().Build());
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task GetChartPage_ReturnsOk()
    {
        var figure = Plt.Create().WithTitle("Test").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var response = await _httpClient.GetAsync($"/chart/{chartId}");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChartPage_ReturnsHtmlContentType()
    {
        var figure = Plt.Create().Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var response = await _httpClient.GetAsync($"/chart/{chartId}");
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetChartPage_ContainsSvg()
    {
        var figure = Plt.Create().WithTitle("Svg Test").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var html = await _httpClient.GetStringAsync($"/chart/{chartId}");
        Assert.Contains("<svg", html);
        Assert.Contains("Svg Test", html);
    }

    [Fact]
    public async Task GetChartPage_UnknownId_Returns404()
    {
        var response = await _httpClient.GetAsync("/chart/unknown-id");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSignalRJs_ReturnsOk()
    {
        var response = await _httpClient.GetAsync("/js/signalr.min.js");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSignalRJs_ReturnsJavaScript()
    {
        var response = await _httpClient.GetAsync("/js/signalr.min.js");
        Assert.Equal("application/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ChartHub_IsReachable()
    {
        var connection = CreateHubConnection();
        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribedClient_ReceivesSvgUpdate()
    {
        var connection = CreateHubConnection();
        var tcs = new TaskCompletionSource<(string chartId, string svg)>();

        connection.On<string, string>("UpdateChartSvg", (id, svg) =>
        {
            tcs.SetResult((id, svg));
        });

        await connection.StartAsync();
        await connection.InvokeAsync("Subscribe", "live-chart");

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        var figure = Plt.Create().WithTitle("Live Update").Plot([1.0], [2.0]).Build();
        await publisher.PublishSvgAsync("live-chart", figure);

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("live-chart", result.chartId);
        Assert.Contains("<svg", result.svg);
        Assert.Contains("Live Update", result.svg);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task UnsubscribedClient_DoesNotReceiveUpdate()
    {
        var connection = CreateHubConnection();
        var received = false;

        connection.On<string, string>("UpdateChartSvg", (_, _) => received = true);

        await connection.StartAsync();
        await connection.InvokeAsync("Subscribe", "temp-chart");
        await connection.InvokeAsync("Unsubscribe", "temp-chart");

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        await publisher.PublishSvgAsync("temp-chart", Plt.Create().Build());

        await Task.Delay(500);
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
