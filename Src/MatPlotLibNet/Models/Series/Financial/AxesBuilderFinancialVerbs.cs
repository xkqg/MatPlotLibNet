// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the financial family (candlesticks, OHLC bars, point and
/// figure): each adds the series through the axes verb of the same name (<see cref="AxesFinancialVerbs"/>) and hands it to the
/// caller's callback.</summary>
public static class AxesBuilderFinancialVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a traditional OHLC bar chart series to the axes.</summary>
        public AxesBuilder OhlcBar(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null, Action<OhlcBarSeries>? configure = null)
            => axes.Configured(axes.Axes.OhlcBar(open, high, low, close, dateLabels), configure);

        /// <summary>Adds a candlestick (OHLC) series to the axes.</summary>
        public AxesBuilder Candlestick(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null, Action<CandlestickSeries>? configure = null)
            => axes.Configured(axes.Axes.Candlestick(open, high, low, close, dateLabels), configure);

        /// <summary>Adds a Point and Figure chart (close only) to the axes: the price as columns of boxes, Xs where it rose and Os
        /// where it fell, with no axis for time.</summary>
        /// <param name="prices">The closing prices, oldest first.</param>
        /// <param name="boxSize">The price one box covers; a finite number above zero.</param>
        /// <param name="reversal">How many boxes back from the last box drawn the price has to come to start a column the other
        /// way; 1 or more, and <see cref="PointAndFigureSeries.RestingReversal"/> (the classic) unless said otherwise.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>This builder.</returns>
        public AxesBuilder PointAndFigure(double[] prices, double boxSize, int reversal = PointAndFigureSeries.RestingReversal, Action<PointAndFigureSeries>? configure = null)
            => axes.Configured(axes.Axes.PointAndFigure(prices, boxSize, reversal), configure);
    }
}
