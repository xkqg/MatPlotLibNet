// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PolarBarSeries"/> default properties and construction.</summary>
public class PolarBarSeriesTests
{
    /// <summary>Verifies that the constructor stores R and Theta data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] r = [5, 10], theta = [0, 1.57];
        var series = new PolarBarSeries(r, theta);
        Assert.Equal(r, series.R);
        Assert.Equal(theta, series.Theta);
    }

    /// <summary>Verifies that BarWidth defaults to 0.3.</summary>
    [Fact]
    public void DefaultBarWidth_Is0Point3()
    {
        var series = new PolarBarSeries([1.0], [0.0]);
        Assert.Equal(0.3, series.BarWidth);
    }

    /// <summary>Verifies that Alpha defaults to 0.8.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point8()
    {
        var series = new PolarBarSeries([1.0], [0.0]);
        Assert.Equal(0.8, series.Alpha);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new PolarBarSeries([1.0], [0.0]);
        Assert.Null(series.Color);
    }
}
