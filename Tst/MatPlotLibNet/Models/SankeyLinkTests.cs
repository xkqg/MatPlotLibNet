// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SankeyLink"/> behavior.</summary>
public class SankeyLinkTests
{
    /// <summary>Verifies that constructor stores source, target, and value.</summary>
    [Fact]
    public void Constructor_StoresProperties()
    {
        var link = new SankeyLink(0, 2, 42.5);
        Assert.Equal(0, link.SourceIndex);
        Assert.Equal(2, link.TargetIndex);
        Assert.Equal(42.5, link.Value);
    }
}
