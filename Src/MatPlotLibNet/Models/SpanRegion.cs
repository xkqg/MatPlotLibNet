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

    public Orientation Orientation { get; }

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.2;

    public LineStyle LineStyle { get; set; } = LineStyle.None;

    public double LineWidth { get; set; } = 1.0;

    public Color? EdgeColor { get; set; }

    public string? Label { get; set; }

    /// <summary>Creates a new span region between the given data values.</summary>
    public SpanRegion(double min, double max, Orientation orientation)
    {
        Min = min;
        Max = max;
        Orientation = orientation;
    }
}
