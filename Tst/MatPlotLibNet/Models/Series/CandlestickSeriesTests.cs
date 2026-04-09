// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="CandlestickSeries"/> default properties and construction.</summary>
public class CandlestickSeriesTests
{
    /// <summary>Verifies that the constructor stores OHLC data arrays.</summary>
    [Fact]
    public void Constructor_StoresOhlcData()
    {
        double[] o = [10, 12], h = [15, 14], l = [8, 10], c = [13, 11];
        var series = new CandlestickSeries(o, h, l, c);
        Assert.Equal(o, series.Open);
        Assert.Equal(h, series.High);
        Assert.Equal(l, series.Low);
        Assert.Equal(c, series.Close);
    }

    /// <summary>Verifies that UpColor defaults to Green.</summary>
    [Fact]
    public void DefaultUpColor_IsGreen()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Equal(Colors.Green, series.UpColor);
    }

    /// <summary>Verifies that DownColor defaults to Red.</summary>
    [Fact]
    public void DefaultDownColor_IsRed()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Equal(Colors.Red, series.DownColor);
    }

    /// <summary>Verifies that BodyWidth defaults to 0.6.</summary>
    [Fact]
    public void DefaultBodyWidth_Is0Point6()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Equal(0.6, series.BodyWidth);
    }

    /// <summary>Verifies that DateLabels defaults to null.</summary>
    [Fact]
    public void DateLabels_DefaultsToNull()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Null(series.DateLabels);
    }
}
