// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The layout constants more than one renderer of the hierarchical and grid families reads: the smallest panel a
/// clustermap and a pair grid draw, the gap from a panel to the names beside it, and the dendrogram's offsets, which its renderer and
/// its frame read. A constant one renderer alone reads is that renderer's own (the treemap's header height, the
/// sunburst's ring inset).</summary>
internal static class HierarchicalLayout
{
    /// <summary>Minimum pixel width or height for a renderer sub-panel below which
    /// the renderer suppresses the panel to avoid sub-pixel noise. Shared by
    /// every composite renderer that subdivides plot bounds — <c>ClustermapSeriesRenderer</c>
    /// (dendrogram margin panels) and <c>PairGridSeriesRenderer</c> (per-cell sub-panels).</summary>
    public const double MinPanelPx = 4.0;

    /// <summary>Pixel gap between a panel and the names drawn beside it: a dendrogram's leaf labels, the names of a clustermap's
    /// rows and columns, the names of a pair grid's variables.</summary>
    public const double LabelGapPx = 6.0;

    /// <summary>Constants used by <c>DendrogramSeriesRenderer</c>.</summary>
    public static class Dendrogram
    {
        /// <summary>Pixel gap between the leaf-axis baseline and the leaf label: the gap every panel keeps to its names.</summary>
        public const double LabelOffsetPx = LabelGapPx;

        /// <summary>Stroke thickness of dendrogram U-shape segments.</summary>
        public const double LineThickness = 1.25;

    }
}

/// <summary>Where the label of a cell of a hierarchy (a treemap tile, an icicle cell) stands in it: inset <see cref="Pad"/> from the
/// left, hung <see cref="BaselineGap"/> below the cell's top, room for the text the width less the pad on each side, a measure hung a pad
/// under the name's baseline and, in the header strip of a treemap's interior tile, a name at the left and a measure at the right. A
/// text hung from a point stands on it by its box (<see cref="TextBox"/>), so no sum here assumes how far its letters rise.</summary>
/// <param name="Pad">The pixels between a cell's edge and its label.</param>
/// <param name="BaselineGap">The pixels the name hangs below the cell's top, and the room kept above the bottom of the cell.</param>
internal readonly record struct CellLabel(double Pad, double BaselineGap)
{
    /// <summary>The label every hierarchy of cells wears: four pixels in from the left, hung two below the top.</summary>
    internal static readonly CellLabel Standard = new(4, 2);

    /// <summary>Where the name of <paramref name="cell"/> stands, left-aligned: a pad from its left, hung the gap below its top.</summary>
    /// <param name="cell">The cell, in pixels.</param>
    /// <returns>The name's placement.</returns>
    internal TextPlacement Name(Rect cell) =>
        new(new Point(cell.X + Pad, cell.Y + BaselineGap), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top };

    /// <summary>How wide a label may be in <paramref name="cell"/>: its width less the pad on each side.</summary>
    /// <param name="cell">The cell, in pixels.</param>
    /// <returns>The room, in pixels.</returns>
    internal double RoomIn(Rect cell) => cell.Width - 2 * Pad;

    /// <summary>How tall a cell must be to hold a label of <paramref name="fontSize"/>: the size and a pad above and below.</summary>
    /// <param name="fontSize">The size of the label's font.</param>
    /// <returns>The height, in pixels.</returns>
    internal double HeightFor(double fontSize) => fontSize + 2 * Pad;

    /// <summary>Where the measure under the name of <paramref name="cell"/> stands, left-aligned at the pad: hung a pad under the
    /// name's baseline.</summary>
    /// <param name="cell">The cell, in pixels.</param>
    /// <param name="nameBaseline">The y of the name's baseline, where its box puts it.</param>
    /// <returns>The measure's placement.</returns>
    internal TextPlacement MeasureUnder(Rect cell, double nameBaseline) =>
        new(new Point(cell.X + Pad, nameBaseline + Pad), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top };

    /// <summary>Whether a measure on the baseline <paramref name="baseline"/> leaves the gap above the bottom of the cell.</summary>
    /// <param name="cell">The cell, in pixels.</param>
    /// <param name="baseline">The y of its baseline.</param>
    /// <returns>Whether it fits.</returns>
    internal bool MeasureFits(Rect cell, double baseline) => baseline <= cell.Y + cell.Height - BaselineGap;

    /// <summary>Where the name in the header strip of an interior cell starts: a pad from the left, a pad above the strip's bottom.</summary>
    /// <param name="cell">The cell, in pixels.</param>
    /// <param name="headerHeight">The height of the header strip.</param>
    /// <returns>The start of the name's baseline.</returns>
    internal Point HeaderName(Rect cell, double headerHeight) => new(cell.X + Pad, cell.Y + headerHeight - Pad);

    /// <summary>Where the measure in the header strip of an interior cell ends, right-aligned: a pad from the right, on the name's baseline.</summary>
    /// <param name="cell">The cell, in pixels.</param>
    /// <param name="headerHeight">The height of the header strip.</param>
    /// <returns>The end of the measure's baseline.</returns>
    internal Point HeaderMeasure(Rect cell, double headerHeight) => new(cell.X + cell.Width - Pad, cell.Y + headerHeight - Pad);

    /// <summary>Whether a header's measure and name stand side by side: both widths and a pad on each side of each fit the cell.</summary>
    /// <param name="measureWidth">The width of the measure.</param>
    /// <param name="nameWidth">The width of the name.</param>
    /// <param name="cell">The cell, in pixels.</param>
    /// <returns>Whether the pair fits.</returns>
    internal bool HeaderPairFits(double measureWidth, double nameWidth, Rect cell) => measureWidth + nameWidth + 4 * Pad <= cell.Width;
}
