// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a shaded rectangular region spanning the full width or height of the axes.</summary>
/// <remarks>A span is clipped to the plot area, as matplotlib's <c>axhspan</c> is: an edge beyond the range the axis
/// draws (any number outside it, an infinity, <see cref="double.MaxValue"/>) runs to the plot edge, and a span wholly beyond
/// the range draws nothing. A span with a border has no border line along an edge that was cut. A span with an edge that is
/// not a number (NaN) is not drawn. Under the matplotlib themes a span whose edges are values ranges its axis as well, as
/// <c>axhspan</c> and <c>axvspan</c> add their rectangle to the data limits, so only a limit set on the axis cuts it; under the
/// library's own themes the range is what the rest of the axes gives.</remarks>
public sealed class SpanRegion : IChangeTracked, IStrokedTool
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this span changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    /// <summary>One edge of the span in data units, as it was given; the renderer takes the two edges in either
    /// order.</summary>
    public double Min { get; }

    /// <summary>The other edge of the span in data units, as it was given.</summary>
    public double Max { get; }

    /// <summary>Whether the span is a horizontal band between two Y values or a vertical band between two X values. Fixed
    /// when the span is created.</summary>
    public Orientation Orientation { get; }

    /// <summary>Whether both edges of the span stand at a value: neither is missing (NaN), an infinity or ±<see cref="double.MaxValue"/>,
    /// the spelling of "to the plot edge" a threshold's zone is stored with. Only such a span adds its edges to the range of its axis
    /// under the matplotlib themes, as only a rectangle whose corners are numbers adds to matplotlib's data limits.</summary>
    internal bool EdgesStandAtValues => Math.Abs(Min) < double.MaxValue && Math.Abs(Max) < double.MaxValue;

    /// <summary>The fill colour of the span. <see langword="null"/> (the default) takes the first colour of the theme's cycle (matplotlib's
    /// <c>C0</c>, a blue under the standard cycle).</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The opacity of the fill, from 0 (clear) to 1 (opaque). Default 0.2.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = 0.2;

    /// <summary>The dash pattern of the span's border. The default, <see cref="RestingLineStyle"/>, draws no border.</summary>
    public LineStyle LineStyle { get; set => Change(ref field, value); } = RestingLineStyle;

    /// <summary>The dash pattern of a span's border when it is told none: none, so no border is drawn.</summary>
    internal const LineStyle RestingLineStyle = LineStyle.None;

    /// <summary>The width of the border line, in pixels. Default <see cref="RestingLineWidth"/>; it shows only when <see cref="LineStyle"/> draws a line.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = RestingLineWidth;

    /// <summary>The width of a span's border, in pixels, when it is told none.</summary>
    internal const double RestingLineWidth = 1.0;

    /// <summary>The colour of the border and of the label. <see langword="null"/> (the default) takes the fill colour.</summary>
    public Color? EdgeColor { get; set => Change(ref field, value); }

    /// <summary>The border is the span's line: the colour it names is its edge colour.</summary>
    Color? IStrokedTool.Color => EdgeColor;

    /// <summary>A border that names no colour takes the fill's (<see cref="FillUnder"/>).</summary>
    Color IStrokedTool.UnnamedColorUnder(Theme theme) => FillUnder(theme);

    /// <summary>The colour the span is filled with under <paramref name="theme"/>, before its <see cref="Alpha"/>: the colour it names,
    /// else the first colour of the theme's cycle.</summary>
    /// <param name="theme">The theme the span is drawn under.</param>
    /// <returns>The colour.</returns>
    internal Color FillUnder(Theme theme) => Color ?? theme.FirstCycleColor;

    /// <summary>Text drawn inside the span near its top, at the left for a horizontal span and centred for a vertical one,
    /// in the border colour. <see langword="null"/> (the default) draws none.</summary>
    public string? Label { get; set => Change(ref field, value); }

    /// <summary>Creates a new span region between the given data values.</summary>
    /// <param name="min">One edge of the span, in data units; either edge may be the larger.</param>
    /// <param name="max">The other edge of the span, in data units.</param>
    /// <param name="orientation">Whether the span is a horizontal band (edges are Y values) or a vertical band (edges are X values).</param>
    public SpanRegion(double min, double max, Orientation orientation)
    {
        Min = min;
        Max = max;
        Orientation = orientation;
    }
}
