// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Contract for incremental technical indicators that recompute O(1) per appended data point.
/// Each indicator owns one or more <see cref="StreamingLineSeries"/> that it auto-appends to.</summary>
public interface IStreamingIndicator
{
    /// <summary>Processes a new close price and updates internal state.</summary>
    void Append(double price);

    /// <summary>Processes a new OHLC bar and updates internal state.</summary>
    void AppendCandle(OhlcBar bar);

    /// <summary>Most recently computed indicator value.</summary>
    double GetLatest();

    /// <summary>Number of data points processed so far.</summary>
    int ProcessedCount { get; }

    /// <summary>Number of data points required before the indicator emits valid (non-NaN) values.</summary>
    int WarmupPeriod { get; }

    /// <summary>Whether the indicator has received enough data to produce valid output.</summary>
    bool IsWarmedUp => ProcessedCount >= WarmupPeriod;

    /// <summary>The output series that this indicator appends its computed values to.</summary>
    IReadOnlyList<StreamingLineSeries> OutputSeries { get; }
}
