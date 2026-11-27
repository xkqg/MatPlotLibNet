// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The mark one look of a tick draws, worked out once for the look and drawn at every tick that wears it
/// (<see cref="AxisTickExtensions"/>' <c>DrawTickMark</c>): its stroke, how far the drawn mark reaches out of the plot and into it (its
/// end at the edge half the spine past it, so it touches the spine's stroke), and how far matplotlib's marker reaches (from the edge,
/// where a surface with a pixel grid hangs it).</summary>
/// <param name="Stroke">The mark's colour and width, a solid line.</param>
/// <param name="Drawn">How far the drawn mark reaches out and in.</param>
/// <param name="Marker">How far matplotlib's marker reaches out and in, from the edge.</param>
internal readonly record struct TickMark(StrokeStyle Stroke, TickMarkSpan Drawn, TickMarkSpan Marker)
{
    /// <summary>The mark of <paramref name="look"/> on an edge whose spine is <paramref name="spineHalfWidth"/> half wide: in its colour,
    /// <paramref name="ink"/> when it names none, as wide and as long as it is, pointing its way (<see cref="TickMarkSpan.Of"/>).</summary>
    /// <param name="look">The tick's length, width, direction and colour.</param>
    /// <param name="ink">The colour of a mark whose look names none.</param>
    /// <param name="spineHalfWidth">Half the spine stroke on the edge.</param>
    /// <returns>The mark.</returns>
    internal static TickMark Of(TickConfig look, Color ink, double spineHalfWidth)
    {
        double length = look.Length;
        var direction = look.Direction;
        return new(StrokeStyle.Solid(look.Color ?? ink, look.Width), TickMarkSpan.Of(direction, length, spineHalfWidth),
            TickMarkSpan.Of(direction, length, 0));
    }
}
