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

/// <summary>End-to-end tests for the v1.2.2 brush-select and hover hub methods using a real
/// <see cref="TestServer"/> and real <see cref="HubConnectionBuilder"/> — no mocks, no fakes.
/// The hover test connects TWO clients to verify the caller-only response pattern: the
/// originating client receives <c>ReceiveTooltipContent</c>, the second client does not.
/// This is the first per-caller mechanism in the library; v1.2.0 only had group broadcast.</summary>
public class SignalRInteractionTestsV122 : IAsyncDisposable
{
    private readonly IHost _host;

    public SignalRInteractionTestsV122()
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
    public async Task OnBrushSelect_FiresHandler_WithDataSpaceRect_NoBroadcast()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
        var received = new TaskCompletionSource<BrushSelectEvent>();
        registry.Register("brush-1", figure, opts =>
            opts.OnBrushSelect(evt => { received.TrySetResult(evt); return default; }));

        var conn = CreateConnection();
        var svgReceived = new TaskCompletionSource<string>();
        conn.On<string, string>("UpdateChartSvg", (_, svg) => svgReceived.TrySetResult(svg));
        await conn.StartAsync(ct);
        await conn.InvokeAsync("Subscribe", "brush-1", cancellationToken: ct);

        var evt = new BrushSelectEvent("brush-1", 0, 0.25, 1.75, 4.25, 5.75);
        await conn.InvokeAsync(nameof(ChartHub.OnBrushSelect), evt, cancellationToken: ct);

        var handlerPayload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal(0.25, handlerPayload.X1);
        Assert.Equal(1.75, handlerPayload.Y1);
        Assert.Equal(4.25, handlerPayload.X2);
        Assert.Equal(5.75, handlerPayload.Y2);

        // Verify NO broadcast fires — give the server time to finish a hypothetical republish,
        // then assert the SVG callback was never invoked.
        await Task.Delay(250, ct);
        Assert.False(svgReceived.Task.IsCompleted, "OnBrushSelect must not trigger an UpdateChartSvg broadcast");

        await conn.DisposeAsync();
        await registry.UnregisterAsync("brush-1");
    }

    [Fact]
    public async Task OnHover_ReturnsTooltip_ToCallerOnly_NotToOtherSubscribers()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        registry.Register("hover-1", figure, opts =>
            opts.OnHover(evt => ValueTask.FromResult<string?>($"<b>x={evt.X},y={evt.Y}</b>")));

        // Two clients connected. Client A invokes OnHover; Client B must NOT receive.
        var connA = CreateConnection();
        var connB = CreateConnection();
        var tooltipA = new TaskCompletionSource<(string chartId, string html)>();
        var tooltipB = new TaskCompletionSource<(string chartId, string html)>();

        connA.On<string, string>("ReceiveTooltipContent", (id, html) => tooltipA.TrySetResult((id, html)));
        connB.On<string, string>("ReceiveTooltipContent", (id, html) => tooltipB.TrySetResult((id, html)));

        await connA.StartAsync(ct);
        await connB.StartAsync(ct);
        await connA.InvokeAsync("Subscribe", "hover-1", cancellationToken: ct);
        await connB.InvokeAsync("Subscribe", "hover-1", cancellationToken: ct);

        var payload = new HoverEventPayload("hover-1", 0, 1.5, 2.5);
        await connA.InvokeAsync(nameof(ChartHub.OnHover), payload, cancellationToken: ct);

        var responseA = await tooltipA.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal("hover-1", responseA.chartId);
        Assert.Equal("<b>x=1.5,y=2.5</b>", responseA.html);

        // Give B a moment to receive (it should NOT)
        await Task.Delay(250, ct);
        Assert.False(tooltipB.Task.IsCompleted,
            "Hover tooltip MUST target the originating caller only, not broadcast to the group");

        await connA.DisposeAsync();
        await connB.DisposeAsync();
        await registry.UnregisterAsync("hover-1");
    }

    [Fact]
    public async Task OnHover_WithoutHandler_SilentlyIgnored()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        registry.Register("hover-none", figure);  // no hover handler

        var conn = CreateConnection();
        var received = new TaskCompletionSource<(string, string)>();
        conn.On<string, string>("ReceiveTooltipContent", (id, html) => received.TrySetResult((id, html)));
        await conn.StartAsync(ct);
        await conn.InvokeAsync("Subscribe", "hover-none", cancellationToken: ct);

        var payload = new HoverEventPayload("hover-none", 0, 1, 2);
        await conn.InvokeAsync(nameof(ChartHub.OnHover), payload, cancellationToken: ct);

        await Task.Delay(250, ct);
        Assert.False(received.Task.IsCompleted,
            "Hover with no handler must not fire ReceiveTooltipContent");

        await conn.DisposeAsync();
        await registry.UnregisterAsync("hover-none");
    }

    [Fact]
    public async Task V120HubMethods_StillWork_AfterV122Changes()
    {
        // Regression guard: OnZoom (a v1.2.0 mutation event) must still broadcast UpdateChartSvg.
        var ct = TestContext.Current.CancellationToken;
        var registry = _host.Services.GetRequiredService<FigureRegistry>();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        registry.Register("zoom-regression", figure);

        var conn = CreateConnection();
        var svgReceived = new TaskCompletionSource<string>();
        conn.On<string, string>("UpdateChartSvg", (_, svg) => svgReceived.TrySetResult(svg));
        await conn.StartAsync(ct);
        await conn.InvokeAsync("Subscribe", "zoom-regression", cancellationToken: ct);

        var evt = new ZoomEvent("zoom-regression", 0, 2, 8, 3, 7);
        await conn.InvokeAsync("OnZoom", evt, cancellationToken: ct);

        _ = await svgReceived.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal(2.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(8.0, figure.SubPlots[0].XAxis.Max);

        await conn.DisposeAsync();
        await registry.UnregisterAsync("zoom-regression");
    }

    private HubConnection CreateConnection() =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/charts-hub", o =>
                o.HttpMessageHandlerFactory = _ => _host.GetTestServer().CreateHandler())
            .Build();

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
