// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>The flow-family verbs of <see cref="Axes"/> (<c>Sankey</c> and <c>Chord</c>), each constructing a series and handing it to
/// <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesFlowVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a Sankey diagram series to the axes.</summary>
        public SankeySeries Sankey(SankeyNode[] nodes, SankeyLink[] links)
        {
            links.ThrowIfPointingOutside(nodes.Length, nameof(links));
            return axes.AddSeries(new SankeySeries(nodes, links));
        }

        /// <summary>Adds a chord diagram of the flows between groups to the axes.</summary>
        /// <param name="names">The name of each group.</param>
        /// <param name="flows">The flow from each group (row) to each group (column); square, finite, not negative, not all zero.</param>
        /// <returns>The newly created <see cref="ChordSeries"/> for further configuration.</returns>
        /// <exception cref="ArgumentException"><paramref name="names"/> does not name each row of <paramref name="flows"/>, or the matrix
        /// cannot be drawn (it is not square, holds a flow that is negative, infinite or not a number, or is all zero).</exception>
        public ChordSeries Chord(string[] names, double[,] flows)
        {
            ChordLayout.Compute(flows);
            names.Length.ThrowIfMisaligned(flows.GetLength(0), nameof(names));
            return axes.AddSeries(new ChordSeries(names, flows));
        }
    }

    extension(IReadOnlyList<SankeyLink> links)
    {
        /// <summary>Throws when a link starts or ends at a node the diagram does not have (a node is named by its place in the
        /// list of nodes).</summary>
        /// <param name="nodeCount">How many nodes the diagram has.</param>
        /// <param name="name">The name of the parameter the links came in, for the error.</param>
        /// <exception cref="ArgumentOutOfRangeException">A link points at a node before the first or past the last; the message
        /// names the link, whether it is the source or the target, and the node.</exception>
        private void ThrowIfPointingOutside(int nodeCount, string name)
        {
            for (int i = 0; i < links.Count; i++)
            {
                links[i].SourceIndex.ThrowIfNotIndexOf(nodeCount, name, i, static place => string.Invariant($"The source of link {place}"));
                links[i].TargetIndex.ThrowIfNotIndexOf(nodeCount, name, i, static place => string.Invariant($"The target of link {place}"));
            }
        }
    }
}
