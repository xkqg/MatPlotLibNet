// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
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
/// stand. The region is placed the way the parent places its data: through its transform on a Cartesian parent, in the
/// triangle's frame on a ternary one, by angle (x) and radius (y) on a polar one, its four corners joined straight as matplotlib
/// joins them. A 3-D parent draws none (matplotlib places that rectangle in the flat view of the 3-D axes, not in its data). The
/// indicator is not cut off at the parent's plot: a limit at or below zero on a log parent stands a thousand decades below the
/// axis, where matplotlib's log scale puts it, so the rectangle runs off the figure; a corner a logit parent masks (outside 0 to
/// 1) has no place, and the sides and the line that end there are left out, as matplotlib leaves them. The indicator is drawn over the parent's marks and under the inset, as matplotlib's (zorder 4.99, the inset 5), so
/// the inset covers the part of the region it overlaps.</remarks>
public sealed class ZoomIndicator : IChangeTracked, IStrokedTool
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this zoom indicator changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    private double _alpha = 0.5;
    private double _lineWidth = 1.0;

    /// <summary>The colour of the rectangle and the connectors. <see langword="null"/> (the default) takes the theme's neutral
    /// grey, matplotlib's 0.5 grey under the default theme.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The opacity of the rectangle and the connectors together, from 0 (clear) to 1 (opaque). 0.5 by default, as in
    /// matplotlib.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha
    {
        get => _alpha;
        set => Change(ref _alpha, value.ThrowIfNotOpacity(nameof(Alpha)));
    }

    /// <summary>The width of the rectangle's sides and of the connectors, in pixels. 1 by default.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, infinite or not a number.</exception>
    public double LineWidth
    {
        get => _lineWidth;
        set => Change(ref _lineWidth, value.ThrowIfNotFinite(nameof(LineWidth)).ThrowIfBelow(0, nameof(LineWidth)));
    }

    /// <summary>The dash pattern of the rectangle and the connectors. Solid by default; <see cref="LineStyle.None"/> draws
    /// nothing.</summary>
    public LineStyle LineStyle { get; set => Change(ref field, value); } = LineStyle.Solid;

    /// <summary>Whether the lines from the rectangle to the inset are drawn (the two matplotlib's rule keeps). <see langword="true"/>
    /// by default; <see langword="false"/> draws the rectangle alone.</summary>
    public bool Connectors { get; set => Change(ref field, value); } = true;
}
