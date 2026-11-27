// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>How far a tick mark reaches from the edge it hangs from: out of the plot and into it, by the way it points
/// (<see cref="Of"/>, the one reader of a <see cref="TickDirection"/>). The mark drawn, the marker matplotlib hangs from the edge and the
/// part of a mark outside the axes, where its label's pad starts (<see cref="TickConfig.OutsideLength"/>), all read it.</summary>
/// <param name="Outer">How far the mark reaches out of the plot, past the edge.</param>
/// <param name="Inner">How far the mark reaches into the plot.</param>
internal readonly record struct TickMarkSpan(double Outer, double Inner)
{
    /// <summary>The reach of a mark of <paramref name="length"/> pointing <paramref name="direction"/>: <see cref="TickDirection.Out"/>
    /// its length out of the plot, <see cref="TickDirection.In"/> its length into it, <see cref="TickDirection.InOut"/> half its length
    /// each way (matplotlib's <c>'|'</c> marker, centred on the edge). The end that meets the edge reaches
    /// <paramref name="atTheEdge"/> past it, so a drawn mark touches the spine's stroke (half its width) and a marker or the room of a
    /// label starts at the edge (0).</summary>
    /// <param name="direction">Which way the mark points.</param>
    /// <param name="length">The mark's length.</param>
    /// <param name="atTheEdge">How far the end that meets the edge reaches past it.</param>
    /// <returns>The reach.</returns>
    internal static TickMarkSpan Of(TickDirection direction, double length, double atTheEdge) => direction switch
    {
        TickDirection.In => new(atTheEdge, length),
        TickDirection.InOut => new(length / 2, length / 2),
        TickDirection.Out => new(length, atTheEdge),
        _ => new(length, length),
    };
}
