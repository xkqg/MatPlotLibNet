// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="PriceSources"/> behavior.</summary>
public class PriceSourceTests
{
    private static readonly double[] Open = [10, 11, 12];
    private static readonly double[] High = [15, 16, 17];
    private static readonly double[] Low = [8, 9, 10];
    private static readonly double[] Close = [13, 14, 15];

    /// <summary>Verifies that Resolve with Close returns the close array unchanged.</summary>
    [Fact]
    public void Resolve_Close_ReturnsClose()
    {
        var result = PriceSources.Resolve(PriceSource.Close, Open, High, Low, Close);
        Assert.Equal(Close, result);
    }

    /// <summary>Verifies that Resolve with HL2 returns the average of high and low.</summary>
    [Fact]
    public void Resolve_HL2_ReturnsAverage()
    {
        var result = PriceSources.Resolve(PriceSource.HL2, Open, High, Low, Close);
        Assert.Equal(11.5, result[0]); // (15+8)/2
        Assert.Equal(12.5, result[1]);
    }

    /// <summary>Verifies that Resolve with HLC3 returns the average of high, low, and close.</summary>
    [Fact]
    public void Resolve_HLC3_ReturnsAverage()
    {
        var result = PriceSources.Resolve(PriceSource.HLC3, Open, High, Low, Close);
        Assert.Equal(12, result[0]); // (15+8+13)/3
    }

    /// <summary>Verifies that Resolve with OHLC4 returns the average of open, high, low, and close.</summary>
    [Fact]
    public void Resolve_OHLC4_ReturnsAverage()
    {
        var result = PriceSources.Resolve(PriceSource.OHLC4, Open, High, Low, Close);
        Assert.Equal(11.5, result[0]); // (10+15+8+13)/4
    }
}

/// <summary>Verifies indicator offset behavior.</summary>
public class IndicatorOffsetTests
{
    /// <summary>Verifies that setting Offset shifts the X coordinates of the resulting series.</summary>
    [Fact]
    public void Sma_WithOffset_ShiftsXCoordinates()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3) { Offset = 5 }.Apply(axes);
        var series = (LineSeries)axes.Series[0];
        // Normal X starts at period-1=2. With offset 5, should start at 7.
        Assert.Equal(7, series.XData[0]);
    }
}

/// <summary>Verifies indicator line style behavior.</summary>
public class IndicatorLineStyleTests
{
    /// <summary>Verifies that setting LineStyle applies the style to the resulting LineSeries.</summary>
    [Fact]
    public void Sma_WithLineStyle_AppliesStyle()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3) { LineStyle = LineStyle.Dashed }.Apply(axes);
        var series = (LineSeries)axes.Series[0];
        Assert.Equal(LineStyle.Dashed, series.LineStyle);
    }
}

/// <summary>Verifies <see cref="Sma"/> OHLC constructor behavior.</summary>
public class SmaOhlcConstructorTests
{
    /// <summary>Verifies that the OHLC constructor with Close source produces a series.</summary>
    [Fact]
    public void Sma_FromOhlc_UsesClose()
    {
        double[] o = [10, 11, 12, 13, 14];
        double[] h = [15, 16, 17, 18, 19];
        double[] l = [8, 9, 10, 11, 12];
        double[] c = [13, 14, 15, 16, 17];

        var axes = new Axes();
        new Sma(PriceSources.Resolve(PriceSource.Close, o, h, l, c), 3).Apply(axes);
        Assert.Single(axes.Series);
    }

    /// <summary>Verifies that the OHLC constructor with HL2 source produces different values than Close.</summary>
    [Fact]
    public void Sma_FromOhlc_UsesHL2()
    {
        double[] o = [10, 10, 10, 10, 10];
        double[] h = [20, 20, 20, 20, 20];
        double[] l = [0, 0, 0, 0, 0];
        double[] c = [15, 15, 15, 15, 15];

        double[] smaClose = new Sma(c, 3).Compute();
        double[] smaHL2 = new Sma(PriceSources.Resolve(PriceSource.HL2, o, h, l, c), 3).Compute();

        Assert.Equal(15, smaClose[0]); // avg of 15,15,15
        Assert.Equal(10, smaHL2[0]);   // avg of 10,10,10 (HL2 = (20+0)/2 = 10)
    }
}

/// <summary>Verifies generic typed Compute results for indicators.</summary>
public class GenericIndicatorTests
{
    /// <summary>Verifies that Sma.Compute returns a typed double array with correct values.</summary>
    [Fact]
    public void Sma_Compute_ReturnsTypedResult()
    {
        var sma = new Sma([10, 20, 30, 40, 50], 3);
        double[] result = sma.Compute();
        Assert.Equal(3, result.Length);
        Assert.Equal(20, result[0]);
    }

    /// <summary>Verifies that BollingerBands.Compute returns a typed tuple with middle, upper, and lower bands.</summary>
    [Fact]
    public void BollingerBands_Compute_ReturnsTypedTuple()
    {
        double[] prices = [10, 20, 30, 25, 35, 40, 30, 45, 50, 55];
        var bb = new BollingerBands(prices, 5);
        var (middle, upper, lower) = bb.Compute();
        Assert.Equal(middle.Length, upper.Length);
        Assert.True(upper[0] > middle[0]);
        Assert.True(lower[0] < middle[0]);
    }
}
