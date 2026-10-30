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

    /// <summary>Another viewer of the chart moved their pointer. Sent to every other viewer of the chart, never to the one
    /// the pointer belongs to, and not through the session that applies gestures.
    /// <para>Default interface member, so a class that already implements this interface keeps compiling; the typed hub
    /// client sends it all the same.</para></summary>
    /// <param name="chartId">The chart.</param>
    /// <param name="viewerId">The connection of the viewer whose pointer it is.</param>
    /// <param name="viewerName">That viewer's name ("Viewer 2").</param>
    /// <param name="axesIndex">The axes the pointer is over.</param>
    /// <param name="x">The pointer's X in data units.</param>
    /// <param name="y">The pointer's Y in data units.</param>
    Task ReceiveCursor(string chartId, string viewerId, string viewerName, int axesIndex, double x, double y) => Task.CompletedTask;

    /// <summary>A viewer moved the shared view of the chart (zoomed, panned, reset), and this is who. Sent after the
    /// redrawn chart, to every viewer, the author included.
    /// <para>Default interface member, as <see cref="ReceiveCursor"/> is.</para></summary>
    /// <param name="chartId">The chart.</param>
    /// <param name="viewerId">The connection of the viewer who moved it.</param>
    /// <param name="viewerName">That viewer's name.</param>
    Task ViewChanged(string chartId, string viewerId, string viewerName) => Task.CompletedTask;

    /// <summary>A viewer left the chart; their pointer can go.
    /// <para>Default interface member, as <see cref="ReceiveCursor"/> is.</para></summary>
    /// <param name="chartId">The chart.</param>
    /// <param name="viewerId">The connection of the viewer who left.</param>
    Task ViewerLeft(string chartId, string viewerId) => Task.CompletedTask;
}
