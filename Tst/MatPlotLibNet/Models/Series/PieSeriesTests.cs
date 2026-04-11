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

    /// <summary>Verifies that Explode defaults to null.</summary>
    [Fact]
    public void DefaultExplode_IsNull()
    {
        var series = new PieSeries([1.0]);
        Assert.Null(series.Explode);
    }

    /// <summary>Verifies that AutoPct defaults to null.</summary>
    [Fact]
    public void DefaultAutoPct_IsNull()
    {
        var series = new PieSeries([1.0]);
        Assert.Null(series.AutoPct);
    }

    /// <summary>Verifies that Shadow defaults to false.</summary>
    [Fact]
    public void DefaultShadow_IsFalse()
    {
        var series = new PieSeries([1.0]);
        Assert.False(series.Shadow);
    }

    /// <summary>Verifies that Radius defaults to null.</summary>
    [Fact]
    public void DefaultRadius_IsNull()
    {
        var series = new PieSeries([1.0]);
        Assert.Null(series.Radius);
    }

    /// <summary>Verifies that Explode can be set.</summary>
    [Fact]
    public void Explode_CanBeSet()
    {
        var series = new PieSeries([30.0, 70.0]) { Explode = [0.1, 0.0] };
        Assert.Equal([0.1, 0.0], series.Explode);
    }

    /// <summary>Verifies that AutoPct can be set.</summary>
    [Fact]
    public void AutoPct_CanBeSet()
    {
        var series = new PieSeries([1.0]) { AutoPct = "{0:F1}%" };
        Assert.Equal("{0:F1}%", series.AutoPct);
    }
}
