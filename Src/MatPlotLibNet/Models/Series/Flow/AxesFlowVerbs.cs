// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

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
            links.ThrowIfPointingOutside(nodes.Length);
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
            if (names.Length != flows.GetLength(0))
                throw new ArgumentException($"names must name each of the {flows.GetLength(0)} groups of flows; it has {names.Length} names.", nameof(names));
            return axes.AddSeries(new ChordSeries(names, flows));
        }
    }
}
