// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>End-to-end round-trip tests for the four client-to-server <see cref="ChartHub"/>
/// methods introduced in v1.2.0. Uses a real <see cref="TestServer"/> + <see cref="HubConnectionBuilder"/>
/// — no mocks. Each test: register a figure in <see cref="FigureRegistry"/>, connect + subscribe,
/// invoke the hub method, wait for <see cref="IChartHubClient.UpdateChartSvg"/> callback, assert
/// the received SVG reflects the mutated figure state.</summary>
public class SignalRInteractionTests : IAsyncDisposable
{
    private readonly IHost _host;

    public SignalRInteractionTests()
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
                    app.UseEndpoints(endpoints => endpoints.MapChartHub());
                });
            })
            .Start();
    }

    [Fact]
    public async Task OnZoom_MutatesFigure_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create()
            .WithTitle("ZoomTest")
            .Plot([0.0, 1.0, 2.0], [0.0, 1.0, 4.0])
            .Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 4;
        registry.Register("zoom-1", figure);

        var (conn, tcs) = await ConnectAndWatchAsync("zoom-1", ct);

        var evt = new ZoomEvent("zoom-1", 0, 0.25, 1.75, 0.5, 3.5);
        await conn.InvokeAsync(nameof(ChartHub.OnZoom), evt, cancellationToken: ct);

        var (_, svg) = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Contains("ZoomTest", svg);
        Assert.Equal(0.25, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(1.75, figure.SubPlots[0].XAxis.Max);
        Assert.Equal(0.5, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(3.5, figure.SubPlots[0].YAxis.Max);

        await conn.DisposeAsync();
        await registry.UnregisterAsync("zoom-1");
    }

    [Fact]
    public async Task OnPan_TranslatesFigure_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        registry.Register("pan-1", figure);

        var (conn, tcs) = await ConnectAndWatchAsync("pan-1", ct);

        var evt = new PanEvent("pan-1", 0, 5, -3);
        await conn.InvokeAsync(nameof(ChartHub.OnPan), evt, cancellationToken: ct);

        _ = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal(5.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(15.0, figure.SubPlots[0].XAxis.Max);
        Assert.Equal(-3.0, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(7.0, figure.SubPlots[0].YAxis.Max);

        await conn.DisposeAsync();
        await registry.UnregisterAsync("pan-1");
    }

    [Fact]
    public async Task OnReset_RestoresFigureLimits_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 100; figure.SubPlots[0].XAxis.Max = 200;
        registry.Register("reset-1", figure);

        var (conn, tcs) = await ConnectAndWatchAsync("reset-1", ct);

        var evt = new ResetEvent("reset-1", 0, 0, 10, 0, 10);
        await conn.InvokeAsync(nameof(ChartHub.OnReset), evt, cancellationToken: ct);

        _ = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Max);

        await conn.DisposeAsync();
        await registry.UnregisterAsync("reset-1");
    }

    [Fact]
    public async Task OnLegendToggle_FlipsSeriesVisibility_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        registry.Register("leg-1", figure);

        var (conn, tcs) = await ConnectAndWatchAsync("leg-1", ct);

        var evt = new LegendToggleEvent("leg-1", 0, 0);
        await conn.InvokeAsync(nameof(ChartHub.OnLegendToggle), evt, cancellationToken: ct);

        _ = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        var series = (Models.Series.ChartSeries)figure.SubPlots[0].Series[0];
        Assert.False(series.Visible);

        await conn.DisposeAsync();
        await registry.UnregisterAsync("leg-1");
    }

    private async Task<(HubConnection conn, TaskCompletionSource<(string, string)> tcs)>
        ConnectAndWatchAsync(string chartId, CancellationToken ct)
    {
        var conn = new HubConnectionBuilder()
            .WithUrl("http://localhost/charts-hub", o =>
                o.HttpMessageHandlerFactory = _ => _host.GetTestServer().CreateHandler())
            .Build();

        var tcs = new TaskCompletionSource<(string, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
        conn.On<string, string>("UpdateChartSvg", (id, svg) => tcs.TrySetResult((id, svg)));

        await conn.StartAsync(ct);
        await conn.InvokeAsync("Subscribe", chartId, cancellationToken: ct);
        return (conn, tcs);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
