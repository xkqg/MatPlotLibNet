// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="GaugeSeries"/> default properties and construction.</summary>
public class GaugeSeriesTests
{
    /// <summary>Verifies that the constructor stores the gauge value.</summary>
    [Fact]
    public void Constructor_StoresValue()
    {
        var series = new GaugeSeries(75);
        Assert.Equal(75, series.Value);
    }

    /// <summary>Verifies that Min defaults to 0 and Max defaults to 100.</summary>
    [Fact]
    public void DefaultMinMax()
    {
        var series = new GaugeSeries(50);
        Assert.Equal(0, series.Min);
        Assert.Equal(100, series.Max);
    }
}

/// <summary>Verifies <see cref="ProgressBarSeries"/> default properties and construction.</summary>
public class ProgressBarSeriesTests
{
    /// <summary>Verifies that the constructor stores the progress value.</summary>
    [Fact]
    public void Constructor_StoresValue()
    {
        var series = new ProgressBarSeries(0.65);
        Assert.Equal(0.65, series.Value);
    }
}

/// <summary>Verifies <see cref="SparklineSeries"/> default properties and construction.</summary>
public class SparklineSeriesTests
{
    /// <summary>Verifies that the constructor stores the values array.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new SparklineSeries([1, 3, 2, 4, 3, 5]);
        Assert.Equal([1.0, 3, 2, 4, 3, 5], series.Values);
    }
}
