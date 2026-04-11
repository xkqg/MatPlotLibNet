// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a node in a hierarchical tree structure, used by Treemap and Sunburst series.</summary>
public sealed record TreeNode
{
    public string Label { get; init; } = "";

    public double Value { get; init; }

    public Color? Color { get; init; }

    public IReadOnlyList<TreeNode> Children { get; init; } = Array.Empty<TreeNode>();

    /// <summary>Computes the total value: own value if leaf, sum of children if branch.</summary>
    public double TotalValue => Children.Count > 0
        ? Children.Sum(c => c.TotalValue)
        : Value;
}
