// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

public class EquityCurveTests
{
    [Fact]
    public void Compute_CumulativeSum()
    {
        var equity = EquityCurve.Compute([100, -30, 50, -10], 10000);
        Assert.Equal(10000, equity[0]);
        Assert.Equal(10100, equity[1]);
        Assert.Equal(10070, equity[2]);
        Assert.Equal(10120, equity[3]);
        Assert.Equal(10110, equity[4]);
    }

    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        new EquityCurve([100, -30, 50]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsLabel()
    {
        var axes = new Axes();
        new EquityCurve([100, -30, 50]).Apply(axes);
        Assert.Equal("Equity", axes.Series[0].Label);
    }
}

public class ProfitLossTests
{
    [Fact]
    public void Apply_AddsSeriesToAxes()
    {
        var axes = new Axes();
        new ProfitLoss([100, -50, 75, -20]).Apply(axes);
        Assert.True(axes.Series.Count > 0);
    }

    [Fact]
    public void Apply_SetsYMinToIncludeNegatives()
    {
        var axes = new Axes();
        new ProfitLoss([100, -50, 75, -20]).Apply(axes);
        // Y-axis should accommodate negative values
        Assert.NotNull(axes.YAxis.Min);
    }
}

public class DrawDownTests
{
    [Fact]
    public void Compute_ReturnsCorrectDrawdown()
    {
        var dd = DrawDown.Compute([100, 110, 105, 120, 100]);
        Assert.Equal(0, dd[0]);     // first point = no drawdown
        Assert.Equal(0, dd[1]);     // new high
        Assert.InRange(dd[2], 4, 5); // ~4.5% drawdown from 110
        Assert.Equal(0, dd[3]);     // new high at 120
        Assert.InRange(dd[4], 16, 17); // ~16.7% drawdown from 120
    }

    [Fact]
    public void Compute_AllValuesNonNegative()
    {
        var dd = DrawDown.Compute([100, 90, 80, 95, 70, 110]);
        foreach (var v in dd)
            Assert.True(v >= 0, $"Drawdown should be non-negative, got {v}");
    }

    [Fact]
    public void Apply_AddsAreaSeriesToAxes()
    {
        var axes = new Axes();
        new DrawDown([100, 110, 105, 120, 100]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<AreaSeries>(axes.Series[0]);
    }
}
