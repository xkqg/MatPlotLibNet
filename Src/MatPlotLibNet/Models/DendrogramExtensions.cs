// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Models;

/// <summary>A clustering as the tree a chart draws: the merges of a <see cref="Dendrogram"/> become one <see cref="TreeNode"/>, the
/// model a dendrogram and a clustermap are drawn from.</summary>
public static class DendrogramExtensions
{
    /// <param name="dendrogram">The clustering to turn into a tree.</param>
    extension(Dendrogram dendrogram)
    {
        /// <summary>The tree of the clustering: a leaf for each observation, valued by its index (the index a clustermap reorders
        /// its rows and columns by) and named by its label, and a node for each merge, valued by the distance it merged at, with the
        /// two clusters it merged as its children. The root is the last merge, and the leaves stand in the clustering's own order
        /// (<see cref="Dendrogram.LeafOrder"/>).</summary>
        /// <param name="leafLabels">The name of each observation, by its index; <see langword="null"/> leaves every leaf unnamed.</param>
        /// <returns>The root of the tree: a lone leaf for a clustering of one observation, an empty leaf for one of none.</returns>
        public TreeNode ToTree(IReadOnlyList<string>? leafLabels = null)
        {
            int count = dendrogram.LeafOrder.Length;
            var nodes = new TreeNode[count + dendrogram.Merges.Length];
            for (int i = 0; i < count; i++)
            {
                nodes[i] = new TreeNode { Label = leafLabels?[i] ?? "", Value = i };
            }

            for (int m = 0; m < dendrogram.Merges.Length; m++)
            {
                var merge = dendrogram.Merges[m];
                nodes[count + m] = new TreeNode { Value = merge.Distance, Children = [nodes[merge.Left], nodes[merge.Right]] };
            }

            return nodes.Length == 0 ? new TreeNode() : nodes[^1];
        }
    }
}
