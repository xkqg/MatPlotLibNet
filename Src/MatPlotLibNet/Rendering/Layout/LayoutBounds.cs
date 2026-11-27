// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>The bounds a tight or a constrained layout holds its four margins and its two gaps in (<see cref="MarginBounds"/>), a part
/// of its pads (<see cref="LayoutPads.Bounds"/>), held by reference to one of its few values so the pads that carry it stay small. A
/// direction reads its own through the Along members: the left and the right margin and the gap between columns across the grid, the
/// top and the bottom margin and the gap between rows down it.</summary>
/// <param name="Margins">The bounds of the margin at each edge of the figure.</param>
/// <param name="ColumnGap">The bounds of the gap between two columns.</param>
/// <param name="RowGap">The bounds of the gap between two rows.</param>
internal sealed record LayoutBounds(Sides<MarginBounds> Margins, MarginBounds ColumnGap, MarginBounds RowGap)
{
    /// <summary>The library's bounds: a left margin of 30 to 120 pixels, a bottom one of 30 to 100, a top one of 20 to 120, a right one
    /// of 10 to 140, a gap between columns of at most 150 and one between rows of at most 120.</summary>
    internal static LayoutBounds Library { get; } = new(
        new Sides<MarginBounds>(Left: new(30, 120), Bottom: new(30, 100), Top: new(20, 120), Right: new(10, 140)),
        ColumnGap: new(0, 150), RowGap: new(0, 120));

    /// <summary>No bound on any margin or gap: each is what the axes need there, as matplotlib's <c>tight_layout</c> keeps them
    /// (<c>_auto_adjust_subplotpars</c>: a margin is what the axes draw past their position and the pad, a gap the same, with no floor
    /// and no cap).</summary>
    internal static LayoutBounds None { get; } = new(
        new Sides<MarginBounds>(MarginBounds.None, MarginBounds.None, MarginBounds.None, MarginBounds.None),
        ColumnGap: MarginBounds.None, RowGap: MarginBounds.None);
}
