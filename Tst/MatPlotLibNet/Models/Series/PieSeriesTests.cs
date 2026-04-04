// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class PieSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[] sizes = [30.0, 70.0];
        var series = new PieSeries(sizes);
        Assert.Equal(sizes, series.Sizes);
    }

    [Fact]
    public void DefaultStartAngle_Is90()
    {
        var series = new PieSeries([1.0]);
        Assert.Equal(90, series.StartAngle);
    }

    [Fact]
    public void DefaultLabels_IsNull()
    {
        var series = new PieSeries([1.0]);
        Assert.Null(series.Labels);
    }

    [Fact]
    public void DefaultCounterClockwise_IsFalse()
    {
        var series = new PieSeries([1.0]);
        Assert.False(series.CounterClockwise);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new PieSeries([1.0]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(PieSeries), visitor.LastVisited);
    }
}
