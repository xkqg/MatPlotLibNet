// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class DonutSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new DonutSeries([30.0, 70.0]);
        Assert.Equal([30.0, 70.0], series.Sizes);
    }

    [Fact]
    public void DefaultInnerRadius_Is0Point4()
    {
        var series = new DonutSeries([50.0, 50.0]);
        Assert.Equal(0.4, series.InnerRadius);
    }
}

public class BubbleSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new BubbleSeries([1.0, 2.0], [3.0, 4.0], [10, 20]);
        Assert.Equal([1.0, 2.0], series.XData);
        Assert.Equal([3.0, 4.0], series.YData);
        Assert.Equal([10.0, 20.0], series.Sizes);
    }

}

public class OhlcBarSeriesTests
{
    [Fact]
    public void Constructor_StoresOhlcData()
    {
        var series = new OhlcBarSeries([10], [15], [8], [13]);
        Assert.Equal([10.0], series.Open);
        Assert.Equal([15.0], series.High);
    }
}

public class WaterfallSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new WaterfallSeries(["Revenue", "Cost", "Profit"], [100, -60, 40]);
        Assert.Equal(["Revenue", "Cost", "Profit"], series.Categories);
        Assert.Equal([100.0, -60.0, 40.0], series.Values);
    }
}

public class FunnelSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new FunnelSeries(["Visits", "Signups", "Paid"], [1000, 300, 50]);
        Assert.Equal(["Visits", "Signups", "Paid"], series.Labels);
        Assert.Equal([1000.0, 300.0, 50.0], series.Values);
    }
}

public class GanttSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new GanttSeries(["Task A", "Task B"], [0, 2], [3, 5]);
        Assert.Equal(["Task A", "Task B"], series.Tasks);
        Assert.Equal([0.0, 2.0], series.Starts);
        Assert.Equal([3.0, 5.0], series.Ends);
    }
}
