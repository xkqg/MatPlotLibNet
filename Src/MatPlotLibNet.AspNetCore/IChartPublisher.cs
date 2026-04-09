// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Service interface for publishing real-time chart updates to connected SignalR clients.</summary>
public interface IChartPublisher
{
    /// <summary>Publishes a figure as JSON to all clients subscribed to the specified chart.</summary>
    /// <param name="chartId">The identifier of the chart to update.</param>
    /// <param name="figure">The figure to publish.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default);

    /// <summary>Publishes a figure as pre-rendered SVG to all clients subscribed to the specified chart.</summary>
    /// <param name="chartId">The identifier of the chart to update.</param>
    /// <param name="figure">The figure to render and publish.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default);
}
