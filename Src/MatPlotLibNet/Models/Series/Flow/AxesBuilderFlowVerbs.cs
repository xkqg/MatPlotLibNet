// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the flow family (a Sankey diagram, a chord diagram): each adds
/// the series through the axes verb of the same name (<see cref="AxesFlowVerbs"/>) and hands it to the caller's
/// callback.</summary>
public static class AxesBuilderFlowVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a Sankey diagram series to the axes.</summary>
        public AxesBuilder Sankey(SankeyNode[] nodes, SankeyLink[] links, Action<SankeySeries>? configure = null)
            => axes.Configured(axes.Axes.Sankey(nodes, links), configure);

        /// <summary>Adds a chord diagram of the flows between groups to the axes.</summary>
        public AxesBuilder Chord(string[] names, double[,] flows, Action<ChordSeries>? configure = null)
            => axes.Configured(axes.Axes.Chord(names, flows), configure);
    }
}
