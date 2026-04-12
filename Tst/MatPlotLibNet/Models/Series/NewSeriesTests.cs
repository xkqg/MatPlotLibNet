// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="DonutSeries"/> default properties and construction.</summary>
public class DonutSeriesTests
{
    /// <summary>Verifies that the constructor stores sizes.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new DonutSeries([30.0, 70.0]);
        Assert.Equal([30.0, 70.0], series.Sizes);
    }

    /// <summary>Verifies that InnerRadius defaults to 0.4.</summary>
    [Fact]
    public void DefaultInnerRadius_Is0Point4()
    {
        var series = new DonutSeries([50.0, 50.0]);
        Assert.Equal(0.4, series.InnerRadius);
    }
}

/// <summary>Verifies <see cref="BubbleSeries"/> default properties and construction.</summary>
public class BubbleSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, and size data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new BubbleSeries([1.0, 2.0], [3.0, 4.0], [10, 20]);
        Assert.Equal([1.0, 2.0], series.XData);
        Assert.Equal([3.0, 4.0], series.YData);
        Assert.Equal([10.0, 20.0], series.Sizes);
    }

}

/// <summary>Verifies <see cref="OhlcBarSeries"/> default properties and construction.</summary>
public class OhlcBarSeriesTests
{
    /// <summary>Verifies that the constructor stores OHLC data arrays.</summary>
    [Fact]
    public void Constructor_StoresOhlcData()
    {
        var series = new OhlcBarSeries([10], [15], [8], [13]);
        Assert.Equal([10.0], series.Open);
        Assert.Equal([15.0], series.High);
    }
}

/// <summary>Verifies <see cref="WaterfallSeries"/> default properties and construction.</summary>
public class WaterfallSeriesTests
{
    /// <summary>Verifies that the constructor stores categories and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new WaterfallSeries(["Revenue", "Cost", "Profit"], [100, -60, 40]);
        Assert.Equal(["Revenue", "Cost", "Profit"], series.Categories);
        Assert.Equal([100.0, -60.0, 40.0], series.Values);
    }
}

/// <summary>Verifies <see cref="FunnelSeries"/> default properties and construction.</summary>
public class FunnelSeriesTests
{
    /// <summary>Verifies that the constructor stores labels and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new FunnelSeries(["Visits", "Signups", "Paid"], [1000, 300, 50]);
        Assert.Equal(["Visits", "Signups", "Paid"], series.Labels);
        Assert.Equal([1000.0, 300.0, 50.0], series.Values);
    }
}

/// <summary>Verifies <see cref="GanttSeries"/> default properties and construction.</summary>
public class GanttSeriesTests
{
    /// <summary>Verifies that the constructor stores tasks, starts, and ends.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new GanttSeries(["Task A", "Task B"], [0, 2], [3, 5]);
        Assert.Equal(["Task A", "Task B"], series.Tasks);
        Assert.Equal([0.0, 2.0], series.Starts);
        Assert.Equal([3.0, 5.0], series.Ends);
    }
}
