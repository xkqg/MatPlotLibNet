// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>One child of a node and the share of its parent's extent it takes.</summary>
/// <param name="Node">The child.</param>
/// <param name="Share">The child's <see cref="TreeNode.TotalValue"/> over the sum of its siblings', between 0 and 1.</param>
internal readonly record struct PartitionShare(TreeNode Node, double Share);

/// <summary>The one rule that cuts a tree into cells: the children of a node divide the extent of their parent in proportion
/// to their total values, in the order they are stored. A sunburst reads the extent as an angle and an icicle as a width; the
/// rule is the same for both, so the two cannot partition one tree differently.</summary>
internal static class TreeNodePartitionExtensions
{
    extension(TreeNode node)
    {
        /// <summary>The children of the node with the share of the node's extent each takes. A node whose children do not add
        /// up to a positive number (nothing in them, a negative sum, a value that is not a number) places none of them: there
        /// is nothing to divide, and a share of NaN would be drawn as a garbled wedge. A child worth zero among others takes a
        /// share of zero.</summary>
        /// <returns>One entry per child, in the stored order; none for a leaf or for children with no positive total.</returns>
        internal IReadOnlyList<PartitionShare> Shares()
        {
            if (node.Children.Count == 0) return [];
            double total = node.TotalValue;
            if (!(total > 0) || !double.IsFinite(total)) return [];

            var shares = new PartitionShare[node.Children.Count];
            for (int i = 0; i < shares.Length; i++)
            {
                shares[i] = new PartitionShare(node.Children[i], node.Children[i].TotalValue / total);
            }

            return shares;
        }

        /// <summary>How many levels lie below the node: 0 for a leaf, 1 for a node whose children are leaves.</summary>
        /// <returns>The height of the node's subtree.</returns>
        internal int Levels() => node.Children.Count == 0 ? 0 : 1 + node.Children.Max(child => child.Levels());
    }
}
