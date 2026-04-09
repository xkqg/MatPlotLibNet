// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Atr"/> behavior.</summary>
public class AtrTests
{
    private static readonly double[] High = [48.70, 48.72, 48.90, 48.87, 48.82, 49.05, 49.20, 49.35, 49.92, 50.19, 50.12, 49.66, 49.88, 50.19, 50.36];
    private static readonly double[] Low = [47.79, 48.14, 48.39, 48.37, 48.24, 48.64, 48.94, 48.86, 49.50, 49.87, 49.20, 48.90, 49.43, 49.73, 49.26];
    private static readonly double[] Close = [48.16, 48.61, 48.75, 48.63, 48.74, 49.03, 49.07, 49.32, 49.91, 50.13, 49.53, 49.50, 49.75, 50.03, 50.31];

    /// <summary>Verifies that Compute returns an array whose length equals the input length minus the period.</summary>
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] atr = new Atr(High, Low, Close, 14).Compute();
        Assert.Equal(Close.Length - 14, atr.Length);
    }

    /// <summary>Verifies that all ATR values are positive.</summary>
    [Fact]
    public void Compute_ValuesArePositive()
    {
        double[] atr = new Atr(High, Low, Close, 5).Compute();
        foreach (var v in atr) Assert.True(v > 0);
    }

    /// <summary>Verifies that Apply adds a LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new Atr(High, Low, Close, 5).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }
}

/// <summary>Verifies <see cref="Adx"/> behavior.</summary>
public class AdxTests
{
    private static readonly double[] High = [30.20, 30.28, 30.45, 29.35, 29.35, 29.29, 28.83, 28.73, 28.67, 28.85, 28.64, 27.68, 27.21, 26.87, 27.41, 26.94, 26.52, 26.52, 27.09, 27.69];
    private static readonly double[] Low = [29.41, 29.32, 29.96, 28.74, 28.56, 28.41, 28.08, 27.43, 27.66, 27.83, 27.40, 27.09, 26.18, 26.13, 26.63, 26.13, 25.43, 25.35, 26.18, 26.58];
    private static readonly double[] Close = [29.87, 30.24, 30.10, 28.90, 28.92, 28.48, 28.56, 27.56, 28.47, 28.28, 27.49, 27.23, 26.35, 26.33, 27.03, 26.22, 26.01, 25.46, 27.03, 27.45];

    /// <summary>Verifies that Compute returns a non-empty result array.</summary>
    [Fact]
    public void Compute_ReturnsNonEmpty()
    {
        double[] adx = new Adx(High, Low, Close, 5).Compute();
        Assert.True(adx.Length > 0);
    }

    /// <summary>Verifies that all ADX values fall within the 0-100 range.</summary>
    [Fact]
    public void Compute_ValuesInRange()
    {
        double[] adx = new Adx(High, Low, Close, 5).Compute();
        foreach (var v in adx) Assert.InRange(v, 0, 100);
    }

    /// <summary>Verifies that Apply adds at least one LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new Adx(High, Low, Close, 5).Apply(axes);
        Assert.True(axes.Series.Count >= 1);
    }
}

/// <summary>Verifies <see cref="KeltnerChannels"/> behavior.</summary>
public class KeltnerChannelsTests
{
    private static readonly double[] High = [15, 16, 17, 18, 19, 20, 21, 22, 23, 24];
    private static readonly double[] Low = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17];
    private static readonly double[] Close = [13, 14, 15, 16, 17, 18, 19, 20, 21, 22];

    /// <summary>Verifies that Apply adds the channel fill and middle EMA series to the axes.</summary>
    [Fact]
    public void Apply_AddsThreeSeries()
    {
        var axes = new Axes();
        new KeltnerChannels(High, Low, Close, 5).Apply(axes);
        // AreaSeries (channel fill) + LineSeries (middle EMA) = 2
        Assert.Equal(2, axes.Series.Count);
    }
}

/// <summary>Verifies <see cref="Ichimoku"/> behavior.</summary>
public class IchimokuTests
{
    private static readonly double[] High = Enumerable.Range(1, 60).Select(i => (double)(50 + i)).ToArray();
    private static readonly double[] Low = Enumerable.Range(1, 60).Select(i => (double)(40 + i)).ToArray();
    private static readonly double[] Close = Enumerable.Range(1, 60).Select(i => (double)(45 + i)).ToArray();

    /// <summary>Verifies that Apply adds Tenkan, Kijun, and cloud fill series to the axes.</summary>
    [Fact]
    public void Apply_AddsSeriesToAxes()
    {
        var axes = new Axes();
        new Ichimoku(High, Low, Close).Apply(axes);
        Assert.True(axes.Series.Count >= 3); // Tenkan, Kijun, Cloud fill at minimum
    }
}
