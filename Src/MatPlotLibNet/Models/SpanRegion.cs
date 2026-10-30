// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a shaded rectangular region spanning the full width or height of the axes.</summary>
/// <remarks>A span is clipped to the plot area, as matplotlib's <c>axhspan</c> is: an edge beyond the range the axis
/// draws (any number outside it, an infinity, <see cref="double.MaxValue"/>) runs to the plot edge, and a span wholly beyond
/// the range draws nothing. A span with a border has no border line along an edge that was cut. A span with an edge that is
/// not a number (NaN) is not drawn.</remarks>
public sealed class SpanRegion
{
    /// <summary>One edge of the span in data units, as it was given; the renderer takes the two edges in either
    /// order.</summary>
    public double Min { get; }

    /// <summary>The other edge of the span in data units, as it was given.</summary>
    public double Max { get; }

    /// <summary>Whether the span is a horizontal band between two Y values or a vertical band between two X values. Fixed
    /// when the span is created.</summary>
    public Orientation Orientation { get; }

    /// <summary>The fill colour of the span. <see langword="null"/> (the default) takes the first colour of the standard
    /// cycle, a blue.</summary>
    public Color? Color { get; set; }

    /// <summary>The opacity of the fill, from 0 (clear) to 1 (opaque). Default 0.2.</summary>
    public double Alpha { get; set; } = 0.2;

    /// <summary>The dash pattern of the span's border. <see cref="LineStyle.None"/> (the default) draws no border.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.None;

    /// <summary>The width of the border line, in pixels. Default 1; it shows only when <see cref="LineStyle"/> draws a line.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>The colour of the border and of the label. <see langword="null"/> (the default) takes the fill colour.</summary>
    public Color? EdgeColor { get; set; }

    /// <summary>Text drawn inside the span near its top, at the left for a horizontal span and centred for a vertical one,
    /// in the border colour. <see langword="null"/> (the default) draws none.</summary>
    public string? Label { get; set; }

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
