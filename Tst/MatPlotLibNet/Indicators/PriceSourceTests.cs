// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Interaction;
using MatPlotLibNet;

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

    /// <summary>Verifies that Resolve with HLC3 returns the average of high, low, and close, and with OHLC4 the average of open, high, low, and close.</summary>
    [Theory]
    [InlineData(PriceSource.HLC3, 12.0)]  // (15+8+13)/3
    [InlineData(PriceSource.OHLC4, 11.5)] // (10+15+8+13)/4
    public void Resolve_AnAveragedSource_ReturnsAverage(PriceSource source, double expected)
    {
        var result = PriceSources.Resolve(source, Open, High, Low, Close);
        Assert.Equal(expected, result[0]);
    }

    /// <summary>PriceSources.Resolve — `_ =&gt; close` default arm.</summary>
    [Fact]
    public void PriceSources_Resolve_UnknownEnumValue_FallsBackToClose()
    {
        var open = new[] { 1.0 };
        var high = new[] { 2.0 };
        var low = new[] { 0.5 };
        var close = new[] { 1.5 };
        var result = PriceSources.Resolve((PriceSource)999, open, high, low, close);
        Assert.Same(close, result);
    }

    [Theory]
    [InlineData(PriceSource.Close, 4.0)]
    [InlineData(PriceSource.Open, 1.0)]
    [InlineData(PriceSource.High, 5.0)]
    [InlineData(PriceSource.Low, 0.5)]
    public void PriceSources_DirectMappingFour_ReturnsCorrespondingArray(PriceSource src, double expectedFirst)
    {
        var open  = new[] { 1.0 };
        var high  = new[] { 5.0 };
        var low   = new[] { 0.5 };
        var close = new[] { 4.0 };
        var result = PriceSources.Resolve(src, open, high, low, close);
        Assert.Equal(expectedFirst, result[0]);
    }

    [Theory]
    [InlineData(PriceSource.HL2, 0, 10, 4, 0, 7.0)]       // high and low
    [InlineData(PriceSource.HLC3, 0, 9, 3, 6, 6.0)]       // high, low and close
    [InlineData(PriceSource.OHLC4, 4, 8, 2, 6, 5.0)]      // all four
    public void PriceSources_AveragesItsComponents(PriceSource src, double open, double high, double low, double close, double expected)
    {
        var result = PriceSources.Resolve(src, [open], [high], [low], [close]);
        Assert.Equal(expected, result[0]);
    }
}
