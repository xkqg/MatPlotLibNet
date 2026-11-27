// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the hierarchical family (a treemap, a sunburst, an icicle, a
/// dendrogram): each adds the series through the axes verb of the same name (<see cref="AxesHierarchicalVerbs"/>) and hands it
/// to the caller's callback.</summary>
public static class AxesBuilderHierarchicalVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a treemap series to the axes.</summary>
        public AxesBuilder Treemap(TreeNode root, Action<TreemapSeries>? configure = null)
            => axes.Configured(axes.Axes.Treemap(root), configure);

        /// <summary>Adds a sunburst series to the axes.</summary>
        public AxesBuilder Sunburst(TreeNode root, Action<SunburstSeries>? configure = null)
            => axes.Configured(axes.Axes.Sunburst(root), configure);

        /// <summary>Adds an icicle chart to the axes: the hierarchy under <paramref name="root"/> as rows of cells, one row for each
        /// level, each cell as wide as its node's share of its parent's width.</summary>
        /// <param name="root">The root of the hierarchy; it is not drawn, its children are the first row.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>This builder.</returns>
        public AxesBuilder Icicle(TreeNode root, Action<IcicleSeries>? configure = null)
            => axes.Configured(axes.Axes.Icicle(root), configure);

        /// <summary>Adds a dendrogram series to the axes.</summary>
        /// <param name="root">The root <see cref="TreeNode"/>. Internal nodes carry their merge
        /// distance in <see cref="TreeNode.Value"/>.</param>
        /// <param name="configure">Optional callback to configure orientation, cut-height, and
        /// clustering colours on the new <see cref="DendrogramSeries"/>.</param>
        /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
        public AxesBuilder Dendrogram(TreeNode root, Action<DendrogramSeries>? configure = null)
            => axes.Configured(axes.Axes.Dendrogram(root), configure);

        /// <summary>
        /// Adds a nested pie series — inner filled disc showing top-level categories, outer ring
        /// showing each category's sub-breakdown. Convenience wrapper around <see cref="Sunburst"/>
        /// with <see cref="SunburstSeries.InnerRadius"/> pinned at 0. Expects a 2-level
        /// <see cref="TreeNode"/> (root → categories → sub-items); deeper trees still render but
        /// add more concentric rings.
        /// </summary>
        public AxesBuilder NestedPie(TreeNode root, Action<SunburstSeries>? configure = null)
            => axes.Sunburst(root, s =>
            {
                s.InnerRadius = 0;
                configure?.Invoke(s);
            });
    }
}
