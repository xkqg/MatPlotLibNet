// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
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
public sealed class StreamingSignalSeries : ChartSeries, IVersionedSeries, IHasColor, ISeriesKind<StreamingSignalSeries>, IWidthGiven
{
    /// <inheritdoc />
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    /// <remarks>The handle of the line it is drawn as (a <see cref="LineSeries"/> with the width it was given, by its kind's rule, and
    /// no marker of its own): a line with the marker its colour slot cycles to, if any, at a line's resting size.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.Of(LegendSwatchKind.LineMarker, this) with { MarkerSize = LineSeries.RestingMarkerSize };

    private readonly StreamRing<SignalSample> _samples;

    // The writer's own counter: it hands out the next ordinal and is never read to answer a question.
    private long _nextOrdinal;

    /// <summary>One sample and the ordinal it was appended at — together, because X is derived from the
    /// ordinal and a Y under someone else's ordinal is drawn in the wrong place.</summary>
    private readonly record struct SignalSample(long Ordinal, double Y);

    /// <summary>Signal color. When <c>null</c> the theme's prop-cycler assigns one.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The width of the line, in pixels. One given (the default included) is drawn as given; until one is, the line is drawn as a line
    /// given none (a <see cref="SignalSeries"/>'s rule): the width the theme's cycler names, else the theme's (the matplotlib themes
    /// draw matplotlib's <c>lines.linewidth</c>, 1.5 pt under v2 and 1 pt under classic), else the resting width of the library's one-pixel line, which this reads at rest.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>How wide the line is drawn where it was given no width: a line's rule (matplotlib's <c>Line2D</c>), with a resting width
    /// of its own.</summary>
    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.OnePixelLine;

    /// <summary>The width the line was given, <see langword="null"/> while none was and the slot decides.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

    /// <summary>Samples per X-unit. Default <see cref="RestingSampleRate"/>.</summary>
    public double SampleRate { get; }

    /// <summary>X-coordinate of the first sample. Default <see cref="RestingXStart"/>.</summary>
    public double XStart { get; }

    /// <summary>Monotonically increasing version counter: one more at every append, batch and clear. A streaming figure that holds
    /// this series asks for a frame when it rises.</summary>
    public long Version => _samples.Version;

    /// <summary>Number of samples currently in the buffer.</summary>
    public int Count => _samples.Count;

    /// <summary>Maximum number of samples the buffer can hold.</summary>
    public int Capacity => _samples.Capacity;

    /// <inheritdoc />
    internal override long LatestChange => ChangeClock.Latest(base.LatestChange, _samples.LatestChange);

    /// <summary>How many samples a streaming signal keeps when it is told no capacity.</summary>
    internal const int RestingCapacity = 100_000;

    /// <summary>The rate a streaming signal reads its samples at when it is told none: a signal's.</summary>
    internal const double RestingSampleRate = SignalSeries.RestingSampleRate;

    /// <summary>Where a streaming signal's first sample stands on X when it is told none: a signal's.</summary>
    internal const double RestingXStart = SignalSeries.RestingXStart;

    /// <summary>Initializes a new streaming signal series.</summary>
    /// <param name="capacity">Maximum samples retained. Default <see cref="RestingCapacity"/>.</param>
    /// <param name="sampleRate">Samples per X-unit. Default <see cref="RestingSampleRate"/>.</param>
    /// <param name="xStart">X-coordinate of the first sample. Default <see cref="RestingXStart"/>.</param>
    public StreamingSignalSeries(int capacity = RestingCapacity, double sampleRate = RestingSampleRate, double xStart = RestingXStart)
    {
        SampleRate = sampleRate;
        XStart = xStart;
        _samples = new StreamRing<SignalSample>(capacity);
    }

    /// <summary>Appends a single Y sample. X is computed automatically.</summary>
    public void AppendSample(double y) => _samples.Append(new SignalSample(_nextOrdinal++, y));

    /// <summary>Appends a batch of Y samples.</summary>
    public void AppendSamples(ReadOnlySpan<double> y)
    {
        var samples = new SignalSample[y.Length];
        for (int i = 0; i < y.Length; i++)
        {
            samples[i] = new SignalSample(_nextOrdinal++, y[i]);
        }

        _samples.AppendRange(samples);
    }

    /// <summary>Removes all samples from the buffer.</summary>
    public void Clear()
    {
        _nextOrdinal = 0;
        _samples.Clear();
    }

    /// <summary>Computes the X-coordinate for the sample at logical index <paramref name="index"/>
    /// (0 = oldest retained), from that sample's OWN ordinal.</summary>
    /// <param name="index">The logical index into what is retained.</param>
    /// <returns>The X coordinate of that sample.</returns>
    public double XAt(int index) => XOf(_samples[index].Ordinal);

    private double XOf(double ordinal) => XStart + ordinal / SampleRate;

    /// <summary>Creates an immutable snapshot. X values are computed from sample rate.</summary>
    public StreamingSnapshot CreateSnapshot()
    {
        // ONE read. Every X comes from the ordinal its own Y was appended under, so no separately-advancing
        // counter can shift the whole trace by a sample.
        var (samples, version) = _samples.Read();
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
        // One walk, one state: the X ends and the Y extremes describe the same moment or none, and asking copies nothing.
        // The ordinals are folded along the box's first side, the values along its second. The ordinals rise from the oldest sample to
        // the newest, so their extent runs from the first to the last.
        var walked = _samples.Aggregate(StreamingSeries.Box.Empty, static (box, sample) => box.Extend(sample.Ordinal, sample.Y));
        if (walked.Count == 0)
        {
            return default;
        }

        // Over the drawable samples only: a missing sample (NaN) and an infinity are left out.
        var y = walked.Y.OrNone();
        return new(XOf(walked.X.Lo), XOf(walked.X.Hi), y?.Lo, y?.Hi);
    }

    static string ISeriesKind<StreamingSignalSeries>.Kind => "streaming_signal";

    /// <inheritdoc />
    /// <remarks>Carries the ring buffer's current snapshot, its sample rate and its X start — the same fields
    /// <see cref="SignalSeries"/> already carries — so a restored stream starts from the picture it was, not
    /// empty (the roadmap row "Live series back from JSON"). The restored stream's own ordinal counter starts
    /// fresh at the snapshot's first sample; only the X-axis mapping (rate, start) and the Y values travel.</remarks>
    public override SeriesDto ToSeriesDto()
    {
        var snapshot = CreateSnapshot();
        return new()
        {
            Type = this.WireKind(),
            YData = snapshot.YData,
            Capacity = Capacity,
            SignalSampleRate = SampleRate,
            SignalXStart = XStart,
            Color = Color,
            LineWidth = GivenLineWidth,
        };
    }

    /// <summary>The reader of a <see cref="StreamingSignalSeries"/> that <see cref="SeriesRegistry"/> takes; it restores the capacity, sample rate and X start it was written with, and the samples of the snapshot.</summary>
    internal static StreamingSignalSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.AddSeries(new StreamingSignalSeries(
            dto.Capacity ?? RestingCapacity, dto.SignalSampleRate ?? RestingSampleRate, dto.SignalXStart ?? RestingXStart));
        s.AppendSamples(dto.YData ?? []);
        if (dto.Color is { } color) s.Color = color;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The buffer's current snapshot, with the sample rate turning the index into a time.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var snapshot = CreateSnapshot();
        return ChartDataTable.XY(Label, snapshot.XData, snapshot.YData);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
