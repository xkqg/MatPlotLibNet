// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>End-to-end round-trip tests for the four client-to-server <see cref="ChartHub"/>
/// methods introduced in v1.2.0. Uses a real <see cref="Microsoft.AspNetCore.TestHost.TestServer"/> and a real hub connection
/// — no mocks. Each test: register a figure in <see cref="FigureRegistry"/>, connect + subscribe,
/// invoke the hub method, wait for <see cref="IChartHubClient.UpdateChartSvg"/> callback, assert
/// the received SVG reflects the mutated figure state.</summary>
public class SignalRInteractionTests : IAsyncDisposable
{
    private readonly IHost _host;

    public SignalRInteractionTests()
    {
        _host = SignalRTestHost.Start();
    }

    [Fact]
    public async Task OnZoom_MutatesFigure_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create()
            .WithTitle("ZoomTest")
            .Plot([0.0, 1.0, 2.0], [0.0, 1.0, 4.0])
            .Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 4;
        await using var chart = RegisteredChart.Of(_host, "zoom-1", figure);
        var viewer = await chart.JoinAsync(ct);

        var svg = await viewer.GestureAsync(nameof(ChartHub.OnZoom), new ZoomEvent("zoom-1", 0, 0.25, 1.75, 0.5, 3.5), ct);
        Assert.Contains("ZoomTest", svg);
        Assert.Equal(0.25, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(1.75, figure.SubPlots[0].XAxis.Max);
        Assert.Equal(0.5, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(3.5, figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public async Task OnPan_TranslatesFigure_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        await using var chart = RegisteredChart.Of(_host, "pan-1", figure);
        var viewer = await chart.JoinAsync(ct);

        await viewer.GestureAsync(nameof(ChartHub.OnPan), new PanEvent("pan-1", 0, 5, -3), ct);
        Assert.Equal(5.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(15.0, figure.SubPlots[0].XAxis.Max);
        Assert.Equal(-3.0, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(7.0, figure.SubPlots[0].YAxis.Max);
    }

    /// <summary>The page's pan payload, as its script writes it: an autoscaled axes has no limits to shift, so the page
    /// sends the view it moved (<c>inView</c>) with the step, and the hub's deserialiser hands it to
    /// <see cref="PanEvent.InView"/>, from which the pan pins and moves the range.</summary>
    [Fact]
    public async Task OnPan_WithThePagesInView_PansAnAutoscaledAxes()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        await using var chart = RegisteredChart.Of(_host, "pan-auto", figure);
        var viewer = await chart.JoinAsync(ct);

        await viewer.GestureAsync(nameof(ChartHub.OnPan), new
        {
            chartId = "pan-auto", axesIndex = 0, dxData = -0.25, dyData = 2.5,
            inView = new { xMin = 0.9, xMax = 3.1, yMin = 8.5, yMax = 41.5 },
        }, ct);
        Assert.Equal(0.65, figure.SubPlots[0].XAxis.Min!.Value, 12);
        Assert.Equal(2.85, figure.SubPlots[0].XAxis.Max!.Value, 12);
        Assert.Equal(11.0, figure.SubPlots[0].YAxis.Min!.Value, 12);
        Assert.Equal(44.0, figure.SubPlots[0].YAxis.Max!.Value, 12);
    }

    [Fact]
    public async Task OnReset_RestoresFigureLimits_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 100; figure.SubPlots[0].XAxis.Max = 200;
        await using var chart = RegisteredChart.Of(_host, "reset-1", figure);
        var viewer = await chart.JoinAsync(ct);

        await viewer.GestureAsync(nameof(ChartHub.OnReset), new ResetEvent("reset-1", 0, 0, 10, 0, 10), ct);
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public async Task OnLegendToggle_FlipsSeriesVisibility_AndPublishesUpdatedSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        await using var chart = RegisteredChart.Of(_host, "leg-1", figure);
        var viewer = await chart.JoinAsync(ct);

        await viewer.GestureAsync(nameof(ChartHub.OnLegendToggle), new LegendToggleEvent("leg-1", 0, 0), ct);
        var series = (Models.Series.ChartSeries)figure.SubPlots[0].Series[0];
        Assert.False(series.Visible);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }

    [Fact]
    public async Task OnBrushSelect_FiresHandler_WithDataSpaceRect_NoBroadcast()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
        var received = new TaskCompletionSource<BrushSelectEvent>();
        await using var chart = RegisteredChart.Of(_host, "brush-1", figure, opts =>
            opts.OnBrushSelect(evt => { received.TrySetResult(evt); return default; }));
        var viewer = await chart.JoinAsync(ct);

        await viewer.Connection.InvokeAsync(nameof(ChartHub.OnBrushSelect), new BrushSelectEvent("brush-1", 0, 0.25, 1.75, 4.25, 5.75), cancellationToken: ct);

        var handlerPayload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal(0.25, handlerPayload.X1);
        Assert.Equal(1.75, handlerPayload.Y1);
        Assert.Equal(4.25, handlerPayload.X2);
        Assert.Equal(5.75, handlerPayload.Y2);

        await viewer.Svgs.NothingArrivesAsync("OnBrushSelect must not trigger an UpdateChartSvg broadcast", ct);
    }

    /// <summary>The clicked data point, over the real transport. It is the brush-select shape rather than the
    /// hover shape: the handler is told, and nothing is sent back or broadcast, because a click asks about the
    /// data rather than changing it. Before this the sink dropped it into its discard arm and the hub had no
    /// method to receive it.</summary>
    [Fact]
    public async Task OnDataCursor_FiresTheHandler_WithThePointClicked_AndBroadcastsNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
        var received = new TaskCompletionSource<DataCursorEvent>();
        await using var chart = RegisteredChart.Of(_host, "click-1", figure, opts =>
            opts.OnDataCursor(evt => { received.TrySetResult(evt); return default; }));
        var viewer = await chart.JoinAsync(ct);

        var clicked = new DataCursorEvent("click-1", 0, new PinnedAnnotation("load", 2.0, 5.0, 120, 60, 0));
        await viewer.Connection.InvokeAsync(nameof(ChartHub.OnDataCursor), clicked, cancellationToken: ct);

        var handled = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal("load", handled.Annotation.SeriesLabel);
        Assert.Equal(2.0, handled.Annotation.DataX);
        Assert.Equal(5.0, handled.Annotation.DataY);
        Assert.Equal(0, handled.AxesIndex);

        await viewer.Svgs.NothingArrivesAsync("a click must not trigger an UpdateChartSvg broadcast", ct);
    }

    [Fact]
    public async Task OnHover_ReturnsTooltip_ToCallerOnly_NotToOtherSubscribers()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        await using var chart = RegisteredChart.Of(_host, "hover-1", figure, opts =>
            opts.OnHover(evt => ValueTask.FromResult<string?>(System.FormattableString.Invariant($"<b>x={evt.X},y={evt.Y}</b>"))));

        // Two clients connected. Client A invokes OnHover; Client B must NOT receive.
        var viewerA = await chart.JoinAsync(ct);
        var viewerB = await chart.JoinAsync(ct);

        await viewerA.Connection.InvokeAsync(nameof(ChartHub.OnHover), new HoverEventPayload("hover-1", 0, 1.5, 2.5), cancellationToken: ct);

        var (chartId, html) = await viewerA.Tooltips.NextAsync(ct);
        Assert.Equal("hover-1", chartId);
        Assert.Equal("<b>x=1.5,y=2.5</b>", html);

        await viewerB.Tooltips.NothingArrivesAsync("Hover tooltip MUST target the originating caller only, not broadcast to the group", ct);
    }

    [Fact]
    public async Task OnHover_WithoutHandler_SilentlyIgnored()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        await using var chart = RegisteredChart.Of(_host, "hover-none", figure);  // no hover handler
        var viewer = await chart.JoinAsync(ct);

        await viewer.Connection.InvokeAsync(nameof(ChartHub.OnHover), new HoverEventPayload("hover-none", 0, 1, 2), cancellationToken: ct);

        await viewer.Tooltips.NothingArrivesAsync("Hover with no handler must not fire ReceiveTooltipContent", ct);
    }

    /// <summary>ROADMAP.md row 100: the embedded browser JS builds a plain object for <c>secondaryRanges</c>, not a
    /// strongly-typed <see cref="ZoomEvent"/> — this pins which wire shape <see cref="AxisSide"/> actually binds
    /// from, since the default JSON hub protocol carries no <c>JsonStringEnumConverter</c> (confirmed by reading
    /// <c>SignalRExtensions.AddMatPlotLibNetSignalR</c>: a bare <c>AddSignalR()</c>, no protocol options).</summary>
    [Fact]
    public async Task OnZoom_WithSecondaryRanges_BindsAxisSideFromItsOrdinal_NotItsName()
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithAddRightYAxis(y2 => y2.SetYLim(0, 500).Plot([1.0, 2.0], [100.0, 200.0]))).Build();
        await using var chart = RegisteredChart.Of(_host, "zoom-secondary-ordinal", figure);
        var viewer = await chart.JoinAsync(ct);

        // The exact shape the JS sends: a plain object, side as AxisSide's ordinal (Right = 1).
        var payload = new
        {
            chartId = "zoom-secondary-ordinal", axesIndex = 0,
            xMin = 1.0, xMax = 2.0, yMin = 3.0, yMax = 4.0,
            secondaryRanges = new[]
            {
                new { side = 1, slotIndex = 0, range = new { xMin = 0.0, xMax = 0.0, yMin = 10.0, yMax = 20.0 } },
            },
        };
        await viewer.GestureAsync("OnZoom", payload, ct);
        Assert.Equal(10.0, figure.SubPlots[0].YAxes.Right[0].Axis.Min);
        Assert.Equal(20.0, figure.SubPlots[0].YAxes.Right[0].Axis.Max);
    }
}
