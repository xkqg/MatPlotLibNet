// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Verifies <see cref="ChartHub"/> behavior.</summary>
public class SignalRIntegrationTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _httpClient;

    public SignalRIntegrationTests()
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
                        endpoints.MapChartHub();
                    });
                });
            })
            .Start();

        _httpClient = _host.GetTestClient();
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

    /// <summary>Verifies that subscribing to a chart group completes without throwing.</summary>
    [Fact]
    public async Task Subscribe_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = CreateHubConnection();
        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", "test-chart", cancellationToken: ct);
        await connection.DisposeAsync();
    }

    /// <summary>Verifies that a subscribed client receives an SVG broadcast from the publisher.</summary>
    [Fact]
    public async Task PublishSvg_ReceivesBroadcast()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = CreateHubConnection();
        var tcs = new TaskCompletionSource<(string chartId, string svg)>();

        connection.On<string, string>("UpdateChartSvg", (id, svg) =>
        {
            tcs.SetResult((id, svg));
        });

        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", "live-chart", cancellationToken: ct);

        // Publish from server side
        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        var figure = Plt.Create()
            .WithTitle("Live Data")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        await publisher.PublishSvgAsync("live-chart", figure, ct);

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal("live-chart", result.chartId);
        Assert.Contains("<svg", result.svg);
        Assert.Contains("Live Data", result.svg);

        await connection.DisposeAsync();
    }

    /// <summary>Verifies that a subscribed client receives a JSON broadcast from the publisher.</summary>
    [Fact]
    public async Task PublishJson_ReceivesBroadcast()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = CreateHubConnection();
        var tcs = new TaskCompletionSource<(string chartId, string json)>();

        connection.On<string, string>("UpdateChart", (id, json) =>
        {
            tcs.SetResult((id, json));
        });

        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", "json-chart", cancellationToken: ct);

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        var figure = Plt.Create().WithTitle("JSON Test").Build();
        await publisher.PublishAsync("json-chart", figure, ct);

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal("json-chart", result.chartId);
        Assert.Contains("JSON Test", result.json);

        await connection.DisposeAsync();
    }

    /// <summary>Verifies that an unsubscribed client does not receive broadcast messages.</summary>
    [Fact]
    public async Task UnsubscribedClient_DoesNotReceive()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = CreateHubConnection();
        var received = false;

        connection.On<string, string>("UpdateChartSvg", (_, _) => received = true);

        await connection.StartAsync(ct);
        // Subscribe then unsubscribe
        await connection.InvokeAsync("Subscribe", "temp-chart", cancellationToken: ct);
        await connection.InvokeAsync("Unsubscribe", "temp-chart", cancellationToken: ct);

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
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
