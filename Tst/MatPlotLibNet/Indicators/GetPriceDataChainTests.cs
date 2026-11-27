// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

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
