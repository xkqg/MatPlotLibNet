// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies the one owner of the tree a hierarchy reads back from the figure JSON: the tree the document carries, or a bare
/// root named <c>Root</c> when it carries none, for every hierarchy kind alike.</summary>
public class TreeNodeSerializationExtensionsTests
{
    [Fact]
    public void TreeOrBareRoot_OfNoTree_IsABareRootNamedRoot()
    {
        var root = ((TreeNodeDto?)null).TreeOrBareRoot();

        Assert.Equal("Root", root.Label);
        Assert.Empty(root.Children);
        Assert.Equal(0, root.Value);
    }

    [Fact]
    public void TreeOrBareRoot_OfATree_IsThatTree()
    {
        var dto = new TreeNodeDto { Label = "all", Children = [new TreeNodeDto { Label = "a", Value = 2 }] };

        var root = dto.TreeOrBareRoot();

        Assert.Equal("all", root.Label);
        Assert.Equal("a", Assert.Single(root.Children).Label);
    }
}
