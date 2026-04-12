// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Rsi"/> behavior.</summary>
public class RsiTests
{
    private static readonly double[] Prices = [44, 44.34, 44.09, 43.61, 44.33, 44.83, 45.10, 45.42, 45.84,
        46.08, 45.89, 46.03, 45.61, 46.28, 46.28, 46.00, 46.03, 46.41, 46.22, 45.64];

    /// <summary>Verifies that Compute returns an array whose length equals input length minus the period.</summary>
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] rsi = new Rsi(Prices, 14).Compute();
        Assert.Equal(Prices.Length - 14, rsi.Length);
    }

    /// <summary>Verifies that all RSI values fall within the 0-100 range.</summary>
    [Fact]
    public void Compute_ValuesInRange()
    {
        double[] rsi = new Rsi(Prices, 14).Compute();
        foreach (var v in rsi)
            Assert.InRange(v, 0, 100);
    }

    /// <summary>Verifies that Apply adds a LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        new Rsi(Prices, 14).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    /// <summary>Verifies that Apply sets Y-axis limits to the 0-100 range.</summary>
    [Fact]
    public void Apply_SetsAxisLimits()
    {
        var axes = new Axes();
        new Rsi(Prices, 14).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(100, axes.YAxis.Max);
    }
}

/// <summary>Verifies <see cref="Macd"/> behavior.</summary>
public class MacdTests
{
    private static readonly double[] Prices = [26.0, 26.5, 26.3, 26.8, 27.0, 27.2, 27.5, 27.3, 27.8, 28.0,
        28.2, 28.5, 28.3, 28.8, 29.0, 28.7, 29.2, 29.5, 29.3, 29.8, 30.0, 30.2, 30.5, 30.3, 30.8, 31.0];

    /// <summary>Verifies that Apply adds MACD line, signal line, and histogram series to the axes.</summary>
    [Fact]
    public void Apply_AddsThreeSeriesToAxes()
    {
        var axes = new Axes();
        new Macd(Prices).Apply(axes);
        Assert.Equal(3, axes.Series.Count); // MACD line + signal + histogram
    }

    /// <summary>Verifies that the histogram series is rendered as a BarSeries.</summary>
    [Fact]
    public void Apply_HistogramIsBarSeries()
    {
        var axes = new Axes();
        new Macd(Prices).Apply(axes);
        Assert.IsType<BarSeries>(axes.Series[2]);
    }
}

/// <summary>Verifies <see cref="VolumeIndicator"/> behavior.</summary>
public class VolumeIndicatorTests
{
    /// <summary>Verifies that Apply adds a BarSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsBarSeriesToAxes()
    {
        var axes = new Axes();
        new VolumeIndicator([1000, 1500, 1200, 1800]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<BarSeries>(axes.Series[0]);
    }

    /// <summary>Verifies that Apply sets the Y-axis minimum to zero.</summary>
    [Fact]
    public void Apply_SetsYMinToZero()
    {
        var axes = new Axes();
        new VolumeIndicator([1000, 1500]).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
    }
}

/// <summary>Verifies <see cref="Stochastic"/> behavior.</summary>
public class StochasticTests
{
    private static readonly double[] High = [128, 127, 126, 128, 130, 129, 131, 132, 130, 131, 132, 133, 134, 133, 135];
    private static readonly double[] Low = [125, 124, 123, 125, 127, 126, 128, 129, 127, 128, 129, 130, 131, 130, 132];
    private static readonly double[] Close = [127, 126, 125, 127, 129, 128, 130, 131, 129, 130, 131, 132, 133, 132, 134];

    /// <summary>Verifies that Apply adds %K and %D line series to the axes.</summary>
    [Fact]
    public void Apply_AddsTwoLineSeriesToAxes()
    {
        var axes = new Axes();
        new Stochastic(High, Low, Close, 14, 3).Apply(axes);
        Assert.Equal(2, axes.Series.Count); // %K + %D
    }

    /// <summary>Verifies that Apply sets Y-axis limits to the 0-100 range.</summary>
    [Fact]
    public void Apply_SetsAxisLimits()
    {
        var axes = new Axes();
        new Stochastic(High, Low, Close, 14, 3).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(100, axes.YAxis.Max);
    }

    /// <summary>Verifies that all %K values fall within the 0-100 range.</summary>
    [Fact]
    public void Compute_ValuesInRange()
    {
        var result = new Stochastic(High, Low, Close, 14, 3).Compute();
        var k = result.K;
        foreach (var v in k)
            Assert.InRange(v, 0, 100);
    }
}
