// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class BarSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        string[] cats = ["A", "B"];
        double[] vals = [1, 2];
        var series = new BarSeries(cats, vals);
        Assert.Equal(cats, series.Categories);
        Assert.Equal(vals, series.Values);
    }

    [Fact]
    public void DefaultOrientation_IsVertical()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(BarOrientation.Vertical, series.Orientation);
    }

    [Fact]
    public void DefaultBarWidth_Is0Point8()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(0.8, series.BarWidth);
    }
}
