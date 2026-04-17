// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>Contract for series that accept streamed data points via ring-buffer-backed storage.</summary>
public interface IStreamingSeries
{
    /// <summary>Appends a single (x, y) data point.</summary>
    void AppendPoint(double x, double y);

    /// <summary>Appends a batch of (x, y) data points.</summary>
    void AppendPoints(ReadOnlySpan<double> x, ReadOnlySpan<double> y);

    /// <summary>Removes all data from the buffer.</summary>
    void Clear();

    /// <summary>Monotonically increasing version counter, incremented on each append.</summary>
    long Version { get; }

    /// <summary>Number of data points currently in the buffer.</summary>
    int Count { get; }

    /// <summary>Maximum number of data points the buffer can hold.</summary>
    int Capacity { get; }

    /// <summary>Creates an immutable point-in-time copy of the data for safe render-thread access.</summary>
    StreamingSnapshot CreateSnapshot();
}
