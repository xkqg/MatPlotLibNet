// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SunburstSeries"/> default properties and construction.</summary>
public class SunburstSeriesTests
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
        var series = new SunburstSeries(root);
        Assert.Same(root, series.Root);
    }

    /// <summary>Verifies that InnerRadius defaults to 0.</summary>
    [Fact]
    public void DefaultInnerRadius_Is0()
    {
        var series = new SunburstSeries(SampleTree);
        Assert.Equal(0.0, series.InnerRadius);
    }

    /// <summary>Verifies that ShowLabels defaults to true.</summary>
    [Fact]
    public void DefaultShowLabels_IsTrue()
    {
        var series = new SunburstSeries(SampleTree);
        Assert.True(series.ShowLabels);
    }

    /// <summary>Verifies that ColorMap defaults to null.</summary>
    [Fact]
    public void DefaultColorMap_IsNull()
    {
        var series = new SunburstSeries(SampleTree);
        Assert.Null(series.ColorMap);
    }
}
