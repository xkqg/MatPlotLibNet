// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>The checks an indicator makes of the bars it is given, each written once: a bar whose high is below its low, and a price
/// that is not above zero for an indicator that takes its logarithm or divides by it. The batch indicators check every bar when they
/// are made, a streaming form each bar it takes; both name the indicator, the bar and what they found. The checks of the parameters
/// are the batch class's <c>Spec</c> factory's (<c>ArgumentRangeExtensions</c>).</summary>
internal static class BarCheckExtensions
{
    /// <param name="bars">The bars an indicator is made with.</param>
    extension(OhlcvBars bars)
    {
        /// <summary>Refuses a bar whose high is below its low.</summary>
        /// <param name="indicator">The indicator, for the message.</param>
        /// <exception cref="ArgumentException">A bar's high is below its low.</exception>
        internal void ThrowIfHighBelowLow(string indicator)
        {
            for (int i = 0; i < bars.High.Length; i++) ThrowIfHighBelowLow(indicator, i, bars.High[i], bars.Low[i]);
        }

        /// <summary>Refuses an open, high, low or close that is not above zero.</summary>
        /// <param name="indicator">The indicator, for the message.</param>
        /// <exception cref="ArgumentException">A price is zero or below it.</exception>
        internal void ThrowIfNotPositiveOhlc(string indicator)
        {
            bars.Open.ThrowIfNotPositivePrices(indicator, "open");
            bars.High.ThrowIfNotPositivePrices(indicator, "high");
            bars.Low.ThrowIfNotPositivePrices(indicator, "low");
            bars.Close.ThrowIfNotPositivePrices(indicator, "close");
        }
    }

    /// <param name="bar">One bar a streaming form takes.</param>
    extension(OhlcvBar bar)
    {
        /// <summary>Refuses the bar when its high is below its low.</summary>
        /// <param name="indicator">The indicator, for the message.</param>
        /// <param name="index">The place of the bar, from 0.</param>
        /// <exception cref="ArgumentException">The bar's high is below its low.</exception>
        internal void ThrowIfHighBelowLow(string indicator, int index) => ThrowIfHighBelowLow(indicator, index, bar.High, bar.Low);
    }

    /// <param name="prices">One price of every bar.</param>
    extension(double[] prices)
    {
        /// <summary>Refuses a price that is not above zero.</summary>
        /// <param name="indicator">The indicator, for the message.</param>
        /// <param name="what">What the prices are (close, high, prices), for the message.</param>
        /// <param name="paramName">The parameter the prices came in, if the caller names one.</param>
        /// <exception cref="ArgumentException">A price is zero or below it.</exception>
        internal void ThrowIfNotPositivePrices(string indicator, string what, string? paramName = null)
        {
            for (int i = 0; i < prices.Length; i++) prices[i].ThrowIfNotPositivePrice(indicator, what, i, paramName);
        }
    }

    /// <param name="price">One price.</param>
    extension(double price)
    {
        /// <summary>Refuses the price when it is not above zero.</summary>
        /// <param name="indicator">The indicator, for the message.</param>
        /// <param name="what">What the price is, for the message.</param>
        /// <param name="index">The place of its bar, from 0.</param>
        /// <param name="paramName">The parameter the price came in, if the caller names one.</param>
        /// <exception cref="ArgumentException">The price is zero or below it.</exception>
        internal void ThrowIfNotPositivePrice(string indicator, string what, int index, string? paramName = null)
        {
            price.ThrowIfNotAbove(0, paramName, (indicator, what, index), static of => string.Invariant($"{of.indicator} requires strictly positive {of.what}; the price of bar {of.index}"));
        }
    }

    /// <summary>The one comparison of a bar's high with its low, and its message.</summary>
    private static void ThrowIfHighBelowLow(string indicator, int index, double high, double low)
    {
        if (high < low)
            throw new ArgumentException(string.Invariant($"{indicator} requires H >= L; bar {index} has H={high} < L={low}."));
    }
}
