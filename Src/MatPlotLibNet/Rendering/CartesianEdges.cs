// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What the edges opposite the primary axes of a Cartesian axes draw (<see cref="OppositeTicks"/>): the top edge of the X axis'
/// ticks, the right edge of the Y axis'. A drawn top X axis owns the top edge (<see cref="Axes.DrawnSecondaryXAxis"/>) and a drawn Y axis
/// on the right owns the right edge (<see cref="YAxisSlot.IsDrawn"/>: it has series or is linked), as matplotlib's <c>twiny</c> and <c>twinx</c> move the host's ticks off
/// the edge they take; otherwise the edge carries the mirrored ticks the axis asked for, or the marks of a theme that marks every side.
/// Worked out once a render; the drawing and the measuring of the bands read it.</summary>
/// <param name="X">What the top edge draws of the X axis' ticks.</param>
/// <param name="Y">What the right edge draws of the Y axis' ticks.</param>
internal readonly record struct CartesianEdges(OppositeTicks X, OppositeTicks Y)
{
    /// <summary>The opposite edges of <paramref name="axes"/> under <paramref name="theme"/>.</summary>
    /// <param name="axes">The axes.</param>
    /// <param name="theme">The theme it is drawn under: whether it marks every side.</param>
    /// <returns>The opposite edges.</returns>
    internal static CartesianEdges Of(Axes axes, Theme theme)
    {
        bool onAllSides = theme.Looks.Ticks.OnAllSides;
        bool topAxis = axes.DrawnSecondaryXAxis is not null;
        var right = axes.YAxes.Right;
        bool rightAxis = false;
        for (int i = 0; i < right.Count; i++) rightAxis |= right[i].IsDrawn;
        return new CartesianEdges(
            OppositeTicks.Of(axes.XAxis.MajorTicks.Mirror, onAllSides, topAxis),
            OppositeTicks.Of(axes.YAxis.MajorTicks.Mirror, onAllSides, rightAxis));
    }
}
