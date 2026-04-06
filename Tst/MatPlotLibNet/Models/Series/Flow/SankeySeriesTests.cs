// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SankeySeries"/> default properties and construction.</summary>
public class SankeySeriesTests
{
    private static SankeyNode[] SampleNodes => [new("A"), new("B"), new("C")];
    private static SankeyLink[] SampleLinks => [new(0, 1, 10), new(0, 2, 20)];

    /// <summary>Verifies that the constructor stores nodes and links.</summary>
    [Fact]
    public void Constructor_StoresNodesAndLinks()
    {
        var series = new SankeySeries(SampleNodes, SampleLinks);
        Assert.Equal(3, series.Nodes.Count);
        Assert.Equal(2, series.Links.Count);
    }

    /// <summary>Verifies that NodeWidth defaults to 20.</summary>
    [Fact]
    public void DefaultNodeWidth_Is20()
    {
        var series = new SankeySeries(SampleNodes, SampleLinks);
        Assert.Equal(20, series.NodeWidth);
    }

    /// <summary>Verifies that NodePadding defaults to 10.</summary>
    [Fact]
    public void DefaultNodePadding_Is10()
    {
        var series = new SankeySeries(SampleNodes, SampleLinks);
        Assert.Equal(10, series.NodePadding);
    }

    /// <summary>Verifies that LinkAlpha defaults to 0.4.</summary>
    [Fact]
    public void DefaultLinkAlpha_Is0Point4()
    {
        var series = new SankeySeries(SampleNodes, SampleLinks);
        Assert.Equal(0.4, series.LinkAlpha);
    }
}
