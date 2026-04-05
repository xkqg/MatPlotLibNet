// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

public class RsiTests
{
    private static readonly double[] Prices = [44, 44.34, 44.09, 43.61, 44.33, 44.83, 45.10, 45.42, 45.84,
        46.08, 45.89, 46.03, 45.61, 46.28, 46.28, 46.00, 46.03, 46.41, 46.22, 45.64];

    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] rsi = new Rsi(Prices, 14).Compute();
        Assert.Equal(Prices.Length - 14, rsi.Length);
    }

    [Fact]
    public void Compute_ValuesInRange()
    {
        double[] rsi = new Rsi(Prices, 14).Compute();
        foreach (var v in rsi)
            Assert.InRange(v, 0, 100);
    }

    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        new Rsi(Prices, 14).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsAxisLimits()
    {
        var axes = new Axes();
        new Rsi(Prices, 14).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(100, axes.YAxis.Max);
    }
}

public class MacdTests
{
    private static readonly double[] Prices = [26.0, 26.5, 26.3, 26.8, 27.0, 27.2, 27.5, 27.3, 27.8, 28.0,
        28.2, 28.5, 28.3, 28.8, 29.0, 28.7, 29.2, 29.5, 29.3, 29.8, 30.0, 30.2, 30.5, 30.3, 30.8, 31.0];

    [Fact]
    public void Apply_AddsThreeSeriesToAxes()
    {
        var axes = new Axes();
        new Macd(Prices).Apply(axes);
        Assert.Equal(3, axes.Series.Count); // MACD line + signal + histogram
    }

    [Fact]
    public void Apply_HistogramIsBarSeries()
    {
        var axes = new Axes();
        new Macd(Prices).Apply(axes);
        Assert.IsType<BarSeries>(axes.Series[2]);
    }
}

public class VolumeIndicatorTests
{
    [Fact]
    public void Apply_AddsBarSeriesToAxes()
    {
        var axes = new Axes();
        new VolumeIndicator([1000, 1500, 1200, 1800]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<BarSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYMinToZero()
    {
        var axes = new Axes();
        new VolumeIndicator([1000, 1500]).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
    }
}

public class StochasticTests
{
    private static readonly double[] High = [128, 127, 126, 128, 130, 129, 131, 132, 130, 131, 132, 133, 134, 133, 135];
    private static readonly double[] Low = [125, 124, 123, 125, 127, 126, 128, 129, 127, 128, 129, 130, 131, 130, 132];
    private static readonly double[] Close = [127, 126, 125, 127, 129, 128, 130, 131, 129, 130, 131, 132, 133, 132, 134];

    [Fact]
    public void Apply_AddsTwoLineSeriesToAxes()
    {
        var axes = new Axes();
        new Stochastic(High, Low, Close, 14, 3).Apply(axes);
        Assert.Equal(2, axes.Series.Count); // %K + %D
    }

    [Fact]
    public void Apply_SetsAxisLimits()
    {
        var axes = new Axes();
        new Stochastic(High, Low, Close, 14, 3).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(100, axes.YAxis.Max);
    }

    [Fact]
    public void Compute_ValuesInRange()
    {
        var result = new Stochastic(High, Low, Close, 14, 3).Compute();
        var k = result.K;
        foreach (var v in k)
            Assert.InRange(v, 0, 100);
    }
}
