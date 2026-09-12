// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming signal series optimized for uniformly-sampled data. X is computed from
/// <see cref="SampleRate"/> and <see cref="XStart"/> rather than stored, so a sample carries its own absolute
/// ORDINAL — the count of samples ever appended before it — and that ordinal is what X is derived from.
///
/// <para>The ordinal used to live in a separate <c>_totalAppended</c> counter, advanced in a second act right
/// after the buffer. A reader between the two computed the first sample's ordinal from a total that had not
/// caught up, and every X in the snapshot shifted by a sample. Keeping the ordinal WITH its value makes that
/// unrepresentable; the counter is now the writer's own and is never read on the read path.</para>
///
/// <para>Ideal for oscilloscope, audio, and telemetry data at fixed sample rates.</para></summary>
public sealed class StreamingSignalSeries : ChartSeries, IHasColor
{
    // The writer's own counter: it hands out the next ordinal and is never read to answer a question.
    private readonly RingBuffer<SignalSample> _samples;
    private long _version;
    private long _nextOrdinal;

    /// <summary>One sample and the ordinal it was appended at — together, because X is derived from the
    /// ordinal and a Y under someone else's ordinal is drawn in the wrong place.</summary>
    private readonly record struct SignalSample(long Ordinal, double Y);

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
    public int Count => _samples.Count;

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
        _samples = new RingBuffer<SignalSample>(capacity);
    }

    /// <summary>Appends a single Y sample. X is computed automatically.</summary>
    public void AppendSample(double y)
    {
        _samples.Append(new SignalSample(_nextOrdinal++, y));
        Interlocked.Increment(ref _version);
    }

    /// <summary>Appends a batch of Y samples.</summary>
    public void AppendSamples(ReadOnlySpan<double> y)
    {
        var samples = new SignalSample[y.Length];
        for (int i = 0; i < y.Length; i++)
        {
            samples[i] = new SignalSample(_nextOrdinal++, y[i]);
        }

        _samples.AppendRange(samples);
        Interlocked.Increment(ref _version);
    }

    /// <summary>Removes all samples from the buffer.</summary>
    public void Clear()
    {
        _samples.Clear();
        _nextOrdinal = 0;
        Interlocked.Increment(ref _version);
    }

    /// <summary>Computes the X-coordinate for the sample at logical index <paramref name="index"/>
    /// (0 = oldest retained), from that sample's OWN ordinal.</summary>
    /// <param name="index">The logical index into what is retained.</param>
    /// <returns>The X coordinate of that sample.</returns>
    public double XAt(int index) => XOf(_samples[index].Ordinal);

    private double XOf(long ordinal) => XStart + ordinal / SampleRate;

    /// <summary>Creates an immutable snapshot. X values are computed from sample rate.</summary>
    public StreamingSnapshot CreateSnapshot()
    {
        // ONE read. Every X comes from the ordinal its own Y was appended under, so no separately-advancing
        // counter can shift the whole trace by a sample.
        long version = Version;
        var samples = _samples.ToArray();
        var xData = new double[samples.Length];
        var yData = new double[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            xData[i] = XOf(samples[i].Ordinal);
            yData[i] = samples[i].Y;
        }

        return new StreamingSnapshot(xData, yData, version);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // One read, one state: the X ends and the Y extremes describe the same moment or none.
        var samples = _samples.ToArray();
        if (samples.Length == 0)
        {
            return new(null, null, null, null);
        }

        double yMin = samples[0].Y, yMax = samples[0].Y;
        for (int i = 1; i < samples.Length; i++)
        {
            yMin = Math.Min(yMin, samples[i].Y);
            yMax = Math.Max(yMax, samples[i].Y);
        }

        return new(XOf(samples[0].Ordinal), XOf(samples[^1].Ordinal), yMin, yMax);
    }

    /// <inheritdoc />
    public override Serialization.SeriesDto ToSeriesDto() => new()
    {
        Type = "streaming_signal",
        Label = Label
    };


    /// <inheritdoc />
    /// <remarks>The buffer's current snapshot, with the sample rate turning the index into a time.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var snapshot = CreateSnapshot();
        return ChartDataTable.FromNumberColumns(
            [new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y)], [snapshot.XData, snapshot.YData]);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
