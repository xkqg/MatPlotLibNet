// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class HistogramSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[] data = [1.0, 2.0, 3.0, 4.0];
        var series = new HistogramSeries(data);
        Assert.Equal(data, series.Data);
    }

    [Fact]
    public void DefaultBins_Is10()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Equal(10, series.Bins);
    }

    [Fact]
    public void DefaultAlpha_Is0Point7()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Equal(0.7, series.Alpha);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Null(series.Color);
    }

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
