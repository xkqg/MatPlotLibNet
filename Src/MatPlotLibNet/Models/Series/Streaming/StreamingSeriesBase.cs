// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>Abstract base for XY streaming series backed by twin <see cref="DoubleRingBuffer"/> instances.
/// Subclasses provide visual properties and visitor dispatch.</summary>
public abstract class StreamingSeriesBase : ChartSeries, IStreamingSeries
{
    private readonly DoubleRingBuffer _xBuffer;
    private readonly DoubleRingBuffer _yBuffer;
    private long _version;

    /// <inheritdoc />
    public long Version => Interlocked.Read(ref _version);

    /// <inheritdoc />
    public int Count => _xBuffer.Count;

    /// <inheritdoc />
    public int Capacity { get; }

    /// <summary>Initializes a new streaming series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum number of data points retained. Oldest are evicted when exceeded.</param>
    protected StreamingSeriesBase(int capacity)
    {
        Capacity = capacity;
        _xBuffer = new DoubleRingBuffer(capacity);
        _yBuffer = new DoubleRingBuffer(capacity);
    }

    /// <inheritdoc />
    public void AppendPoint(double x, double y)
    {
        _xBuffer.Append(x);
        _yBuffer.Append(y);
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public void AppendPoints(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        if (x.Length != y.Length)
            throw new ArgumentException("X and Y spans must have equal length.");
        _xBuffer.AppendRange(x);
        _yBuffer.AppendRange(y);
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _xBuffer.Clear();
        _yBuffer.Clear();
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public StreamingSnapshot CreateSnapshot()
    {
        return new StreamingSnapshot(_xBuffer.ToArray(), _yBuffer.ToArray(), Version);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Count == 0) return new(null, null, null, null);
        return new(_xBuffer.Min, _xBuffer.Max, _yBuffer.Min, _yBuffer.Max);
    }

    /// <inheritdoc />
    public override Serialization.SeriesDto ToSeriesDto() => new()
    {
        Type = "streaming",
        Label = Label
    };
}
