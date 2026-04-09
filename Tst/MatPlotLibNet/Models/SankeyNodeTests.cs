// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SankeyNode"/> behavior.</summary>
public class SankeyNodeTests
{
    /// <summary>Verifies that Label is stored from constructor.</summary>
    [Fact]
    public void Label_StoredFromConstructor()
    {
        var node = new SankeyNode("Source");
        Assert.Equal("Source", node.Label);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void Color_DefaultsToNull()
    {
        var node = new SankeyNode("A");
        Assert.Null(node.Color);
    }

    /// <summary>Verifies that Color can be set.</summary>
    [Fact]
    public void Color_CanBeSet()
    {
        var node = new SankeyNode("A", Colors.Blue);
        Assert.Equal(Colors.Blue, node.Color);
    }
}
