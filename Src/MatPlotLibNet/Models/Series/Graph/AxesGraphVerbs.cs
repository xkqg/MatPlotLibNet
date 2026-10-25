// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The graph-family verb of <see cref="Axes"/> (<c>NetworkGraph</c>), constructing the series and handing
/// it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesGraphVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a network-graph series to the axes (nodes + edges in 2D).</summary>
        /// <param name="nodes">The graph's nodes (each with an Id and optional X/Y/colour/size scalars).</param>
        /// <param name="edges">The graph's edges (From/To node IDs, optional Weight, optional IsDirected).</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        /// <returns>The newly added <see cref="NetworkGraphSeries"/> for further property configuration.</returns>
        public NetworkGraphSeries NetworkGraph(
            IReadOnlyList<GraphNode> nodes,
            IReadOnlyList<GraphEdge> edges,
            Action<NetworkGraphSeries>? configure = null)
        {
            var series = new NetworkGraphSeries(nodes, edges);
            configure?.Invoke(series);
            return axes.AddSeries(series);
        }
    }
}
