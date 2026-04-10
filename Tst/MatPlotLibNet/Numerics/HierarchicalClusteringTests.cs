// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="HierarchicalClustering"/> correctness.</summary>
public class HierarchicalClusteringTests
{
    [Fact]
    public void TwoPoints_ProducesSingleMerge()
    {
        var dist = new double[,] { { 0, 1 }, { 1, 0 } };
        var result = HierarchicalClustering.Cluster(dist);
        Assert.Single(result.Merges);
    }

    [Fact]
    public void ThreePoints_ProducesTwoMerges()
    {
        var dist = new double[,]
        {
            { 0.0, 1.0, 4.0 },
            { 1.0, 0.0, 3.0 },
            { 4.0, 3.0, 0.0 }
        };
        var result = HierarchicalClustering.Cluster(dist);
        Assert.Equal(2, result.Merges.Length);
    }

    [Fact]
    public void LeafOrder_ContainsAllLeafIndices()
    {
        var dist = new double[,]
        {
            { 0, 2, 6, 10 },
            { 2, 0, 5,  9 },
            { 6, 5, 0,  4 },
            { 10, 9, 4, 0 }
        };
        var result = HierarchicalClustering.Cluster(dist);
        Assert.Equal(4, result.LeafOrder.Length);
        // All indices 0-3 should appear exactly once
        var sorted = result.LeafOrder.OrderBy(x => x).ToArray();
        Assert.Equal([0, 1, 2, 3], sorted);
    }

    [Fact]
    public void IdenticalPoints_ZeroDistance_Clusters()
    {
        // Two identical points have distance 0 — should cluster first
        var dist = new double[,]
        {
            { 0, 0, 5 },
            { 0, 0, 5 },
            { 5, 5, 0 }
        };
        var result = HierarchicalClustering.Cluster(dist);
        // First merge should be indices 0 and 1 (distance 0)
        Assert.Equal(0, result.Merges[0].Distance);
    }

    [Fact]
    public void EmptyMatrix_ReturnsEmptyDendrogram()
    {
        var result = HierarchicalClustering.Cluster(new double[0, 0]);
        Assert.Empty(result.Merges);
        Assert.Empty(result.LeafOrder);
    }
}
