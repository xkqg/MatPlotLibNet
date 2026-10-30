// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The rule that cuts a tree into cells, read by the sunburst (as angles) and the icicle (as widths): children divide
/// their parent's extent in proportion to their total values, in the stored order; a parent whose children add up to nothing
/// positive places none of them.</summary>
public class TreeNodePartitionTests
{
    private static TreeNode Leaf(string label, double value) => new() { Label = label, Value = value };

    private static TreeNode Branch(string label, params TreeNode[] children) => new() { Label = label, Children = children };

    [Fact]
    public void TheChildrenOfANode_DivideItsExtent_InProportionToTheirTotals_InStoredOrder()
    {
        var root = Branch("root", Leaf("A", 3), Leaf("B", 1));

        var shares = root.Shares();

        Assert.Equal(["A", "B"], shares.Select(share => share.Node.Label));
        Assert.Equal([0.75, 0.25], shares.Select(share => share.Share));
    }

    [Fact]
    public void ABranchChild_TakesItsTotalNotAValueItWasGiven()
    {
        // The branch's own Value is ignored once it has children: its total is what its children add up to.
        var branch = new TreeNode { Label = "B", Value = 100, Children = [Leaf("B1", 1), Leaf("B2", 1)] };
        var root = Branch("root", Leaf("A", 2), branch);

        var shares = root.Shares();

        Assert.Equal([0.5, 0.5], shares.Select(share => share.Share));
    }

    [Fact]
    public void TheSharesOfASubtree_AreRelativeToItsOwnParent()
    {
        var root = Branch("root", Branch("A", Leaf("A1", 1), Leaf("A2", 3)), Leaf("B", 4));

        var inner = root.Children[0].Shares();

        Assert.Equal([0.25, 0.75], inner.Select(share => share.Share));
    }

    [Fact]
    public void AChildWorthZero_AmongOthers_TakesAShareOfZero()
    {
        var shares = Branch("root", Leaf("A", 0), Leaf("B", 2)).Shares();

        Assert.Equal([0.0, 1.0], shares.Select(share => share.Share));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(-1.0, -2.0)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(1.0, double.NegativeInfinity)]
    public void ChildrenThatDoNotAddUpToAPositiveNumber_ArePlacedNowhere(double first, double second)
    {
        Assert.Empty(Branch("root", Leaf("A", first), Leaf("B", second)).Shares());
    }

    [Fact]
    public void ALeaf_HasNoChildrenToPlace()
    {
        Assert.Empty(Leaf("A", 5).Shares());
    }

    [Fact]
    public void TheLevelsBelowANode_AreTheHeightOfItsSubtree()
    {
        Assert.Equal(0, Leaf("A", 1).Levels());
        Assert.Equal(1, Branch("root", Leaf("A", 1), Leaf("B", 1)).Levels());
        Assert.Equal(3, Branch("root", Leaf("A", 1), Branch("B", Branch("C", Leaf("D", 1)))).Levels());
    }
}
