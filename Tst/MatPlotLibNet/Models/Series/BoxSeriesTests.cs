// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BoxSeries"/> default properties and construction.</summary>
public class BoxSeriesTests
{
    /// <summary>Verifies that the constructor stores datasets.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[][] datasets = [[1.0, 2.0, 3.0]];
        var series = new BoxSeries(datasets);
        Assert.Equal(datasets, series.Datasets);
    }

    /// <summary>Verifies that ShowOutliers defaults to true.</summary>
    [Fact]
    public void DefaultShowOutliers_IsTrue()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.True(series.ShowOutliers);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that Widths defaults to 0.5.</summary>
    [Fact]
    public void DefaultWidths_Is0Point5()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Equal(0.5, series.Widths);
    }

    /// <summary>Verifies that Vert defaults to true.</summary>
    [Fact]
    public void DefaultVert_IsTrue()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.True(series.Vert);
    }

    /// <summary>Verifies that Whis defaults to 1.5.</summary>
    [Fact]
    public void DefaultWhis_Is1Point5()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Equal(1.5, series.Whis);
    }

    /// <summary>Verifies that ShowMeans defaults to false.</summary>
    [Fact]
    public void DefaultShowMeans_IsFalse()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.False(series.ShowMeans);
    }

    /// <summary>Verifies that Positions defaults to null.</summary>
    [Fact]
    public void DefaultPositions_IsNull()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Null(series.Positions);
    }
}
