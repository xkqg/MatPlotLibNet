// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a streaming series: each returns the series, because the series is the
/// handle live data is appended through.</summary>
public static class AxesBuilderStreamingVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a streaming line series backed by a ring buffer.</summary>
        /// <param name="capacity">Maximum data points retained. Default <see cref="StreamingSeries.RestingCapacity"/>.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>The created <see cref="StreamingLineSeries"/> for appending data.</returns>
        public StreamingLineSeries StreamingPlot(int capacity = StreamingSeries.RestingCapacity, Action<StreamingLineSeries>? configure = null)
            => axes.Streamed(new StreamingLineSeries(capacity), configure);

        /// <summary>Adds a streaming scatter series backed by a ring buffer.</summary>
        /// <param name="capacity">Maximum data points retained. Default <see cref="StreamingSeries.RestingCapacity"/>.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>The created <see cref="StreamingScatterSeries"/> for appending data.</returns>
        public StreamingScatterSeries StreamingScatter(int capacity = StreamingSeries.RestingCapacity, Action<StreamingScatterSeries>? configure = null)
            => axes.Streamed(new StreamingScatterSeries(capacity), configure);

        /// <summary>Adds a streaming signal series (uniform sample rate, Y-only storage).</summary>
        /// <param name="capacity">Maximum samples retained. Default <see cref="StreamingSignalSeries.RestingCapacity"/>.</param>
        /// <param name="sampleRate">Samples per X-unit. Default <see cref="StreamingSignalSeries.RestingSampleRate"/>.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>The created <see cref="StreamingSignalSeries"/> for appending samples.</returns>
        public StreamingSignalSeries StreamingSignal(int capacity = StreamingSignalSeries.RestingCapacity, double sampleRate = StreamingSignalSeries.RestingSampleRate, Action<StreamingSignalSeries>? configure = null)
            => axes.Streamed(new StreamingSignalSeries(capacity, sampleRate), configure);

        /// <summary>Adds a streaming candlestick series backed by four parallel ring buffers.</summary>
        /// <param name="capacity">Maximum bars retained. Default <see cref="StreamingCandlestickSeries.RestingCapacity"/>.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>The created <see cref="StreamingCandlestickSeries"/> for appending OHLC bars.</returns>
        public StreamingCandlestickSeries StreamingCandlestick(int capacity = StreamingCandlestickSeries.RestingCapacity, Action<StreamingCandlestickSeries>? configure = null)
            => axes.Streamed(new StreamingCandlestickSeries(capacity), configure);

        /// <summary>The one tail of a streaming verb: the series is added and handed to the caller's callback like any other, and returned
        /// instead of the builder, because the series is the handle live data is appended through.</summary>
        /// <typeparam name="TSeries">The streaming series type.</typeparam>
        /// <param name="series">The new streaming series.</param>
        /// <param name="configure">The caller's callback, or <see langword="null"/>.</param>
        /// <returns><paramref name="series"/>.</returns>
        private TSeries Streamed<TSeries>(TSeries series, Action<TSeries>? configure) where TSeries : ISeries
        {
            axes.AddSeries(series, configure);
            return series;
        }
    }
}
