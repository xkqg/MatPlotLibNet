// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="WilliamsR"/> behavior.</summary>
public class WilliamsRTests
{
    private static readonly double[] High =
        [18, 17, 18, 19, 20, 19, 21, 22, 21, 22, 23, 22, 23, 24, 23, 24, 25, 24, 25, 26];
    private static readonly double[] Low =
        [10, 11, 10, 11, 12, 11, 13, 14, 13, 14, 15, 14, 15, 16, 15, 16, 17, 16, 17, 18];
    private static readonly double[] Close =
        [15, 14, 15, 16, 17, 16, 18, 19, 18, 19, 20, 19, 20, 21, 20, 21, 22, 21, 22, 23];

    /// <summary>Output length equals input length minus period plus one.</summary>
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] result = new WilliamsR(High, Low, Close, 14).Compute();
        Assert.Equal(High.Length - 14 + 1, result.Length);
    }

    /// <summary>All values fall within [-100, 0].</summary>
    [Fact]
    public void Compute_ValuesInRange()
    {
        double[] result = new WilliamsR(High, Low, Close, 14).Compute();
        foreach (var v in result)
            Assert.InRange(v, -100.0, 0.0);
    }

    /// <summary>Apply adds a LineSeries and sets Y-axis limits.</summary>
    [Fact]
    public void Apply_AddsLineSeriesAndSetsYAxis()
    {
        var axes = new Axes();
        new WilliamsR(High, Low, Close, 14).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
        Assert.Equal(-100, axes.YAxis.Min);
        Assert.Equal(0, axes.YAxis.Max);
    }
}

/// <summary>Verifies <see cref="Obv"/> behavior.</summary>
public class ObvTests
{
    private static readonly double[] Close = [100, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] Volume = [1000, 1200, 800, 1500, 2000, 900, 1800, 2200, 700, 1600];

    /// <summary>Output length equals input length.</summary>
    [Fact]
    public void Compute_ReturnsFullLength()
    {
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(Close.Length, result.Length);
    }

    /// <summary>OBV starts at first volume value.</summary>
    [Fact]
    public void Compute_StartsWithFirstVolume()
    {
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(Volume[0], result[0]);
    }

    /// <summary>When close rises, volume is added.</summary>
    [Fact]
    public void Compute_AddsVolumeOnUp()
    {
        // Close[1]=102 > Close[0]=100 → OBV[1] = OBV[0] + Volume[1]
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(result[0] + Volume[1], result[1]);
    }

    /// <summary>When close falls, volume is subtracted.</summary>
    [Fact]
    public void Compute_SubtractsVolumeOnDown()
    {
        // Close[2]=101 < Close[1]=102 → OBV[2] = OBV[1] - Volume[2]
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(result[1] - Volume[2], result[2]);
    }

    /// <summary>Apply adds a single LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new Obv(Close, Volume).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }
}

/// <summary>Verifies <see cref="Cci"/> behavior.</summary>
public class CciTests
{
    private static readonly double[] High =
        [18, 17, 18, 19, 20, 19, 21, 22, 21, 22, 23, 22, 23, 24, 23, 24, 25, 24, 25, 26];
    private static readonly double[] Low =
        [10, 11, 10, 11, 12, 11, 13, 14, 13, 14, 15, 14, 15, 16, 15, 16, 17, 16, 17, 18];
    private static readonly double[] Close =
        [15, 14, 15, 16, 17, 16, 18, 19, 18, 19, 20, 19, 20, 21, 20, 21, 22, 21, 22, 23];

    /// <summary>Output length equals input length minus period plus one.</summary>
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] result = new Cci(High, Low, Close, 14).Compute();
        Assert.Equal(High.Length - 14 + 1, result.Length);
    }

    /// <summary>Apply adds a LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new Cci(High, Low, Close, 14).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }
}

/// <summary>Verifies <see cref="ParabolicSar"/> behavior.</summary>
public class ParabolicSarTests
{
    private static readonly double[] High = [48.70, 48.72, 48.90, 48.87, 48.82, 49.05, 49.20, 49.35, 49.92, 50.19,
        50.12, 49.66, 49.88, 50.19, 50.36, 50.57, 50.65, 50.43, 49.63, 48.66];
    private static readonly double[] Low = [47.79, 48.14, 48.39, 48.37, 48.24, 48.64, 48.94, 48.86, 49.50, 49.87,
        49.20, 48.90, 49.43, 49.73, 49.26, 50.09, 50.30, 49.21, 48.98, 48.22];

    /// <summary>SAR array length equals input length.</summary>
    [Fact]
    public void Compute_SarLengthEqualsInput()
    {
        var result = new ParabolicSar(High, Low).Compute();
        Assert.Equal(High.Length, result.Sar.Length);
        Assert.Equal(High.Length, result.IsLong.Length);
    }

    /// <summary>Apply adds exactly two scatter series (long dots and short dots).</summary>
    [Fact]
    public void Apply_AddsTwoScatterSeries()
    {
        var axes = new Axes();
        new ParabolicSar(High, Low).Apply(axes);
        Assert.Equal(2, axes.Series.Count);
        Assert.IsType<ScatterSeries>(axes.Series[0]);
        Assert.IsType<ScatterSeries>(axes.Series[1]);
    }
}
