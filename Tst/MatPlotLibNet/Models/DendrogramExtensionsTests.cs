// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Models;

/// <summary>A clustering's merges become one tree (<see cref="DendrogramExtensions"/>): a leaf per observation that names it by
/// its index, a node per merge at the distance it merged at, the root the last merge, and the leaves in the clustering's own
/// order, so a chart that draws trees draws a clustering.</summary>
public class DendrogramExtensionsTests
{
    // Leaves 0 and 2 merge first (at 1), then 1 and 3 (at 2), then the two pairs (at 6).
    private static readonly Dendrogram FourLeaves = new(
        [new DendrogramNode(0, 2, 1.0, 2), new DendrogramNode(1, 3, 2.0, 2), new DendrogramNode(4, 5, 6.0, 4)],
        [0, 2, 1, 3]);

    private static IEnumerable<TreeNode> Leaves(TreeNode node) =>
        node.Children.Count == 0 ? [node] : node.Children.SelectMany(Leaves);

    [Fact]
    public void TheRoot_IsTheLastMerge_AtTheDistanceItMergedAt()
    {
        var root = FourLeaves.ToTree();

        Assert.Equal(6.0, root.Value);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal([1.0, 2.0], root.Children.Select(child => child.Value));
    }

    [Fact]
    public void TheLeaves_StandInTheClusteringsOrder_EachValuedByItsIndex()
    {
        Assert.Equal([0.0, 2.0, 1.0, 3.0], Leaves(FourLeaves.ToTree()).Select(leaf => leaf.Value));
    }

    [Fact]
    public void TheLeaves_TakeTheLabelsGivenForTheirObservations_AndNoneWhenNoneAreGiven()
    {
        Assert.Equal(["a", "c", "b", "d"], Leaves(FourLeaves.ToTree(["a", "b", "c", "d"])).Select(leaf => leaf.Label));
        Assert.All(Leaves(FourLeaves.ToTree()), leaf => Assert.Equal("", leaf.Label));
    }

    [Fact]
    public void AClusteringOfOneObservation_IsOneLeaf()
    {
        var root = HierarchicalClustering.Cluster(new double[,] { { 0 } }).ToTree(["only"]);

        Assert.Empty(root.Children);
        Assert.Equal(0.0, root.Value);
        Assert.Equal("only", root.Label);
    }

    [Fact]
    public void AClusteringOfNothing_IsAnEmptyLeaf() =>
        Assert.Empty(HierarchicalClustering.Cluster(new double[0, 0]).ToTree().Children);

    [Fact]
    public void ATreeOfAClustering_OrdersAClustermapsRowsAsTheClusteringDoes()
    {
        var clustering = HierarchicalClustering.Cluster(new double[,] { { 1, 2 }, { 9, 9 }, { 1.1, 2.1 } }.RowDistances());

        Assert.Equal(clustering.LeafOrder, MatPlotLibNet.Models.Series.ClustermapSeries.ResolveLeafOrder(clustering.ToTree(), 3));
    }
}
