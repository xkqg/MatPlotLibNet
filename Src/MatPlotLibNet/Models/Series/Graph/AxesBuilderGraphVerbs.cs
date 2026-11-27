// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verb of the axes builder that adds a network graph: it adds the series through the axes verb of the same name
/// (<see cref="AxesGraphVerbs"/>) and hands it to the caller's callback.</summary>
public static class AxesBuilderGraphVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a network-graph series to the axes (nodes + edges in 2D).</summary>
        /// <param name="nodes">The graph's nodes.</param>
        /// <param name="edges">The graph's edges.</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
        public AxesBuilder NetworkGraph(
            IReadOnlyList<GraphNode> nodes,
            IReadOnlyList<GraphEdge> edges,
            Action<NetworkGraphSeries>? configure = null)
            => axes.Configured(axes.Axes.NetworkGraph(nodes, edges), configure);
    }
}
