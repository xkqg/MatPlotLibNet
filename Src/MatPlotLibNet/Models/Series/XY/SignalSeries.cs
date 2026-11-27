// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>
/// A line series optimised for uniformly-sampled data (e.g., audio, sensor streams).
/// X values are derived arithmetically — <c>x[i] = XStart + i / SampleRate</c> — so
/// <see cref="IndexRangeFor"/> runs in O(1) and <see cref="XData"/> is computed lazily,
/// keeping the hot-path (renderer) allocation-free for viewport slicing.
/// </summary>
public sealed class SignalSeries : XYSeries, XY.IMonotonicXY, IHasColor, ISeriesKind<SignalSeries>, IGivenStroked
{
    /// <inheritdoc />
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.Line(this);

    private double[]? _materializedX;

    /// <summary>Samples per X-unit (e.g., Hz for a time-domain signal).</summary>
    public double SampleRate { get; }

    /// <summary>X value of the first sample.</summary>
    public double XStart { get; }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The dash pattern of the line. Default solid.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>How many samples a signal holds per X-unit when it is told no rate: the default of the verbs, the streaming signal and the
    /// reader.</summary>
    internal const double RestingSampleRate = 1.0;

    /// <summary>Where a signal's first sample stands on X when it is told none.</summary>
    internal const double RestingXStart = 0.0;

    /// <summary>
    /// Initializes a new <see cref="SignalSeries"/> from Y samples.
    /// </summary>
    /// <param name="yData">Y values at each sample point.</param>
    /// <param name="sampleRate">Samples per X-unit. Defaults to <see cref="RestingSampleRate"/>.</param>
    /// <param name="xStart">X coordinate of the first sample. Defaults to <see cref="RestingXStart"/>.</param>
    public SignalSeries(double[] yData, double sampleRate = RestingSampleRate, double xStart = RestingXStart)
        : base(Array.Empty<double>(), yData)
    {
        SampleRate = sampleRate;
        XStart = xStart;
    }

    /// <summary>Lazily materialises the X array. The renderer never calls this — it uses
    /// <see cref="XAt"/> via <see cref="XY.IMonotonicXY"/> instead.</summary>
    public override double[] XData =>
        _materializedX ??= Enumerable.Range(0, YData.Length).Select(XAt).ToArray();

    /// <inheritdoc />
    internal override bool HoldsNoData => YData.Length == 0;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (HoldsNoData) return new(0, 0, 0, 0);
        // Over the drawable samples only: a missing sample (NaN) and an infinity are left out.
        var y = YData.DrawableRange();
        return new(XStart, XAt(YData.Length - 1), y?.Lo, y?.Hi);
    }

    // ── IMonotonicXY ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public int Length => YData.Length;

    /// <inheritdoc />
    public double XAt(int i) => XStart + i / SampleRate;

    /// <inheritdoc />
    public double YAt(int i) => YData[i];

    /// <inheritdoc />
    /// <remarks>O(1) arithmetic — no array scan.</remarks>
    public XY.IndexRange IndexRangeFor(double xMin, double xMax)
    {
        int n = YData.Length;
        if (n == 0) return new(0, 0);

        int s = (int)Math.Max(0, Math.Floor((xMin - XStart) * SampleRate));
        int e = (int)Math.Min(n, Math.Ceiling((xMax - XStart) * SampleRate) + 1);
        return s >= e ? new(0, 0) : new(s, e);
    }

    static string ISeriesKind<SignalSeries>.Kind => "signal";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        YData = YData,
        SignalSampleRate = SampleRate,
        SignalXStart = XStart.OrNullAtRest(RestingXStart),
        Label = Label,
        Stroking = Stroke,
        Color = Color
    };

    /// <summary>The reader of a <see cref="SignalSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static SignalSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Signal(dto.YData ?? [], dto.SignalSampleRate ?? RestingSampleRate, dto.SignalXStart ?? RestingXStart);
        s.Color = dto.Color;
        s.Stroke = dto.StrokeOver(s.Stroke);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
