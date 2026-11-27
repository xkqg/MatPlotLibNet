// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Immutable result of <see cref="LegendLayoutCalculator.Compute(MatPlotLibNet.Models.Legend, IReadOnlyList{string}, Rect)"/>: all the
/// pre-computed geometry a renderer needs to draw the legend (box dimensions,
/// fonts, column widths, per-entry X/Y offsets).
/// </summary>
/// <remarks>
/// Drawing the legend (<c>LegendRenderer.Draw</c>) and hit-testing it
/// (<c>LegendRenderer.ItemBounds</c>) both take their geometry from this
/// record, which <see cref="LegendLayoutCalculator"/> returns; there is exactly ONE
/// authoritative implementation of the formulas.
/// </remarks>
public sealed record LegendLayout(
    Font Font,
    Font TitleFont,
    double SwatchWidth,
    double SwatchHeight,
    double SwatchGap,
    double Padding,
    double LineHeight,
    int NCols,
    int NRows,
    double[] ColMaxWidths,
    double ColSpacingPx,
    double TotalContentWidth,
    double TitleHeight,
    double BoxWidth,
    double BoxHeight,
    double BoxX,
    double BoxY)
{
    /// <summary>Where each entry stands (<see cref="LegendSlot"/>), in entry order: the top of its row, the row laid out by its own text
    /// (<see cref="LegendBox.RowOf"/>) and the height it is hit-tested by. Empty for a layout made by hand, whose rows are
    /// <see cref="LineHeight"/> apart.</summary>
    internal LegendSlot[] Slots { get; init; } = [];

    /// <summary>The band the title takes (<see cref="LegendBox.TitleOf"/>): where its baseline stands below the padded top of the box.</summary>
    internal LegendRow Title { get; init; }

    /// <summary>How far right of the padded left of the box the first column starts: half what a title wider than the entries
    /// adds to the box (<see cref="LegendBox.InnerWidthOf"/>), 0 otherwise.</summary>
    internal double ContentOffset { get; init; }

    /// <summary>The box rule the layout was made by, which says which column and row each entry takes.</summary>
    internal LegendBox Box { get; init; } = LegendBox.Library;

    /// <summary>How many entries the layout was made for.</summary>
    internal int Count { get; init; }

    /// <summary>The column and row of entry <paramref name="index"/> (<see cref="LegendBox.CellOf"/>).</summary>
    /// <param name="index">The entry's place in the list.</param>
    /// <returns>The column and the row of the entry.</returns>
    internal LegendCell CellOf(int index) => Box.CellOf(index, Count, NCols);

    /// <summary>Returns the X pixel offset of the given column (0-indexed).</summary>
    /// <param name="col">The column, from 0.</param>
    /// <returns>The X of the left edge of the column, in pixels.</returns>
    public double ColumnX(int col)
    {
        double colX = BoxX + Padding + ContentOffset;
        for (int c = 0; c < col; c++)
            colX += SwatchWidth + SwatchGap + ColMaxWidths[c] + ColSpacingPx;
        return colX;
    }

    /// <summary>Returns the Y pixel offset of the given row (0-indexed): the top of the entry in that row of the first column, which
    /// holds an entry in every row of the legend. Each entry's row is as tall as its own text, so a row of another column may stand
    /// elsewhere. A row the layout holds no entry for (past the last, or any row of a layout made by hand, which
    /// holds none) stands where rows <see cref="LineHeight"/> apart below the title put it.</summary>
    /// <param name="row">The row, from 0.</param>
    /// <returns>The Y of the top of the row, in pixels.</returns>
    public double EntryY(int row)
    {
        for (int i = 0; i < Slots.Length; i++)
            if (CellOf(i) is { Column: 0 } cell && cell.Row == row) return Slots[i].Top;
        return BoxY + Padding + TitleHeight + row * LineHeight;
    }

    /// <summary>Returns the hit-test width (swatch + gap + column-label) for the given column.</summary>
    public double ItemWidth(int col) => SwatchWidth + SwatchGap + ColMaxWidths[col];
}
