// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Home across the wire. The page's Home key sends the range it read off the latest drawing, and after a
/// republish that drawing is the zoomed one, so a server that applied the numbers returned the chart to where the
/// reader already was. The figure on the server remembers the view the first zoom started from and goes back to it,
/// whichever numbers arrive, and whichever viewer zoomed.</summary>
public class SignalRHomeTests : IAsyncDisposable
{
    private readonly IHost _host;

    public SignalRHomeTests()
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

    private async Task<(HubConnection Connection, Func<Task> NextSvg)> ConnectAsync(string chartId, CancellationToken ct)
    {
        var conn = new HubConnectionBuilder()
            .WithUrl("http://localhost/charts-hub", o =>
                o.HttpMessageHandlerFactory = _ => _host.GetTestServer().CreateHandler())
            .Build();

        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        conn.On<string, string>("UpdateChartSvg", (_, _) => pending.TrySetResult());
        await conn.StartAsync(ct);
        await conn.InvokeAsync("Subscribe", chartId, cancellationToken: ct);

        // Waits for the next republish, then arms the next wait.
        async Task Next()
        {
            await pending.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        return (conn, Next);
    }

    [Fact]
    public async Task Home_AfterAZoom_IgnoresTheStaleRangeThePageSends_AndRestoresTheFirstView()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        registry.Register("home-1", figure);
        var (conn, next) = await ConnectAsync("home-1", ct);

        await conn.InvokeAsync(nameof(ChartHub.OnZoom), new ZoomEvent("home-1", 0, 1.5, 2.5, 15, 35), cancellationToken: ct);
        await next();
        Assert.Equal(1.5, figure.SubPlots[0].XAxis.Min);

        // The page read these numbers off the republished (zoomed) drawing.
        await conn.InvokeAsync(nameof(ChartHub.OnReset), new ResetEvent("home-1", 0, 1.5, 2.5, 15, 35), cancellationToken: ct);
        await next();

        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Null(figure.SubPlots[0].XAxis.Max);
        Assert.Null(figure.SubPlots[0].YAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Max);

        await conn.DisposeAsync();
        await registry.UnregisterAsync("home-1");
    }

    [Fact]
    public async Task Home_OfAViewerWhoNeverZoomed_ReturnsTheChartToTheFirstView_NotToTheOtherViewersZoom()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        registry.Register("home-2", figure);
        var (viewerA, nextA) = await ConnectAsync("home-2", ct);
        var (viewerB, nextB) = await ConnectAsync("home-2", ct);

        await viewerA.InvokeAsync(nameof(ChartHub.OnZoom), new ZoomEvent("home-2", 0, 1.5, 2.5, 15, 35), cancellationToken: ct);
        await nextA();
        await nextB();

        // B sees A's zoom as its latest drawing, so the numbers B's page sends with Home are A's zoomed ones.
        await viewerB.InvokeAsync(nameof(ChartHub.OnReset), new ResetEvent("home-2", 0, 1.5, 2.5, 15, 35), cancellationToken: ct);
        await nextB();

        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Max);

        await viewerA.DisposeAsync();
        await viewerB.DisposeAsync();
        await registry.UnregisterAsync("home-2");
    }

    [Fact]
    public async Task Home_OnAFigureNothingMoved_AppliesTheRangeItCarries()
    {
        // The numbers of a reset are what the sender started from. A figure that remembers no first view for the axes
        // (nothing has moved it) has nothing better to go back to, so the event keeps meaning what it always meant.
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        registry.Register("home-3", figure);
        var (conn, next) = await ConnectAsync("home-3", ct);

        await conn.InvokeAsync(nameof(ChartHub.OnReset), new ResetEvent("home-3", 0, 0, 5, 0, 50), cancellationToken: ct);
        await next();

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(5.0, figure.SubPlots[0].XAxis.Max);

        await conn.DisposeAsync();
        await registry.UnregisterAsync("home-3");
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
