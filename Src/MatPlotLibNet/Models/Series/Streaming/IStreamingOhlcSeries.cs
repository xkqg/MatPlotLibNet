// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>Contract for series that accept streamed OHLC (Open-High-Low-Close) bars.</summary>
public interface IStreamingOhlcSeries
{
    /// <summary>Appends a single OHLC bar.</summary>
    void AppendBar(double open, double high, double low, double close);

    /// <summary>Appends a single OHLC bar from a record.</summary>
    void AppendBar(OhlcBar bar);

    /// <summary>Removes all data from the buffer.</summary>
    void Clear();

    /// <summary>Monotonically increasing version counter, incremented on each append.</summary>
    long Version { get; }

    /// <summary>Number of bars currently in the buffer.</summary>
    int Count { get; }

    /// <summary>Maximum number of bars the buffer can hold.</summary>
    int Capacity { get; }

    /// <summary>Creates an immutable point-in-time copy of the OHLC data for safe render-thread access.</summary>
    OhlcStreamingSnapshot CreateOhlcSnapshot();

    /// <summary>Fired after each <see cref="AppendBar(OhlcBar)"/> call, carrying the appended bar.</summary>
    event Action<OhlcBar>? BarAppended;
}
