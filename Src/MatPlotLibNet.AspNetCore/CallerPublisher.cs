// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Default <see cref="ICallerPublisher"/> implementation that routes per-caller
/// tooltip responses via <c>IHubContext.Clients.Client(connectionId)</c>. This is the only
/// place in the library that uses the <c>Clients.Client</c> targeting pattern — all other
/// broadcasts go through <c>Clients.Group</c>.</summary>
public sealed class CallerPublisher(IHubContext<ChartHub, IChartHubClient> hubContext) : ICallerPublisher
{
    /// <inheritdoc />
    public Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default) =>
        hubContext.Clients.Client(connectionId).ReceiveTooltipContent(chartId, html);
}
