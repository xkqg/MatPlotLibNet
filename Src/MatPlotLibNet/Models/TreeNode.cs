// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a node in a hierarchical tree structure, used by Treemap and Sunburst series.</summary>
public sealed record TreeNode
{
    /// <summary>Gets the display label for this node.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the value of this node (used for leaf nodes).</summary>
    public double Value { get; init; }

    /// <summary>Gets the optional color override for this node.</summary>
    public Color? Color { get; init; }

    /// <summary>Gets the child nodes of this node.</summary>
    public IReadOnlyList<TreeNode> Children { get; init; } = [];

    /// <summary>Computes the total value: own value if leaf, sum of children if branch.</summary>
    public double TotalValue => Children.Count > 0
        ? Children.Sum(c => c.TotalValue)
        : Value;
}
