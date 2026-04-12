// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="TreeNode"/> behavior.</summary>
public class TreeNodeTests
{
    /// <summary>Verifies that a leaf node's TotalValue equals its own Value.</summary>
    [Fact]
    public void Leaf_TotalValue_ReturnsValue()
    {
        var node = new TreeNode { Label = "A", Value = 42 };
        Assert.Equal(42, node.TotalValue);
    }

    /// <summary>Verifies that a branch node's TotalValue sums its children.</summary>
    [Fact]
    public void Branch_TotalValue_SumsChildren()
    {
        var node = new TreeNode
        {
            Label = "Root",
            Children = [
                new TreeNode { Label = "A", Value = 10 },
                new TreeNode { Label = "B", Value = 20 },
                new TreeNode { Label = "C", Value = 30 }
            ]
        };
        Assert.Equal(60, node.TotalValue);
    }

    /// <summary>Verifies that Children defaults to an empty list.</summary>
    [Fact]
    public void EmptyChildren_DefaultsToEmptyList()
    {
        var node = new TreeNode { Label = "Leaf" };
        Assert.Empty(node.Children);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void Color_DefaultsToNull()
    {
        var node = new TreeNode { Label = "X" };
        Assert.Null(node.Color);
    }

    /// <summary>Verifies that nested trees compute TotalValue recursively.</summary>
    [Fact]
    public void NestedTree_TotalValue_IsRecursive()
    {
        var tree = new TreeNode
        {
            Label = "Root",
            Children = [
                new TreeNode
                {
                    Label = "Branch",
                    Children = [
                        new TreeNode { Label = "Leaf1", Value = 5 },
                        new TreeNode { Label = "Leaf2", Value = 15 }
                    ]
                },
                new TreeNode { Label = "Leaf3", Value = 10 }
            ]
        };
        Assert.Equal(30, tree.TotalValue);
    }

    /// <summary>Verifies that Color can be set on a node.</summary>
    [Fact]
    public void Color_CanBeSet()
    {
        var node = new TreeNode { Label = "Colored", Value = 5, Color = Colors.Red };
        Assert.Equal(Colors.Red, node.Color);
    }
}
