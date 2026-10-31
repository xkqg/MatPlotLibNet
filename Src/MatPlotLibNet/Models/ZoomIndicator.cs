// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>The zoom an inset indicates on its parent (matplotlib's <c>indicate_inset_zoom</c>): the parent draws a rectangle
/// over the region the inset shows, in the parent's data coordinates, and lines from the rectangle's corners to the corners of
/// the inset's frame. Set on the INSET (<see cref="Axes.ZoomIndicator"/>, or <c>IndicateZoom</c> on the inset's builder); the
/// rectangle always spans the limits the inset draws, its own limits or the ones it scaled to its data (margins included).</summary>
/// <remarks>Of the four connectors matplotlib's rule keeps two, chosen by where the inset stands against the rectangle on the
/// picture (an inset up and to the right of its region draws the upper-left and lower-right ones), so the lines do not cross
/// the inset; an inset wholly inside its region, or a region wholly inside its inset, draws none. The rule is applied in
/// pixels, so a parent axis that runs backwards, a log axis and an inset whose limits run backwards are all drawn where they
/// stand. Only a Cartesian parent draws the indicator: a polar, 3-D or ternary parent has no data rectangle to draw the region
/// in, and nothing is drawn either when a corner of the region is a value the parent's axis cannot place (below zero on a log
/// axis). The indicator is drawn over the parent's marks, right after the inset.</remarks>
public sealed class ZoomIndicator
{
    private double _alpha = 0.5;
    private double _lineWidth = 1.0;

    /// <summary>The colour of the rectangle and the connectors. <see langword="null"/> (the default) takes the theme's neutral
    /// grey, matplotlib's 0.5 grey under the default theme.</summary>
    public Color? Color { get; set; }

    /// <summary>The opacity of the rectangle and the connectors together, from 0 (clear) to 1 (opaque). 0.5 by default, as in
    /// matplotlib.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha
    {
        get => _alpha;
        set => _alpha = value is >= 0 and <= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The opacity of a zoom indicator (Alpha) must be between 0 and 1.");
    }

    /// <summary>The width of the rectangle's sides and of the connectors, in pixels. 1 by default.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, infinite or not a number.</exception>
    public double LineWidth
    {
        get => _lineWidth;
        set => _lineWidth = double.IsFinite(value) && value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The line width of a zoom indicator (LineWidth) must be a finite number of pixels, 0 or more.");
    }

    /// <summary>The dash pattern of the rectangle and the connectors. Solid by default; <see cref="LineStyle.None"/> draws
    /// nothing.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Whether the lines from the rectangle to the inset are drawn (the two matplotlib's rule keeps). <see langword="true"/>
    /// by default; <see langword="false"/> draws the rectangle alone.</summary>
    public bool Connectors { get; set; } = true;
}
