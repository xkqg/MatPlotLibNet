// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="TreemapSeries"/> default properties and construction.</summary>
public class TreemapSeriesTests
{
    private static TreeNode SampleTree => new()
    {
        Label = "Root",
        Children = [
            new TreeNode { Label = "A", Value = 30 },
            new TreeNode { Label = "B", Value = 70 }
        ]
    };

    /// <summary>Verifies that the constructor stores the root node.</summary>
    [Fact]
    public void Constructor_StoresRoot()
    {
        var root = SampleTree;
        var series = new TreemapSeries(root);
        Assert.Same(root, series.Root);
    }

    /// <summary>Verifies that Padding defaults to 2.</summary>
    [Fact]
    public void DefaultPadding_Is2()
    {
        var series = new TreemapSeries(SampleTree);
        Assert.Equal(2.0, series.Padding);
    }

    /// <summary>Verifies that ShowLabels defaults to true.</summary>
    [Fact]
    public void DefaultShowLabels_IsTrue()
    {
        var series = new TreemapSeries(SampleTree);
        Assert.True(series.ShowLabels);
    }

    /// <summary>Verifies that ColorMap defaults to null.</summary>
    [Fact]
    public void DefaultColorMap_IsNull()
    {
        var series = new TreemapSeries(SampleTree);
        Assert.Null(series.ColorMap);
    }
}
