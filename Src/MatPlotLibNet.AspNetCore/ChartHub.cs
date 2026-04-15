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

    public ChartHub(FigureRegistry registry) => _registry = registry;

    /// <summary>Subscribes the calling client to updates for the specified chart.</summary>
    public Task Subscribe(string chartId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, chartId);

    /// <summary>Unsubscribes the calling client from updates for the specified chart.</summary>
    public Task Unsubscribe(string chartId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, chartId);

    /// <summary>Client-to-server: apply a zoom to the registered figure. Fire-and-forget —
    /// mutation + re-render + fan-out happen on the session's reader task.</summary>
    public void OnZoom(ZoomEvent evt) => _registry.Publish(evt.ChartId, evt);

    /// <summary>Client-to-server: apply a pan translation in data coordinates.</summary>
    public void OnPan(PanEvent evt) => _registry.Publish(evt.ChartId, evt);

    /// <summary>Client-to-server: reset axis limits to the figure's original extent.</summary>
    public void OnReset(ResetEvent evt) => _registry.Publish(evt.ChartId, evt);

    /// <summary>Client-to-server: toggle a series' <see cref="Models.Series.ChartSeries.Visible"/>
    /// flag from a legend entry click.</summary>
    public void OnLegendToggle(LegendToggleEvent evt) => _registry.Publish(evt.ChartId, evt);

    /// <summary>Client-to-server (v1.2.2): observer event from a Shift+drag rubber-band
    /// selection. Routes to the registered brush-select handler on the session, if any.
    /// Non-mutating — the figure is never re-rendered from a brush-select.</summary>
    public void OnBrushSelect(BrushSelectEvent evt) => _registry.Publish(evt.ChartId, evt);

    /// <summary>Client-to-server (v1.2.2): hover event. The client supplies the data-space
    /// point via <see cref="HoverEventPayload"/>; the hub stamps
    /// <see cref="HoverEvent.CallerConnectionId"/> from <see cref="Microsoft.AspNetCore.SignalR.HubCallerContext.ConnectionId"/>
    /// server-side so a client cannot spoof another user's connection. The session's hover
    /// handler computes tooltip HTML which is delivered to the originating caller only via
    /// <see cref="IChartHubClient.ReceiveTooltipContent"/>.</summary>
    public void OnHover(HoverEventPayload payload)
    {
        var evt = new HoverEvent(
            payload.ChartId,
            payload.AxesIndex,
            payload.X,
            payload.Y,
            CallerConnectionId: Context.ConnectionId);
        _registry.Publish(evt.ChartId, evt);
    }
}
