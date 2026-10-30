// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Fluent extension methods for attaching streaming indicators to a
/// <see cref="StreamingCandlestickSeries"/>. <see cref="WithStreaming{TIndicator}"/> is the one attach: the indicator is fed every
/// bar the series receives, with the volume the bar came with, and its output lines are added to the axes. The named verbs
/// (<c>WithStreamingSma</c>, …) are that call with the indicator made for the reader.</summary>
public static class StreamingIndicatorExtensions
{
    extension(StreamingCandlestickSeries candles)
    {
        /// <summary>Attaches a streaming indicator: feeds it every bar the series receives (the prices of the bar, and its
        /// volume, or NaN when the bar came without one) and adds its output lines to <paramref name="axes"/>.</summary>
        /// <typeparam name="TIndicator">The indicator; it keeps its own type, so a caller can read its warm-up and latest value.</typeparam>
        /// <param name="axes">The axes the output lines are added to.</param>
        /// <param name="indicator">The indicator, made by the caller.</param>
        /// <returns><paramref name="indicator"/>.</returns>
        public TIndicator WithStreaming<TIndicator>(Axes axes, TIndicator indicator)
            where TIndicator : IStreamingIndicator
        {
            candles.OhlcvBarAppended += indicator.AppendBar;
            foreach (var s in indicator.OutputSeries) axes.AddSeries(s);
            return indicator;
        }

        /// <summary>Attaches a streaming SMA that auto-computes on each appended bar.</summary>
        public StreamingSma WithStreamingSma(Axes axes, int period)
            => candles.WithStreaming(axes, new StreamingSma(period, candles.Capacity));

        /// <summary>Attaches a streaming EMA that auto-computes on each appended bar.</summary>
        public StreamingEma WithStreamingEma(Axes axes, int period)
            => candles.WithStreaming(axes, new StreamingEma(period, candles.Capacity));

        /// <summary>Attaches a streaming RSI that auto-computes on each appended bar.</summary>
        public StreamingRsi WithStreamingRsi(Axes axes, int period = 14)
            => candles.WithStreaming(axes, new StreamingRsi(period, candles.Capacity));

        /// <summary>Attaches streaming Bollinger Bands that auto-compute on each appended bar.</summary>
        public StreamingBollinger WithStreamingBollinger(Axes axes,
            int period = 20, double numStdDev = 2.0)
            => candles.WithStreaming(axes, new StreamingBollinger(period, numStdDev, candles.Capacity));

        /// <summary>Attaches a streaming MACD that auto-computes on each appended bar.</summary>
        public StreamingMacd WithStreamingMacd(Axes axes,
            int fast = 12, int slow = 26, int signal = 9)
            => candles.WithStreaming(axes, new StreamingMacd(fast, slow, signal, candles.Capacity));

        /// <summary>Attaches a streaming ATR that auto-computes on each appended candle.</summary>
        public StreamingAtr WithStreamingAtr(Axes axes, int period = 14)
            => candles.WithStreaming(axes, new StreamingAtr(period, candles.Capacity));

        /// <summary>Attaches a streaming Stochastic Oscillator that auto-computes on each appended candle.</summary>
        public StreamingStochastic WithStreamingStochastic(Axes axes,
            int kPeriod = 14, int dPeriod = 3)
            => candles.WithStreaming(axes, new StreamingStochastic(kPeriod, dPeriod, candles.Capacity));

        /// <summary>Attaches a streaming Williams %R that auto-computes on each appended candle.</summary>
        public StreamingWilliamsR WithStreamingWilliamsR(Axes axes, int period = 14)
            => candles.WithStreaming(axes, new StreamingWilliamsR(period, candles.Capacity));

        /// <summary>Attaches a streaming CCI that auto-computes on each appended candle.</summary>
        public StreamingCci WithStreamingCci(Axes axes, int period = 20)
            => candles.WithStreaming(axes, new StreamingCci(period, candles.Capacity));

        /// <summary>Attaches a streaming on-balance volume that adds or subtracts the volume of each appended bar. A bar that
        /// comes without a volume makes the sum NaN, because the sum cannot be known.</summary>
        public StreamingObv WithStreamingObv(Axes axes)
            => candles.WithStreaming(axes, new StreamingObv(candles.Capacity));

        /// <summary>Attaches a streaming VWAP that weighs the typical price of each appended bar by its volume.</summary>
        public StreamingVwap WithStreamingVwap(Axes axes)
            => candles.WithStreaming(axes, new StreamingVwap(candles.Capacity));
    }
}
