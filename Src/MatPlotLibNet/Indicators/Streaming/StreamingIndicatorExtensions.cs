// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Fluent extension methods for attaching streaming indicators to a
/// <see cref="StreamingCandlestickSeries"/>. Each indicator auto-subscribes to
/// <see cref="IStreamingOhlcSeries.BarAppended"/> and appends its output to the same axes.</summary>
public static class StreamingIndicatorExtensions
{
    extension(StreamingCandlestickSeries candles)
    {
        /// <summary>Attaches a streaming SMA that auto-computes on each appended bar.</summary>
        public StreamingSma WithStreamingSma(Axes axes, int period)
        {
            var ind = new StreamingSma(period, candles.Capacity);
            candles.BarAppended += bar => ind.Append(bar.Close);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches a streaming EMA that auto-computes on each appended bar.</summary>
        public StreamingEma WithStreamingEma(Axes axes, int period)
        {
            var ind = new StreamingEma(period, candles.Capacity);
            candles.BarAppended += bar => ind.Append(bar.Close);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches a streaming RSI that auto-computes on each appended bar.</summary>
        public StreamingRsi WithStreamingRsi(Axes axes, int period = 14)
        {
            var ind = new StreamingRsi(period, candles.Capacity);
            candles.BarAppended += bar => ind.Append(bar.Close);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches streaming Bollinger Bands that auto-compute on each appended bar.</summary>
        public StreamingBollinger WithStreamingBollinger(Axes axes,
            int period = 20, double numStdDev = 2.0)
        {
            var ind = new StreamingBollinger(period, numStdDev, candles.Capacity);
            candles.BarAppended += bar => ind.Append(bar.Close);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches a streaming MACD that auto-computes on each appended bar.</summary>
        public StreamingMacd WithStreamingMacd(Axes axes,
            int fast = 12, int slow = 26, int signal = 9)
        {
            var ind = new StreamingMacd(fast, slow, signal, candles.Capacity);
            candles.BarAppended += bar => ind.Append(bar.Close);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches a streaming ATR that auto-computes on each appended candle.</summary>
        public StreamingAtr WithStreamingAtr(Axes axes, int period = 14)
        {
            var ind = new StreamingAtr(period, candles.Capacity);
            candles.BarAppended += bar => ind.AppendCandle(bar);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches a streaming Stochastic Oscillator that auto-computes on each appended candle.</summary>
        public StreamingStochastic WithStreamingStochastic(Axes axes,
            int kPeriod = 14, int dPeriod = 3)
        {
            var ind = new StreamingStochastic(kPeriod, dPeriod, candles.Capacity);
            candles.BarAppended += bar => ind.AppendCandle(bar);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches a streaming Williams %R that auto-computes on each appended candle.</summary>
        public StreamingWilliamsR WithStreamingWilliamsR(Axes axes, int period = 14)
        {
            var ind = new StreamingWilliamsR(period, candles.Capacity);
            candles.BarAppended += bar => ind.AppendCandle(bar);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }

        /// <summary>Attaches a streaming CCI that auto-computes on each appended candle.</summary>
        public StreamingCci WithStreamingCci(Axes axes, int period = 20)
        {
            var ind = new StreamingCci(period, candles.Capacity);
            candles.BarAppended += bar => ind.AppendCandle(bar);
            foreach (var s in ind.OutputSeries) axes.AddSeries(s);
            return ind;
        }
    }
}
