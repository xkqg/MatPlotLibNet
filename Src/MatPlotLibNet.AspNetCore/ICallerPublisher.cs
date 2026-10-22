// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore;

/// <summary>Abstraction over SignalR's per-connection send pattern: where <see cref="IChartPublisher"/>
/// broadcasts to a group, this answers a single caller, which is how the content of a hover tooltip is
/// delivered. Extracted as an interface so
/// <c>ChartSession</c> can depend on the narrow pattern rather than <c>IHubContext</c>
/// directly, which keeps tests mockable without spinning up a SignalR test server.</summary>
public interface ICallerPublisher
{
    /// <summary>Sends a tooltip HTML fragment to a single connected client identified by
    /// <paramref name="connectionId"/>. Used by <c>ChartSession.DrainAsync</c> after a
    /// <see cref="Interaction.HoverEvent"/> handler returns content.</summary>
    Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default);
}
