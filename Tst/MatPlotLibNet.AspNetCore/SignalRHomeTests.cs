// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using Microsoft.AspNetCore.SignalR.Client;
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
        _host = SignalRTestHost.Start();
    }

    [Fact]
    public async Task Home_AfterAZoom_IgnoresTheStaleRangeThePageSends_AndRestoresTheFirstView()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        await using var chart = RegisteredChart.Of(_host, "home-1", figure);
        var viewer = await chart.JoinAsync(ct);

        await viewer.GestureAsync(nameof(ChartHub.OnZoom), new ZoomEvent("home-1", 0, 1.5, 2.5, 15, 35), ct);
        Assert.Equal(1.5, figure.SubPlots[0].XAxis.Min);

        // The page read these numbers off the republished (zoomed) drawing.
        await viewer.GestureAsync(nameof(ChartHub.OnReset), new ResetEvent("home-1", 0, 1.5, 2.5, 15, 35), ct);

        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Null(figure.SubPlots[0].XAxis.Max);
        Assert.Null(figure.SubPlots[0].YAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public async Task Home_OfAViewerWhoNeverZoomed_ReturnsTheChartToTheFirstView_NotToTheOtherViewersZoom()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        await using var chart = RegisteredChart.Of(_host, "home-2", figure);
        var viewerA = await chart.JoinAsync(ct);
        var viewerB = await chart.JoinAsync(ct);

        await viewerA.GestureAsync(nameof(ChartHub.OnZoom), new ZoomEvent("home-2", 0, 1.5, 2.5, 15, 35), ct);
        await viewerB.Svgs.NextAsync(ct);

        // B sees A's zoom as its latest drawing, so the numbers B's page sends with Home are A's zoomed ones.
        await viewerB.GestureAsync(nameof(ChartHub.OnReset), new ResetEvent("home-2", 0, 1.5, 2.5, 15, 35), ct);

        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public async Task Home_OnAFigureNothingMoved_AppliesTheRangeItCarries()
    {
        // The numbers of a reset are what the sender started from. A figure that remembers no first view for the axes
        // (nothing has moved it) has nothing better to go back to, so the event keeps meaning what it always meant.
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        await using var chart = RegisteredChart.Of(_host, "home-3", figure);
        var viewer = await chart.JoinAsync(ct);

        await viewer.GestureAsync(nameof(ChartHub.OnReset), new ResetEvent("home-3", 0, 0, 5, 0, 50), ct);

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(5.0, figure.SubPlots[0].XAxis.Max);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
