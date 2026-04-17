// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming signal series optimized for uniformly-sampled data. Only Y values are stored;
/// X is computed from <see cref="SampleRate"/> and <see cref="XStart"/>. Ideal for oscilloscope,
/// audio, and telemetry data at fixed sample rates.</summary>
public sealed class StreamingSignalSeries : ChartSeries, IHasColor
{
    private readonly DoubleRingBuffer _yBuffer;
    private long _version;
    private long _totalAppended;

    /// <summary>Signal color. When <c>null</c> the theme's prop-cycler assigns one.</summary>
    public Color? Color { get; set; }

    /// <summary>Line width in pixels. Default 1.0.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>Samples per X-unit. Default 1.0 (one sample per X unit).</summary>
    public double SampleRate { get; }

    /// <summary>X-coordinate of the first sample. Default 0.0.</summary>
    public double XStart { get; }

    /// <summary>Monotonically increasing version counter.</summary>
    public long Version => Interlocked.Read(ref _version);

    /// <summary>Number of samples currently in the buffer.</summary>
    public int Count => _yBuffer.Count;

    /// <summary>Maximum number of samples the buffer can hold.</summary>
    public int Capacity { get; }

    /// <summary>Initializes a new streaming signal series.</summary>
    /// <param name="capacity">Maximum samples retained. Default 100,000.</param>
    /// <param name="sampleRate">Samples per X-unit. Default 1.0.</param>
    /// <param name="xStart">X-coordinate of the first sample. Default 0.0.</param>
    public StreamingSignalSeries(int capacity = 100_000, double sampleRate = 1.0, double xStart = 0.0)
    {
        Capacity = capacity;
        SampleRate = sampleRate;
        XStart = xStart;
        _yBuffer = new DoubleRingBuffer(capacity);
    }

    /// <summary>Appends a single Y sample. X is computed automatically.</summary>
    public void AppendSample(double y)
    {
        _yBuffer.Append(y);
        Interlocked.Increment(ref _totalAppended);
        Interlocked.Increment(ref _version);
    }

    /// <summary>Appends a batch of Y samples.</summary>
    public void AppendSamples(ReadOnlySpan<double> y)
    {
        _yBuffer.AppendRange(y);
        Interlocked.Add(ref _totalAppended, y.Length);
        Interlocked.Increment(ref _version);
    }

    /// <summary>Removes all samples from the buffer.</summary>
    public void Clear()
    {
        _yBuffer.Clear();
        Interlocked.Exchange(ref _totalAppended, 0);
        Interlocked.Increment(ref _version);
    }

    /// <summary>Computes the X-coordinate for the sample at logical index <paramref name="index"/>.</summary>
    public double XAt(int index)
    {
        long firstSampleIndex = _totalAppended - _yBuffer.Count;
        return XStart + (firstSampleIndex + index) / SampleRate;
    }

    /// <summary>Creates an immutable snapshot. X values are computed from sample rate.</summary>
    public StreamingSnapshot CreateSnapshot()
    {
        var yData = _yBuffer.ToArray();
        var xData = new double[yData.Length];
        long firstSampleIndex = Interlocked.Read(ref _totalAppended) - yData.Length;
        for (int i = 0; i < xData.Length; i++)
            xData[i] = XStart + (firstSampleIndex + i) / SampleRate;
        return new StreamingSnapshot(xData, yData, Version);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        int count = _yBuffer.Count;
        if (count == 0) return new(null, null, null, null);
        double xMin = XAt(0);
        double xMax = XAt(count - 1);
        return new(xMin, xMax, _yBuffer.Min, _yBuffer.Max);
    }

    /// <inheritdoc />
    public override Serialization.SeriesDto ToSeriesDto() => new()
    {
        Type = "streaming_signal",
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
