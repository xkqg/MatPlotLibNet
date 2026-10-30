// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet;

/// <summary>The hierarchical-family verbs of <see cref="Axes"/> (<c>Treemap</c>, <c>Sunburst</c>, <c>Dendrogram</c>),
/// each constructing one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesHierarchicalVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a treemap series to the axes.</summary>
        public TreemapSeries Treemap(TreeNode root) => axes.AddSeries(new TreemapSeries(root));

        /// <summary>Adds a sunburst series to the axes.</summary>
        public SunburstSeries Sunburst(TreeNode root) => axes.AddSeries(new SunburstSeries(root));

        /// <summary>Adds an icicle chart to the axes: the hierarchy under <paramref name="root"/> as rows of cells, one row for
        /// each level, each cell as wide as its node's share of its parent's width.</summary>
        /// <param name="root">The root of the hierarchy; it is not drawn, its children are the first row.</param>
        /// <returns>The newly created <see cref="IcicleSeries"/> for further configuration.</returns>
        public IcicleSeries Icicle(TreeNode root) => axes.AddSeries(new IcicleSeries(root));

        /// <summary>Adds a dendrogram series to the axes.</summary>
        /// <param name="root">The root <see cref="TreeNode"/>. Internal nodes carry their merge
        /// distance in <see cref="TreeNode.Value"/>.</param>
        /// <returns>The newly added <see cref="DendrogramSeries"/> for further property configuration.</returns>
        public DendrogramSeries Dendrogram(TreeNode root) => axes.AddSeries(new DendrogramSeries(root));
    }
}
