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
public sealed class SignalSeries : XYSeries, XY.IMonotonicXY, IHasColor
{
    private double[]? _materializedX;

    /// <summary>Samples per X-unit (e.g., Hz for a time-domain signal).</summary>
    public double SampleRate { get; }

    /// <summary>X value of the first sample.</summary>
    public double XStart { get; }

    public Color? Color { get; set; }

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    /// <summary>
    /// Initializes a new <see cref="SignalSeries"/> from Y samples.
    /// </summary>
    /// <param name="yData">Y values at each sample point.</param>
    /// <param name="sampleRate">Samples per X-unit. Defaults to 1.</param>
    /// <param name="xStart">X coordinate of the first sample. Defaults to 0.</param>
    public SignalSeries(double[] yData, double sampleRate = 1.0, double xStart = 0.0)
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
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        YData.Length == 0
            ? new(0, 0, 0, 0)
            : new(XStart, XAt(YData.Length - 1), YData.Min(), YData.Max());

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

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "signal",
        YData = YData,
        SignalSampleRate = SampleRate,
        SignalXStart = XStart == 0.0 ? null : XStart,
        Label = Label,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        Color = Color
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
