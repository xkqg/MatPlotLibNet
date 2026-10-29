// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The one rule a Sankey's links obey: they name nodes that exist.</summary>
internal static class SankeyLinkExtensions
{
    extension(IReadOnlyList<SankeyLink> links)
    {
        /// <summary>Throws when a link starts or ends at a node the diagram does not have (a node is named by its place in the
        /// list of nodes).</summary>
        /// <param name="nodeCount">How many nodes the diagram has.</param>
        /// <exception cref="ArgumentException">A link points at a node before the first or past the last; the message names the
        /// link and the node.</exception>
        internal void ThrowIfPointingOutside(int nodeCount)
        {
            for (int i = 0; i < links.Count; i++)
            {
                foreach (int node in (int[])[links[i].SourceIndex, links[i].TargetIndex])
                {
                    if (node < 0 || node >= nodeCount)
                        throw new ArgumentException($"Link {i} points at node {node}, but there are only {nodeCount} nodes.");
                }
            }
        }
    }
}
