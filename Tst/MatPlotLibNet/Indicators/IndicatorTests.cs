// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Sma"/> behavior.</summary>
public class SmaTests
{
    /// <summary>Verifies that Compute returns an array whose length equals input length minus period plus one.</summary>
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Sma(prices, 3).Compute();
        Assert.Equal(3, result.Length); // 5 - 3 + 1
    }

    /// <summary>Verifies that Compute returns correct simple moving average values.</summary>
    [Fact]
    public void Compute_ReturnsCorrectValues()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Sma(prices, 3).Compute();
        Assert.Equal(20, result[0]); // (10+20+30)/3
        Assert.Equal(30, result[1]); // (20+30+40)/3
        Assert.Equal(40, result[2]); // (30+40+50)/3
    }

    /// <summary>Verifies that Apply adds a LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        var indicator = new Sma([10, 20, 30, 40, 50], 3);
        indicator.Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    /// <summary>Verifies that Apply sets the series label to the expected SMA format.</summary>
    [Fact]
    public void Apply_SetsLabel()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3).Apply(axes);
        Assert.Equal("SMA(3)", axes.Series[0].Label);
    }

    /// <summary>Verifies that a custom color is applied to the resulting LineSeries.</summary>
    [Fact]
    public void Apply_RespectsCustomColor()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3) { Color = Colors.Red }.Apply(axes);
        Assert.Equal(Colors.Red, ((LineSeries)axes.Series[0]).Color);
    }

    /// <summary>Verifies that Sma implements the IIndicator interface.</summary>
    [Fact]
    public void ImplementsIIndicator()
    {
        Assert.IsAssignableFrom<IIndicator>(new Sma([1.0], 1));
    }

    /// <summary>Verifies that Sma extends the Indicator base class.</summary>
    [Fact]
    public void ExtendsIndicatorBase()
    {
        Assert.IsAssignableFrom<Indicator>(new Sma([1.0], 1));
    }
}

/// <summary>Verifies <see cref="Ema"/> behavior.</summary>
public class EmaTests
{
    /// <summary>Verifies that Compute returns an array matching the input length.</summary>
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Ema(prices, 3).Compute();
        Assert.Equal(5, result.Length);
    }

    /// <summary>Verifies that the first EMA value equals the SMA seed value.</summary>
    [Fact]
    public void Compute_FirstValueIsSma()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Ema(prices, 3).Compute();
        Assert.Equal(20, result[2]); // SMA of first 3: (10+20+30)/3
    }

    /// <summary>Verifies that Apply adds a LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        new Ema([10, 20, 30, 40, 50], 3).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    /// <summary>Covers the early-return branch when the input price array is shorter than the period.</summary>
    [Fact]
    public void Compute_TooFewPrices_ReturnsEmpty()
    {
        // 2 prices, period 5 → returns empty
        Assert.Empty(new Ema([10, 20], 5).Compute().Values);
    }
}

/// <summary>Verifies <see cref="BollingerBands"/> behavior.</summary>
public class BollingerBandsTests
{
    /// <summary>Verifies that Apply adds the band fill and middle SMA series to the axes.</summary>
    [Fact]
    public void Apply_AddsThreeSeriesToAxes()
    {
        var axes = new Axes();
        double[] prices = [10, 20, 30, 25, 35, 40, 30, 45, 50, 55];
        new BollingerBands(prices, 5).Apply(axes);
        // AreaSeries (band fill) + LineSeries (middle SMA)
        Assert.Equal(2, axes.Series.Count);
    }

    /// <summary>Verifies that Apply sets the expected label containing the period.</summary>
    [Fact]
    public void Apply_SetsLabel()
    {
        var axes = new Axes();
        double[] prices = [10, 20, 30, 25, 35, 40, 30, 45, 50, 55];
        new BollingerBands(prices, 5).Apply(axes);
        Assert.Contains("BB(5", axes.Series[1].Label!);
    }
}

/// <summary>Verifies <see cref="Vwap"/> behavior.</summary>
public class VwapTests
{
    /// <summary>Verifies that Compute returns an array matching the input length.</summary>
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] prices = [100, 102, 101, 103];
        double[] volumes = [1000, 1500, 1200, 1800];
        double[] result = new Vwap(prices, volumes).Compute();
        Assert.Equal(4, result.Length);
    }

    /// <summary>Verifies that Apply adds a LineSeries to the axes.</summary>
    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        new Vwap([100, 102, 101], [1000, 1500, 1200]).Apply(axes);
        Assert.Single(axes.Series);
    }
}

/// <summary>Verifies <see cref="FibonacciRetracement"/> behavior.</summary>
public class FibonacciRetracementTests
{
    /// <summary>Verifies that Apply adds five Fibonacci retracement reference lines.</summary>
    [Fact]
    public void Apply_AddsFiveReferenceLines()
    {
        var axes = new Axes();
        axes.Plot([0, 1, 2], [100, 200, 150]); // need a series for axes to render
        new FibonacciRetracement(100, 200).Apply(axes);
        Assert.Equal(5, axes.ReferenceLines.Count); // 23.6%, 38.2%, 50%, 61.8%, 78.6%
    }

    /// <summary>Verifies that all retracement lines fall within the high-low price range.</summary>
    [Fact]
    public void Apply_LinesAreWithinRange()
    {
        var axes = new Axes();
        axes.Plot([0, 1], [100, 200]);
        new FibonacciRetracement(100, 200).Apply(axes);
        foreach (var line in axes.ReferenceLines)
        {
            Assert.InRange(line.Value, 100, 200);
        }
    }

    /// <summary>Covers <see cref="FibonacciRetracement.Compute"/> — returns the five Fibonacci price levels.</summary>
    [Fact]
    public void Compute_ReturnsFiveLevels()
    {
        var result = new FibonacciRetracement(100, 200).Compute();
        double[] arr = result;
        Assert.Equal(5, arr.Length);
        // Levels are at high - range * level, so:
        Assert.Equal(200 - 100 * 0.236, arr[0], 1e-9);
        Assert.Equal(200 - 100 * 0.382, arr[1], 1e-9);
        Assert.Equal(200 - 100 * 0.5,   arr[2], 1e-9);
        Assert.Equal(200 - 100 * 0.618, arr[3], 1e-9);
        Assert.Equal(200 - 100 * 0.786, arr[4], 1e-9);
    }

    /// <summary>Covers the explicit-color branch in <see cref="FibonacciRetracement.Apply"/>.</summary>
    [Fact]
    public void Apply_CustomColor_AppliedToLines()
    {
        var axes = new Axes();
        axes.Plot([0, 1], [100, 200]);
        new FibonacciRetracement(100, 200) { Color = MatPlotLibNet.Styling.Colors.Cyan }.Apply(axes);
        foreach (var line in axes.ReferenceLines)
        {
            Assert.Equal(MatPlotLibNet.Styling.Colors.Cyan, line.Color);
        }
    }
}

/// <summary>Verifies the AddIndicator fluent API on the figure builder.</summary>
public class AddIndicatorBuilderTests
{
    /// <summary>Verifies that AddIndicator adds the indicator series alongside the existing plot series.</summary>
    [Fact]
    public void AddIndicator_FluentChaining()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0, 4.0, 5.0], [10, 20, 30, 40, 50])
                .AddIndicator(new Sma([10, 20, 30, 40, 50], 3)))
            .Build();

        Assert.Equal(2, figure.SubPlots[0].Series.Count); // original + SMA
    }
}

/// <summary>Verifies that chaining BB then SMA on the same candlestick panel uses the original price data.</summary>
public class GetPriceDataChainTests
{
    private static readonly double[] Open  = [100, 102, 101, 103, 104, 103, 105, 107, 106, 108,
                                               109, 107, 108, 110, 109, 111, 112, 110, 111, 113,
                                               100, 102, 101, 103, 104, 103, 105, 107, 106, 108];
    private static readonly double[] High  = [103, 104, 103, 105, 106, 105, 108, 109, 108, 110,
                                               111, 109, 110, 112, 111, 113, 114, 112, 113, 115,
                                               103, 104, 103, 105, 106, 105, 108, 109, 108, 110];
    private static readonly double[] Low   = [ 98,  99,  98, 100, 101, 100, 102, 104, 103, 105,
                                               106, 104, 105, 107, 106, 108, 109, 107, 108, 110,
                                                98,  99,  98, 100, 101, 100, 102, 104, 103, 105];
    private static readonly double[] Close = [101, 102, 101, 103, 105, 104, 106, 107, 106, 108,
                                               107, 108, 110, 109, 111, 112, 110, 111, 113, 114,
                                               101, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] Vol   = Enumerable.Repeat(1000.0, 30).ToArray();

    /// <summary>BollingerBands followed by Sma on a candlestick panel must not throw.</summary>
    [Fact]
    public void BollingerBands_ThenSma_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            FigureTemplates.FinancialDashboard(Open, High, Low, Close, Vol,
                configurePricePanel: ax =>
                {
                    ax.BollingerBands(20);
                    ax.Sma(5); // must resolve close prices from CandlestickSeries, not BB output
                })
                .ToSvg());
        Assert.Null(ex);
    }

    /// <summary>Sma after BollingerBands resolves original close prices (30-period data → SMA(5) has 26 values).</summary>
    [Fact]
    public void BollingerBands_ThenSma_ResolvesOriginalPriceData()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Vol,
            configurePricePanel: ax =>
            {
                ax.BollingerBands(20);
                ax.Sma(5);
            })
            .Build();

        // Price panel has: CandlestickSeries + BB AreaSeries + BB LineSeries + SMA LineSeries = 4 series
        Assert.Equal(4, figure.SubPlots[0].Series.Count);
    }
}
