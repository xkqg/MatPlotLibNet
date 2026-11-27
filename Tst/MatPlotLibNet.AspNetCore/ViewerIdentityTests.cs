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

/// <summary>Several people can look at one chart. The hub knows which connection each message came in on, and nothing a
/// client writes in its payload can say otherwise: the event reaches the handler stamped with that connection and with a
/// name the chart gave it ("Viewer 1", "Viewer 2", in the order they joined).</summary>
public class ViewerIdentityTests : IAsyncDisposable
{
    private readonly IHost _host;

    public ViewerIdentityTests()
    {
        _host = SignalRTestHost.Start();
    }

    private FigureRegistry Registry => _host.Services.GetRequiredService<FigureRegistry>();

    private RecordingChartEventHandler Record(string chartId)
    {
        var recorder = new RecordingChartEventHandler();
        Registry.Register(chartId, Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build(), o => o.Handle(recorder));
        return recorder;
    }

    private async Task<HubConnection> JoinAsync(string chartId, CancellationToken ct)
    {
        var conn = _host.ConnectToTheHub();
        await conn.StartAsync(ct);
        await conn.InvokeAsync("Subscribe", chartId, cancellationToken: ct);
        return conn;
    }

    [Fact]
    public async Task AnEvent_ReachesTheHandler_StampedWithTheConnectionItCameOn_NotTheOneItClaimed()
    {
        var ct = TestContext.Current.CancellationToken;
        var recorder = Record("v-1");
        var viewer = await JoinAsync("v-1", ct);

        await viewer.InvokeAsync(nameof(ChartHub.OnZoom),
            new ZoomEvent("v-1", 0, 1, 2, 3, 4) { ViewerId = "someone-else", ViewerName = "Viewer 99" }, ct);
        await recorder.WaitForAsync(1);

        var (evt, context) = Assert.Single(recorder.Handled);
        Assert.Equal(viewer.ConnectionId, evt.ViewerId);
        Assert.Equal(new ChartEventContext(viewer.ConnectionId, "Viewer 1"), context);

        await viewer.DisposeAsync();
        await Registry.UnregisterAsync("v-1");
    }

    [Fact]
    public async Task Viewers_AreNamedInTheOrderTheyJoined()
    {
        var ct = TestContext.Current.CancellationToken;
        var recorder = Record("v-2");
        var first = await JoinAsync("v-2", ct);
        var second = await JoinAsync("v-2", ct);

        await second.InvokeAsync(nameof(ChartHub.OnPan), new PanEvent("v-2", 0, 0.1, 0.1), ct);
        await first.InvokeAsync(nameof(ChartHub.OnPan), new PanEvent("v-2", 0, 0.1, 0.1), ct);
        await recorder.WaitForAsync(2);

        var names = recorder.Handled.ToDictionary(s => s.Context.ViewerId!, s => s.Context.ViewerName);
        Assert.Equal("Viewer 2", names[second.ConnectionId!]);
        Assert.Equal("Viewer 1", names[first.ConnectionId!]);

        await first.DisposeAsync();
        await second.DisposeAsync();
        await Registry.UnregisterAsync("v-2");
    }

    [Fact]
    public async Task AViewerWhoLeftAndCameBack_GetsANewName_NotAnotherViewersOldOne()
    {
        var ct = TestContext.Current.CancellationToken;
        var recorder = Record("v-3");
        var first = await JoinAsync("v-3", ct);
        await first.DisposeAsync();

        var again = await JoinAsync("v-3", ct);
        await again.InvokeAsync(nameof(ChartHub.OnPan), new PanEvent("v-3", 0, 0.1, 0.1), ct);
        await recorder.WaitForAsync(1);

        Assert.Equal("Viewer 2", Assert.Single(recorder.Handled).Context.ViewerName);

        await again.DisposeAsync();
        await Registry.UnregisterAsync("v-3");
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
