// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Where the boxes of a box plot stand and how wide they are drawn where their author names no position and no width: the
/// rule of the library's own themes (<see cref="Library"/>), or matplotlib's <c>bxp</c> (<see cref="Matplotlib"/>), which stands the
/// boxes at 1, 2, ..., widens them with the span of their positions, ends the axis half a unit past the outer boxes and puts the
/// ticks at the boxes.</summary>
/// <param name="Datasets">Where the boxes stand when no position is named for them (<see cref="DatasetStanding"/>, the rule a violin
/// reads too).</param>
/// <param name="SpanShare">The share of the span between the outer positions a box is as wide as, held between
/// <paramref name="WidthLeast"/> and <paramref name="WidthMost"/> (matplotlib's <c>clip(0.15 * ptp(positions), 0.15, 0.5)</c>);
/// <see langword="null"/> draws every box <paramref name="WidthMost"/> wide.</param>
/// <param name="WidthLeast">The narrowest a box is drawn where it is as wide as a share of the span.</param>
/// <param name="WidthMost">The widest a box is drawn where it is as wide as a share of the span, and the width of every box where it is
/// not.</param>
/// <param name="EdgeReach">How far past the outer boxes the axis along the positions ends, in units of the positions, and as a sticky
/// edge that no margin pads (<c>bxp</c>: <c>min(positions) - 0.5</c>, <c>max(positions) + 0.5</c>); <see langword="null"/> ranges the
/// axis over the places the boxes are drawn at.</param>
/// <param name="TicksAtBoxes">Whether the ticks of the axis along the positions stand at the boxes (<c>bxp</c>: a <c>FixedLocator</c> at
/// the positions).</param>
internal sealed record BoxStanding(DatasetStanding Datasets, double? SpanShare, double WidthLeast, double WidthMost, double? EdgeReach, bool TicksAtBoxes)
{
    /// <summary>The library's own boxes: the first at 0, each 0.5 wide, the axis ranged over the places they are drawn at, the ticks
    /// where the axis puts them.</summary>
    internal static BoxStanding Library { get; } = new(DatasetStanding.Library, SpanShare: null, WidthLeast: 0.5, WidthMost: 0.5, EdgeReach: null, TicksAtBoxes: false);

    /// <summary>The boxes of matplotlib 3.11.2's <c>bxp</c>: the first at 1, a box 0.15 of the span between the outer positions wide,
    /// at least 0.15 and at most 0.5, the axis half a unit past the outer boxes, the ticks at the boxes.</summary>
    internal static BoxStanding Matplotlib { get; } = new(DatasetStanding.Matplotlib, SpanShare: 0.15, WidthLeast: 0.15, WidthMost: 0.5, EdgeReach: 0.5, TicksAtBoxes: true);

    /// <summary>Whether the width of a box given none follows the span of the positions, so the span is worth working out.</summary>
    internal bool WidthFollowsSpan => SpanShare is not null;

    /// <summary>The width of a box given none.</summary>
    /// <param name="positionSpan">The distance between the outer positions; not read where the width does not follow it
    /// (<see cref="WidthFollowsSpan"/>), so a position no axis can place leaves the width of the library's boxes as it is.</param>
    /// <returns>The width, in units of the positions.</returns>
    internal double WidthOver(double positionSpan) =>
        SpanShare is { } share ? Math.Clamp(share * positionSpan, WidthLeast, WidthMost) : WidthMost;
}
