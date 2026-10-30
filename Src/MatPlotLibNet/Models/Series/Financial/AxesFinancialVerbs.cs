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

        /// <summary>Adds a Point and Figure chart (close only): the price as columns of boxes, Xs where it rose and Os where it
        /// fell, with no axis for time.</summary>
        /// <param name="prices">The closing prices, oldest first.</param>
        /// <param name="boxSize">The price one box covers; a finite number above zero.</param>
        /// <param name="reversal">How many boxes back from the last box drawn the price has to come to start a column the other
        /// way; 1 or more, and 3 (the classic) unless said otherwise.</param>
        /// <returns>The newly created <see cref="PointAndFigureSeries"/> for further configuration.</returns>
        public PointAndFigureSeries PointAndFigure(double[] prices, double boxSize, int reversal = 3) =>
            axes.AddSeries(new PointAndFigureSeries(prices, boxSize, reversal));

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
