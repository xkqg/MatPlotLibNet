// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PieSeries"/> default properties and construction.</summary>
public class PieSeriesTests
{
    /// <summary>Verifies that the constructor stores sizes.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] sizes = [30.0, 70.0];
        var series = new PieSeries(sizes);
        Assert.Equal(sizes, series.Sizes);
    }

    /// <summary>Verifies that StartAngle defaults to 90.</summary>
    [Fact]
    public void DefaultStartAngle_Is90()
    {
        var series = new PieSeries([1.0]);
        Assert.Equal(90, series.StartAngle);
    }

    /// <summary>Verifies that Labels defaults to null.</summary>
    [Fact]
    public void DefaultLabels_IsNull()
    {
        var series = new PieSeries([1.0]);
        Assert.Null(series.Labels);
    }

    /// <summary>Verifies that CounterClockwise defaults to false.</summary>
    [Fact]
    public void DefaultCounterClockwise_IsFalse()
    {
        var series = new PieSeries([1.0]);
        Assert.False(series.CounterClockwise);
    }
}
