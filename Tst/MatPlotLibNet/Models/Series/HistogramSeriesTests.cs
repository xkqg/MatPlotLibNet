// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="HistogramSeries"/> default properties and construction.</summary>
public class HistogramSeriesTests
{
    /// <summary>Verifies that the constructor stores the data array.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] data = [1.0, 2.0, 3.0, 4.0];
        var series = new HistogramSeries(data);
        Assert.Equal(data, series.Data);
    }

    /// <summary>Verifies that Bins defaults to 10.</summary>
    [Fact]
    public void DefaultBins_Is10()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Equal(10, series.Bins);
    }

    /// <summary>Verifies that Alpha defaults to 0.7.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point7()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Equal(0.7, series.Alpha);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that ComputeBins returns valid counts, min, and bin width.</summary>
    [Fact]
    public void ComputeBins_ReturnsValidResult()
    {
        var series = new HistogramSeries([1.0, 2.0, 3.0, 4.0, 5.0]) { Bins = 5 };
        var bins = series.ComputeBins();
        Assert.Equal(5, bins.Counts.Length);
        Assert.Equal(1.0, bins.Min);
        Assert.True(bins.BinWidth > 0);
    }
}
