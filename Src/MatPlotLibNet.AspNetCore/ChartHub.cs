// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using Microsoft.AspNetCore.SignalR;

namespace MatPlotLibNet.AspNetCore;

/// <summary>SignalR hub that manages client subscriptions to real-time chart update groups
/// and accepts client-to-server interaction events (zoom, pan, reset, legend-toggle).</summary>
public sealed class ChartHub : Hub<IChartHubClient>
{
    private readonly FigureRegistry _registry;
    private readonly ChartSubscriptions _subscriptions;

    /// <summary>Creates the hub for one connection; SignalR builds it per call and the container supplies both arguments.</summary>
    /// <param name="registry">The registry that holds each chart's session and receives the interaction events clients send.</param>
    /// <param name="subscriptions">The record of which connection is subscribed to which chart, and under what viewer name.</param>
    public ChartHub(FigureRegistry registry, ChartSubscriptions subscriptions)
    {
        _registry = registry;
        _subscriptions = subscriptions;
    }

    /// <summary>Hands an event the client sent to the chart's session, stamped with the connection it arrived on and the name
    /// the chart gave that connection, over whatever the payload said: a client cannot speak for another viewer.</summary>
    private void Route(FigureInteractionEvent evt) =>
        _registry.Publish(evt.ChartId, evt.StampedBy(Context.ConnectionId, _subscriptions.ViewerNameOf(evt.ChartId, Context.ConnectionId)));

    /// <summary>Subscribes the calling client to updates for the specified chart.</summary>
    public Task Subscribe(string chartId)
    {
        _subscriptions.Subscribe(chartId, Context.ConnectionId);
        return Groups.AddToGroupAsync(Context.ConnectionId, chartId);
    }

    /// <summary>Unsubscribes the calling client from updates for the specified chart.</summary>
    public async Task Unsubscribe(string chartId)
    {
        _subscriptions.Unsubscribe(chartId, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chartId);
        await Clients.Group(chartId).ViewerLeft(chartId, Context.ConnectionId);
    }

    /// <summary>A closed browser never calls Unsubscribe: the connection leaves every chart it joined here, so
    /// a render lane stops spending frames on a tab that is gone.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var charts = _subscriptions.ChartsOf(Context.ConnectionId);
        _subscriptions.Disconnected(Context.ConnectionId);

        // The others in each chart the connection was in are told it is gone, so its pointer does not stay where it was.
        foreach (var chartId in charts)
        {
            await Clients.OthersInGroup(chartId).ViewerLeft(chartId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Client-to-server: apply a zoom to the registered figure. Fire-and-forget —
    /// mutation + re-render + fan-out happen on the session's reader task.</summary>
    public void OnZoom(ZoomEvent evt) => Route(evt);

    /// <summary>Client-to-server: apply a pan translation in data coordinates.</summary>
    public void OnPan(PanEvent evt) => Route(evt);

    /// <summary>Client-to-server: reset axis limits to the figure's original extent.</summary>
    public void OnReset(ResetEvent evt) => Route(evt);

    /// <summary>Client-to-server: toggle a series' <see cref="Models.Series.ChartSeries.Visible"/>
    /// flag from a legend entry click.</summary>
    public void OnLegendToggle(LegendToggleEvent evt) => Route(evt);

    /// <summary>Client-to-server: observer event from a Shift+drag rubber-band
    /// selection. Routes to the registered brush-select handler on the session, if any.
    /// Non-mutating — the figure is never re-rendered from a brush-select.</summary>
    public void OnBrushSelect(BrushSelectEvent evt) => Route(evt);

    /// <summary>Client-to-server: the reader clicked a data point. Routes to the session's data-cursor
    /// handler, if any. Non-mutating, like a brush-select: a click asks about the data, it does not change it,
    /// so the figure is never re-rendered from one.</summary>
    public void OnDataCursor(DataCursorEvent evt) => Route(evt);

    /// <summary>Client-to-server: a rectangle drawn with Ctrl+drag, to zoom into. A native control sends it; the page's
    /// own script zooms with <see cref="OnZoom"/>.</summary>
    public void OnRectangleZoom(RectangleZoomEvent evt) => Route(evt);

    /// <summary>Client-to-server: a span of X chosen with Alt+drag. Non-mutating, like a brush-select.</summary>
    public void OnSpanSelect(SpanSelectEvent evt) => Route(evt);

    /// <summary>Client-to-server: rotate the camera of a 3-D axes by the given angles.</summary>
    public void OnRotate3D(Rotate3DEvent evt) => Route(evt);

    /// <summary>Client-to-server: add a trendline to the axes.</summary>
    public void OnAddTrendline(AddTrendlineEvent evt) => Route(evt);

    /// <summary>Client-to-server: add a horizontal level to the axes.</summary>
    public void OnAddHorizontalLevel(AddHorizontalLevelEvent evt) => Route(evt);

    /// <summary>Client-to-server: add a Fibonacci retracement to the axes.</summary>
    public void OnAddFibonacciRetracement(AddFibonacciRetracementEvent evt) => Route(evt);

    /// <summary>Client-to-server: where this viewer's pointer is, relayed at once to the other viewers of the chart. It does
    /// not go through the session that applies the gestures: a pointer moves far more often than anything else, and a
    /// slow handler must not hold the cursors up, nor the cursors the handler. Nothing is coalesced here; the page sends a
    /// position when the last one is old enough. A message from a connection that is not in the chart is ignored.</summary>
    public Task OnCursor(CursorEventPayload payload) =>
        _subscriptions.ViewerNameOf(payload.ChartId, Context.ConnectionId) is { } name
            ? Clients.OthersInGroup(payload.ChartId).ReceiveCursor(
                payload.ChartId, Context.ConnectionId, name, payload.AxesIndex, payload.X, payload.Y)
            : Task.CompletedTask;

    /// <summary>Client-to-server: hover event. The client supplies the data-space
    /// point via <see cref="HoverEventPayload"/>; the hub stamps
    /// <see cref="HoverEvent.CallerConnectionId"/> from <see cref="Microsoft.AspNetCore.SignalR.HubCallerContext.ConnectionId"/>
    /// server-side so a client cannot spoof another user's connection. The session's hover
    /// handler computes tooltip HTML which is delivered to the originating caller only via
    /// <see cref="IChartHubClient.ReceiveTooltipContent"/>.</summary>
    public void OnHover(HoverEventPayload payload)
    {
        Route(new HoverEvent(
            payload.ChartId,
            payload.AxesIndex,
            payload.X,
            payload.Y,
            CallerConnectionId: Context.ConnectionId));
    }
}
