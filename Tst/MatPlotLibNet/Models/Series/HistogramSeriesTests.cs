// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

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

    /// <summary>Verifies that Density defaults to false.</summary>
    [Fact]
    public void DefaultDensity_IsFalse()
    {
        var series = new HistogramSeries([1.0]);
        Assert.False(series.Density);
    }

    /// <summary>Verifies that Cumulative defaults to false.</summary>
    [Fact]
    public void DefaultCumulative_IsFalse()
    {
        var series = new HistogramSeries([1.0]);
        Assert.False(series.Cumulative);
    }

    /// <summary>Verifies that HistType defaults to Bar.</summary>
    [Fact]
    public void DefaultHistType_IsBar()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Equal(HistType.Bar, series.HistType);
    }

    /// <summary>Verifies that Weights defaults to null.</summary>
    [Fact]
    public void DefaultWeights_IsNull()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Null(series.Weights);
    }

    /// <summary>Verifies that RWidth defaults to 1.0.</summary>
    [Fact]
    public void DefaultRWidth_Is1()
    {
        var series = new HistogramSeries([1.0]);
        Assert.Equal(1.0, series.RWidth);
    }

    /// <summary>Verifies that HistType can be set.</summary>
    [Fact]
    public void HistType_CanBeSet()
    {
        var series = new HistogramSeries([1.0]);
        series.HistType = HistType.Step;
        Assert.Equal(HistType.Step, series.HistType);
    }

    /// <summary>Verifies that Density can be set.</summary>
    [Fact]
    public void Density_CanBeSet()
    {
        var series = new HistogramSeries([1.0]);
        series.Density = true;
        Assert.True(series.Density);
    }

    /// <summary>Verifies that Cumulative can be set.</summary>
    [Fact]
    public void Cumulative_CanBeSet()
    {
        var series = new HistogramSeries([1.0]);
        series.Cumulative = true;
        Assert.True(series.Cumulative);
    }

    /// <summary>Verifies that Weights can be set.</summary>
    [Fact]
    public void Weights_CanBeSet()
    {
        var series = new HistogramSeries([1.0, 2.0]);
        series.Weights = [0.5, 0.5];
        Assert.NotNull(series.Weights);
        Assert.Equal(2, series.Weights.Length);
    }

    /// <summary>Verifies that RWidth can be set.</summary>
    [Fact]
    public void RWidth_CanBeSet()
    {
        var series = new HistogramSeries([1.0]);
        series.RWidth = 0.8;
        Assert.Equal(0.8, series.RWidth);
    }
}
