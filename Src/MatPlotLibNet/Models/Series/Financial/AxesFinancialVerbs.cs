// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The financial-family verbs of <see cref="Axes"/> (<c>Candlestick</c>, <c>OhlcBar</c>), each constructing
/// one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesFinancialVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a candlestick (OHLC) series from the given price data.</summary>
        public CandlestickSeries Candlestick(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null)
        {
            open.Length.ThrowIfMisaligned(high.Length);
            open.Length.ThrowIfMisaligned(low.Length);
            open.Length.ThrowIfMisaligned(close.Length);
            return axes.AddSeries(new CandlestickSeries(open, high, low, close) { DateLabels = dateLabels });
        }

        /// <summary>Adds a traditional OHLC bar chart series from price data.</summary>
        public OhlcBarSeries OhlcBar(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null)
        {
            open.Length.ThrowIfMisaligned(high.Length);
            open.Length.ThrowIfMisaligned(low.Length);
            open.Length.ThrowIfMisaligned(close.Length);
            return axes.AddSeries(new OhlcBarSeries(open, high, low, close) { DateLabels = dateLabels });
        }
    }
}
