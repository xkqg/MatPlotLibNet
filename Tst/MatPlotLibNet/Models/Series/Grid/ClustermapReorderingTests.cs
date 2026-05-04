// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>v1.10 — Verifies <see cref="ClustermapSeries.ResolveLeafOrder"/> in isolation.
/// Pure unit tests: no rendering, no SVG, no serialization.</summary>
public class ClustermapReorderingTests
{
    // ── Null / missing tree ───────────────────────────────────────────────────

    [Fact]
    public void NullTree_ReturnsIdentityOrder()
    {
        var order = ClustermapSeries.ResolveLeafOrder(null, 3);
        Assert.Equal([0, 1, 2], order);
    }

    [Fact]
    public void NullTree_ZeroCount_ReturnsEmpty()
    {
        var order = ClustermapSeries.ResolveLeafOrder(null, 0);
        Assert.Empty(order);
    }

    // ── Pure-leaf root (no children) ──────────────────────────────────────────

    [Fact]
    public void SingleLeafRoot_ReturnsIdentityOrder()
    {
        var leaf = new TreeNode { Label = "A", Value = 0.0 };
        var order = ClustermapSeries.ResolveLeafOrder(leaf, 1);
        Assert.Equal([0], order);
    }

    // ── Two-leaf tree ─────────────────────────────────────────────────────────

    [Fact]
    public void TwoLeafTree_ValuesInOrder_ReturnsOrder()
    {
        var root = new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 0.0 }, // index 0
                new TreeNode { Value = 1.0 }, // index 1
            ]
        };
        var order = ClustermapSeries.ResolveLeafOrder(root, 2);
        Assert.Equal([0, 1], order);
    }

    [Fact]
    public void TwoLeafTree_ValuesSwapped_ReturnsSwappedOrder()
    {
        var root = new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 1.0 }, // index 1
                new TreeNode { Value = 0.0 }, // index 0
            ]
        };
        var order = ClustermapSeries.ResolveLeafOrder(root, 2);
        Assert.Equal([1, 0], order);
    }

    // ── Three-leaf balanced tree ───────────────────────────────────────────────

    [Fact]
    public void ThreeLeafTree_ReturnsLeafValuesInDfsOrder()
    {
        // DFS pre-order visits: root → left-child (leaf 2) → right-child → left-leaf (0) → right-leaf (1)
        var root = new TreeNode
        {
            Value = 2.0,
            Children =
            [
                new TreeNode { Value = 2.0 }, // leaf: index 2
                new TreeNode
                {
                    Value = 1.0,
                    Children =
                    [
                        new TreeNode { Value = 0.0 }, // leaf: index 0
                        new TreeNode { Value = 1.0 }, // leaf: index 1
                    ]
                }
            ]
        };
        var order = ClustermapSeries.ResolveLeafOrder(root, 3);
        Assert.Equal([2, 0, 1], order);
    }

    // ── Out-of-range leaf index: defensive fallback ───────────────────────────

    [Fact]
    public void OutOfRangeLeafIndex_FallsBackToIdentity()
    {
        var root = new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 99.0 }, // index 99 — out of range for count=2
                new TreeNode { Value = 0.0 },
            ]
        };
        var order = ClustermapSeries.ResolveLeafOrder(root, 2);
        Assert.Equal([0, 1], order); // identity fallback
    }

    // ── Count mismatch: tree has fewer leaves than count ─────────────────────

    [Fact]
    public void FewerLeavesInTreeThanCount_FallsBackToIdentity()
    {
        var root = new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 0.0 },
                new TreeNode { Value = 1.0 },
            ]
        };
        // count=4 but tree only has 2 leaves
        var order = ClustermapSeries.ResolveLeafOrder(root, 4);
        Assert.Equal([0, 1, 2, 3], order); // identity fallback
    }

    // ── Duplicate leaf indices: defensive fallback ────────────────────────────

    [Fact]
    public void DuplicateLeafIndex_FallsBackToIdentity()
    {
        var root = new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 0.0 }, // index 0 appears twice
                new TreeNode { Value = 0.0 },
            ]
        };
        var order = ClustermapSeries.ResolveLeafOrder(root, 2);
        Assert.Equal([0, 1], order); // identity fallback
    }

    // ── Count mismatch: tree has MORE leaves than count ──────────────────────

    [Fact]
    public void MoreLeavesInTreeThanCount_FallsBackToIdentity()
    {
        // 3 leaves but count=2 → order.Count(3) != count(2) → identity
        var root = new TreeNode
        {
            Value = 2.0,
            Children =
            [
                new TreeNode { Value = 0.0 },
                new TreeNode { Value = 1.0 },
                new TreeNode { Value = 2.0 }, // extra leaf
            ]
        };
        var order = ClustermapSeries.ResolveLeafOrder(root, 2);
        Assert.Equal([0, 1], order);
    }

    // ── Zero count with non-null tree ─────────────────────────────────────────

    [Fact]
    public void ZeroCount_NonNullTree_ReturnsEmpty()
    {
        var leaf = new TreeNode { Value = 0.0 };
        var order = ClustermapSeries.ResolveLeafOrder(leaf, 0);
        Assert.Empty(order);
    }

    // ── Cell value correctness after permute ──────────────────────────────────

    [Fact]
    public void ResolvedOrder_PermutesMatrixCellsCorrectly()
    {
        // row DFS order: leaf 2 first, then leaf 0, then leaf 1
        var rowRoot = new TreeNode
        {
            Value = 2.0,
            Children =
            [
                new TreeNode { Value = 2.0 },
                new TreeNode
                {
                    Value = 1.0,
                    Children =
                    [
                        new TreeNode { Value = 0.0 },
                        new TreeNode { Value = 1.0 },
                    ]
                }
            ]
        };

        // column DFS order: leaf 1 first, then leaf 0
        var colRoot = new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 1.0 },
                new TreeNode { Value = 0.0 },
            ]
        };

        double[,] data =
        {
            { 10, 20 }, // row 0
            { 30, 40 }, // row 1
            { 50, 60 }, // row 2
        };

        int[] rowOrder = ClustermapSeries.ResolveLeafOrder(rowRoot, 3); // [2, 0, 1]
        int[] colOrder = ClustermapSeries.ResolveLeafOrder(colRoot, 2); // [1, 0]

        // Manually apply permutation: reordered[r,c] = data[rowOrder[r], colOrder[c]]
        Assert.Equal([2, 0, 1], rowOrder);
        Assert.Equal([1, 0], colOrder);

        // Cell (0,0): data[rowOrder[0], colOrder[0]] = data[2,1] = 60
        Assert.Equal(60, data[rowOrder[0], colOrder[0]]);
        // Cell (0,1): data[2,0] = 50
        Assert.Equal(50, data[rowOrder[0], colOrder[1]]);
        // Cell (1,0): data[0,1] = 20
        Assert.Equal(20, data[rowOrder[1], colOrder[0]]);
        // Cell (2,0): data[1,1] = 40
        Assert.Equal(40, data[rowOrder[2], colOrder[0]]);
    }
}
