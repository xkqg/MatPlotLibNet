// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Shared <see cref="TreeNode"/> factories for dendrogram tests. Centralises the
/// fixtures that previously appeared verbatim in <c>DendrogramSeriesTests</c> and
/// <c>DendrogramSerializationTests</c>.</summary>
internal static class DendrogramTreeFixtures
{
    /// <summary>Two-leaf tree: <c>root(2)</c> → <c>A(0)</c>, <c>B(0)</c>. The smallest tree
    /// that still produces an internal merge segment.</summary>
    public static TreeNode TwoLeaf() => new()
    {
        Label = "root",
        Value = 2.0,
        Children =
        [
            new TreeNode { Label = "A", Value = 0.0 },
            new TreeNode { Label = "B", Value = 0.0 },
        ],
    };
}
