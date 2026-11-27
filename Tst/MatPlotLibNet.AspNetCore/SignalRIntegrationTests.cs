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
        _host = SignalRTestHost.Start();

        _httpClient = _host.GetTestClient();
    }

    /// <summary>Verifies that the ChartHub is reachable and a client can connect successfully.</summary>
    [Fact]
    public async Task ChartHub_IsReachable()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = _host.ConnectToTheHub();
        await connection.StartAsync(ct);
        Assert.Equal(HubConnectionState.Connected, connection.State);
        await connection.DisposeAsync();
    }

    /// <summary>The hub keeps the ledger a render lane reads: a subscribed connection is counted, and a
    /// connection that simply goes away is swept out of every chart it joined.</summary>
    [Fact]
    public async Task Subscribe_IsCountedInTheLedger_AndADisconnectSweepsIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var ledger = _host.Services.GetRequiredService<IChartSubscriptions>();
        var connection = _host.ConnectToTheHub();
        await connection.StartAsync(ct);

        await connection.InvokeAsync("Subscribe", "ledger-chart", cancellationToken: ct);
        Assert.True(ledger.HasSubscribers("ledger-chart"));
        Assert.Equal(1, ledger.Count("ledger-chart"));

        await connection.InvokeAsync("Unsubscribe", "ledger-chart", cancellationToken: ct);
        Assert.False(ledger.HasSubscribers("ledger-chart"));

        await connection.InvokeAsync("Subscribe", "ledger-chart", cancellationToken: ct);
        await connection.DisposeAsync();
        await Eventually.HoldsAsync(() => !ledger.HasSubscribers("ledger-chart"));   // the disconnect swept the connection out of the chart
    }

    /// <summary>Verifies that a subscribed client receives the broadcast of the publisher, as SVG and as JSON, each under its own hub method.</summary>
    [Theory]
    [InlineData(true, "UpdateChartSvg", "live-chart", "Live Data", "<svg")]
    [InlineData(false, "UpdateChart", "json-chart", "JSON Test", "")]
    public async Task APublishedChart_ReachesItsSubscriber(bool asSvg, string method, string chart, string title, string marker)
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = _host.ConnectToTheHub();
        var tcs = new TaskCompletionSource<(string chartId, string body)>();

        connection.On<string, string>(method, (id, body) =>
        {
            tcs.SetResult((id, body));
        });

        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", chart, cancellationToken: ct);

        // Publish from server side
        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        var figure = Plt.Create().WithTitle(title).Plot([1.0, 2.0], [3.0, 4.0]).Build();

        await (asSvg ? publisher.PublishSvgAsync(chart, figure, ct) : publisher.PublishAsync(chart, figure, ct));

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal(chart, result.chartId);
        Assert.Contains(marker, result.body);
        Assert.Contains(title, result.body);

        await connection.DisposeAsync();
    }

    /// <summary>Verifies that an unsubscribed client does not receive broadcast messages.</summary>
    [Fact]
    public async Task UnsubscribedClient_DoesNotReceive()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = _host.ConnectToTheHub();
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

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
