// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BarSeries"/> default properties and construction.</summary>
public class BarSeriesTests
{
    /// <summary>Verifies that the constructor stores categories and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        string[] cats = ["A", "B"];
        double[] vals = [1, 2];
        var series = new BarSeries(cats, vals);
        Assert.Equal(cats, series.Categories);
        Assert.Equal(vals, series.Values);
    }

    /// <summary>Verifies that Orientation defaults to Vertical.</summary>
    [Fact]
    public void DefaultOrientation_IsVertical()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(BarOrientation.Vertical, series.Orientation);
    }

    /// <summary>Verifies that BarWidth defaults to 0.8.</summary>
    [Fact]
    public void DefaultBarWidth_Is0Point8()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(0.8, series.BarWidth);
    }
}
