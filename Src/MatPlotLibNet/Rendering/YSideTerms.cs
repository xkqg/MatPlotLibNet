// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Everything that differs between a Y axis on the left of the plot and one on the right, worked out once from
/// its <see cref="AxisSide"/>: which edge it stands on and which way is out, how its tick marks hang and its tick labels
/// are anchored, which spine it meets and which series list numbers the series of a slot stacked past the primary. The one
/// part that draws every Y axis (<c>CartesianYAxisSlotPart</c>) reads these terms and never asks which side it is on, so a
/// right slot and a left slot are drawn as each other's mirror image.</summary>
/// <remarks>The mirrored primary Y ticks on the right spine are drawn by the <see cref="Right"/> terms too.</remarks>
internal readonly record struct YSideTerms
{
    /// <summary>The left side: the edge is the plot's left edge, out is towards smaller x, labels end at their
    /// anchor.</summary>
    internal static YSideTerms Left { get; } = new()
    {
        TickSide = TickSide.Left,
        Outward = -1,
        EdgeFraction = 0,
        LabelAlignment = TextAlignment.Right,
        Spine = spines => spines.Left,
        OutwardPair = SeriesAxisPair.SecondaryLeftY,
    };

    /// <summary>The right side: the edge is the plot's right edge, out is towards larger x, labels start at their
    /// anchor.</summary>
    internal static YSideTerms Right { get; } = new()
    {
        TickSide = TickSide.Right,
        Outward = 1,
        EdgeFraction = 1,
        LabelAlignment = TextAlignment.Left,
        Spine = spines => spines.Right,
        OutwardPair = SeriesAxisPair.SecondaryY,
    };

    /// <summary>The terms of <paramref name="side"/>: the one place a side is turned into how it is drawn.</summary>
    /// <param name="side">The side a Y axis stands on.</param>
    /// <returns><see cref="Left"/> or <see cref="Right"/>.</returns>
    internal static YSideTerms Of(AxisSide side) => side == AxisSide.Left ? Left : Right;

    /// <summary>The edge the tick marks hang from and the labels are placed beside.</summary>
    internal TickSide TickSide { get; init; }

    /// <summary>Which way is out, away from the plot, along x: -1 on the left, +1 on the right. A slot's edge and its
    /// title stand this way from the plot.</summary>
    internal double Outward { get; init; }

    /// <summary>Where the plot's own edge on this side is, as a fraction of the plot's width from its left edge: 0 on the
    /// left, 1 on the right.</summary>
    internal double EdgeFraction { get; init; }

    /// <summary>How a tick label is anchored, so it grows away from the plot.</summary>
    internal TextAlignment LabelAlignment { get; init; }

    /// <summary>The spine on this side, read from the axes' spines: the one a mark meets half a stroke into.</summary>
    internal Func<SpinesConfig, SpineConfig> Spine { get; init; }

    /// <summary>The series list that numbers the series of the slots stacked past the primary on this side
    /// (<see cref="SeriesSlotExtensions.SlotIndex"/>); the primary's own series are numbered by
    /// <see cref="SeriesAxisPair.Primary"/>.</summary>
    internal SeriesAxisPair OutwardPair { get; init; }

    /// <summary>The x of the edge of a slot standing <paramref name="offset"/> pixels out past the plot's edge on this
    /// side.</summary>
    /// <param name="plot">The plot area.</param>
    /// <param name="offset">How far out the slot stands: 0 for slot 0, the stacked bands before it otherwise.</param>
    /// <returns>The edge, in pixels.</returns>
    internal double EdgeX(Rect plot, double offset) => plot.X + EdgeFraction * plot.Width + Outward * offset;

    /// <summary>Hangs a tick drawing from an edge on this side: its tick side and label anchor, the edge, and half the
    /// stroke of the spine on this side.</summary>
    /// <param name="draw">How the ticks read (formatter, fonts, rotation).</param>
    /// <param name="edgeX">The x of the edge the ticks hang from.</param>
    /// <param name="spines">The axes' spines.</param>
    /// <returns>The drawing, hung from that edge.</returns>
    internal TickDrawContext Hang(TickDrawContext draw, double edgeX, SpinesConfig spines) => draw with
    {
        Side = TickSide,
        Alignment = LabelAlignment,
        AxisEdge = edgeX,
        SpineHalf = Spine(spines).LineWidth / 2.0,
    };
}
