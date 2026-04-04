// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;

namespace MatPlotLibNet.AspNetCore;

/// <summary>SignalR hub that manages client subscriptions to real-time chart update groups.</summary>
public sealed class ChartHub : Hub<IChartHubClient>
{
    /// <summary>Subscribes the calling client to updates for the specified chart.</summary>
    /// <param name="chartId">The identifier of the chart to subscribe to.</param>
    public Task Subscribe(string chartId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, chartId);

    /// <summary>Unsubscribes the calling client from updates for the specified chart.</summary>
    /// <param name="chartId">The identifier of the chart to unsubscribe from.</param>
    public Task Unsubscribe(string chartId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, chartId);
}
