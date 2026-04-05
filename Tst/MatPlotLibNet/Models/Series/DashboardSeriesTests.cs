// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

public class GaugeSeriesTests
{
    [Fact]
    public void Constructor_StoresValue()
    {
        var series = new GaugeSeries(75);
        Assert.Equal(75, series.Value);
    }

    [Fact]
    public void DefaultMinMax()
    {
        var series = new GaugeSeries(50);
        Assert.Equal(0, series.Min);
        Assert.Equal(100, series.Max);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new GaugeSeries(50);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(GaugeSeries), visitor.LastVisited);
    }
}

public class ProgressBarSeriesTests
{
    [Fact]
    public void Constructor_StoresValue()
    {
        var series = new ProgressBarSeries(0.65);
        Assert.Equal(0.65, series.Value);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new ProgressBarSeries(0.5);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(ProgressBarSeries), visitor.LastVisited);
    }
}

public class SparklineSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new SparklineSeries([1, 3, 2, 4, 3, 5]);
        Assert.Equal([1.0, 3, 2, 4, 3, 5], series.Values);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new SparklineSeries([1, 2, 3]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(SparklineSeries), visitor.LastVisited);
    }
}
