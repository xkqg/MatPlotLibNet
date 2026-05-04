// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Tree-walking helpers over <see cref="TreeNode"/>. Replaces the per-renderer
/// hand-rolled DFS recursion that would otherwise duplicate across every hierarchical
/// series renderer.</summary>
public static class TreeNodeExtensions
{
    /// <summary>Yields every node in the subtree rooted at <paramref name="root"/> in
    /// depth-first pre-order: parent first, then each child's subtree (left-to-right).</summary>
    /// <param name="root">The subtree root. Yielded as the first element.</param>
    /// <returns>A lazy enumerable visiting <paramref name="root"/> followed by every
    /// descendant.</returns>
    /// <remarks>Predicate-cut walks (e.g. SciPy <c>color_threshold</c> cluster collection)
    /// and bottom-up folds (e.g. layout coordinate computation) need their own bespoke
    /// recursion because they prune or aggregate state during descent — this enumerable
    /// suits the simple "visit-all" case only.</remarks>
    public static IEnumerable<TreeNode> Walk(this TreeNode root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var node in child.Walk())
                yield return node;
    }
}
