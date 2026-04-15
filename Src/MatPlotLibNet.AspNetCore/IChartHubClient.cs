// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore;

/// <summary>SignalR client interface for receiving real-time chart updates.</summary>
public interface IChartHubClient
{
    /// <summary>Receives a chart update as serialized JSON.</summary>
    /// <param name="chartId">The identifier of the chart being updated.</param>
    /// <param name="figureJson">The JSON-serialized figure data.</param>
    Task UpdateChart(string chartId, string figureJson);

    /// <summary>Receives a chart update as pre-rendered SVG markup.</summary>
    /// <param name="chartId">The identifier of the chart being updated.</param>
    /// <param name="svg">The SVG string for the updated chart.</param>
    Task UpdateChartSvg(string chartId, string svg);

    /// <summary>Receives a tooltip HTML fragment in response to a <see cref="Interaction.HoverEvent"/>.
    /// Delivered via <c>Clients.Client(connectionId)</c> to the originating caller only — not
    /// broadcast to the SignalR group. The client-side dispatcher script renders the fragment
    /// in a styled tooltip overlay near the hovered point.</summary>
    /// <param name="chartId">The identifier of the chart the hover originated from.</param>
    /// <param name="html">The tooltip HTML fragment returned by the server-side hover handler.</param>
    Task ReceiveTooltipContent(string chartId, string html);
}
