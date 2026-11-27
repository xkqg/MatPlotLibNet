// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Configures the margins and gaps between subplots in a figure.</summary>
public sealed record SubPlotSpacing
{
    /// <summary>The space between the left edge of the figure and the subplots, in pixels. Default 60.</summary>
    public double MarginLeft { get; init; } = 60;

    /// <summary>The space between the right edge of the figure and the subplots, in pixels. Default 20.</summary>
    public double MarginRight { get; init; } = 20;

    /// <summary>The space between the top edge of the figure and the subplots, in pixels; a figure title that needs more
    /// room pushes the subplots further down. Default 40.</summary>
    public double MarginTop { get; init; } = 40;

    /// <summary>The space between the bottom edge of the figure and the subplots, in pixels. Default 50.</summary>
    public double MarginBottom { get; init; } = 50;

    /// <summary>The space between neighbouring columns of subplots, in pixels. Default 40.</summary>
    public double HorizontalGap { get; init; } = 40;

    /// <summary>The space between neighbouring rows of subplots, in pixels. Default 40.</summary>
    public double VerticalGap { get; init; } = 40;

    /// <summary>When true, the margins and gaps are worked out from the measured size of the tick labels, axis titles and
    /// legends instead of being taken from this record, so nothing is clipped and no space is wasted. Off by default.</summary>
    public bool TightLayout { get; init; }

    /// <summary>When true, the margins and gaps are worked out from the measured size of the tick labels, axis titles and
    /// legends instead of being taken from this record, so nothing overlaps or is clipped; inset axes are then placed within
    /// the inner data area, clear of the labels and ticks. Off by default.</summary>
    public bool ConstrainedLayout { get; init; }

    /// <summary>The rectangle the tight or the constrained layout fits the subplots, their labels and the figure's title into, in
    /// fractions of the figure (matplotlib's <c>rect</c> of <c>tight_layout</c>): the part of the figure outside it is kept free on
    /// top of the margins the layout works out. <see langword="null"/> (the default) is the whole figure; the default layout,
    /// whose margins are given, does not read it.</summary>
    public LayoutRect? LayoutRect { get; init; }

    /// <summary>The gap between neighbouring columns as a share of the average width of a column, as matplotlib's
    /// <c>figure.subplot.wspace</c> (0.2 in both its styles) takes it: with this set, <see cref="HorizontalGap"/> is worked out from
    /// the width the columns share (<c>share * width / (columns + share * (columns - 1))</c>) when the spacing is resolved for a grid
    /// (<see cref="Resolve(double, double, int, int)"/>), and the pixels it holds are the gap of a figure with no grid to work from.
    /// A tight or constrained layout sizes the gaps by what the axes draw and does not read it.
    /// <see langword="null"/> (the default) leaves the gap at <see cref="HorizontalGap"/>.</summary>
    public double? HorizontalGapFraction { get; init; }

    /// <summary>The gap between neighbouring rows as a share of the average height of a row, as matplotlib's
    /// <c>figure.subplot.hspace</c> takes it; the rows share the height the way <see cref="HorizontalGapFraction"/> describes for
    /// the columns. <see langword="null"/> (the default) leaves the gap at <see cref="VerticalGap"/>.</summary>
    public double? VerticalGapFraction { get; init; }

    // --- Fractional-margin support ---

    /// <summary>
    /// When <see langword="true"/> the Fract* fields hold fractional values (0–1) relative to the
    /// figure width/height.  Call <see cref="Resolve(double, double, int, int)"/> to convert to an absolute
    /// <see cref="SubPlotSpacing"/> before layout computation.
    /// </summary>
    public bool IsFractional { get; init; }

    /// <summary>The left margin as a fraction of the figure width, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractLeft   { get; init; }
    /// <summary>The right margin as a fraction of the figure width, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractRight  { get; init; }
    /// <summary>The top margin as a fraction of the figure height, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractTop    { get; init; }
    /// <summary>The bottom margin as a fraction of the figure height, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractBottom { get; init; }

    /// <summary>
    /// Creates a <see cref="SubPlotSpacing"/> whose margins are expressed as fractions of the
    /// figure dimensions.  Use this for themes that should scale with figure size (e.g. the
    /// Matplotlib themes: matplotlib's default style leaves margins of <c>left=0.125, right=0.1,
    /// top=0.12, bottom=0.11</c>, its classic style <c>left=0.125, right=0.1, top=0.1, bottom=0.1</c>). The gaps
    /// can be shares of the average axes too: <c>FromFractions(...) with { HorizontalGapFraction = 0.2 }</c>
    /// (<see cref="HorizontalGapFraction"/>).
    /// </summary>
    /// <param name="left">Left margin as a fraction of figure width (e.g. 0.125).</param>
    /// <param name="right">Right margin as a fraction of figure width (e.g. 0.10).</param>
    /// <param name="top">Top margin as a fraction of figure height (e.g. 0.12).</param>
    /// <param name="bottom">Bottom margin as a fraction of figure height (e.g. 0.12).</param>
    /// <param name="horizontalGap">Gap between columns in pixels (default 40).</param>
    /// <param name="verticalGap">Gap between rows in pixels (default 40).</param>
    /// <returns>A fractional <see cref="SubPlotSpacing"/> sentinel.</returns>
    public static SubPlotSpacing FromFractions(
        double left, double right, double top, double bottom,
        double horizontalGap = 40, double verticalGap = 40) =>
        new()
        {
            IsFractional  = true,
            FractLeft     = left,
            FractRight    = right,
            FractTop      = top,
            FractBottom   = bottom,
            HorizontalGap = horizontalGap,
            VerticalGap   = verticalGap,
        };

    /// <summary>
    /// Returns an absolute <see cref="SubPlotSpacing"/> with pixel margins computed from the
    /// figure dimensions, for a figure whose grid is not known: the gaps keep their pixel value
    /// (<see cref="HorizontalGapFraction"/> and <see cref="VerticalGapFraction"/> need the grid, <see cref="Resolve(double, double, int, int)"/>).
    /// A spacing that holds no fraction at all is returned unchanged.
    /// </summary>
    /// <param name="figureWidth">Figure width in pixels.</param>
    /// <param name="figureHeight">Figure height in pixels.</param>
    /// <returns>Absolute <see cref="SubPlotSpacing"/> ready for layout.</returns>
    public SubPlotSpacing Resolve(double figureWidth, double figureHeight) =>
        Resolve(figureWidth, figureHeight, rows: 1, columns: 1);

    /// <summary>
    /// Returns an absolute <see cref="SubPlotSpacing"/> in pixels for a figure whose subplots stand on a grid of
    /// <paramref name="rows"/> by <paramref name="columns"/>: fractional margins become pixels, and a gap that is a share of
    /// the average axes (<see cref="HorizontalGapFraction"/>, <see cref="VerticalGapFraction"/>) becomes the pixels matplotlib's
    /// <c>GridSpec</c> gives it, <c>share * room / (cells + share * (cells - 1))</c> of the room the margins leave. A spacing
    /// that holds neither a fraction nor a share is returned unchanged; a grid of one cell has no gap to work out, so the
    /// pixels stay.
    /// </summary>
    /// <param name="figureWidth">Figure width in pixels.</param>
    /// <param name="figureHeight">Figure height in pixels.</param>
    /// <param name="rows">How many rows the grid has.</param>
    /// <param name="columns">How many columns the grid has.</param>
    /// <returns>Absolute <see cref="SubPlotSpacing"/> ready for layout, holding pixels only.</returns>
    public SubPlotSpacing Resolve(double figureWidth, double figureHeight, int rows, int columns)
    {
        if (!IsFractional && HorizontalGapFraction is null && VerticalGapFraction is null) return this;

        // One record, whatever is resolved: a draw resolves the spacing, so no second copy is made on the way.
        double left = IsFractional ? Math.Round(figureWidth * FractLeft) : MarginLeft;
        double right = IsFractional ? Math.Round(figureWidth * FractRight) : MarginRight;
        double top = IsFractional ? Math.Round(figureHeight * FractTop) : MarginTop;
        double bottom = IsFractional ? Math.Round(figureHeight * FractBottom) : MarginBottom;
        return this with
        {
            IsFractional = false,
            MarginLeft = left,
            MarginRight = right,
            MarginTop = top,
            MarginBottom = bottom,
            HorizontalGap = GapOf(HorizontalGapFraction, figureWidth - left - right, columns, HorizontalGap),
            VerticalGap = GapOf(VerticalGapFraction, figureHeight - top - bottom, rows, VerticalGap),
            HorizontalGapFraction = null,
            VerticalGapFraction = null,
        };
    }

    /// <summary>The gap between <paramref name="cells"/> cells sharing <paramref name="room"/> when the gap is
    /// <paramref name="share"/> of the average cell: matplotlib's <c>cell = room / (cells + share * (cells - 1))</c>,
    /// <c>gap = share * cell</c>; <paramref name="resting"/> when no share is named or there is no gap to lay.</summary>
    private static double GapOf(double? share, double room, int cells, double resting) =>
        share is { } fraction && cells > 1 ? SeparationOf(fraction, room / (cells + fraction * (cells - 1))) : resting;

    /// <summary>The gap matplotlib's <c>GridSpec</c> lays between two neighbouring cells: <paramref name="share"/> of the mean cell
    /// (<c>sep_h = hspace * cell_h</c>). The one rule the gaps between subplots and the room between the parts of a broken axis are laid
    /// by.</summary>
    /// <param name="share">The gap as a share of the mean cell (<c>wspace</c>, <c>hspace</c>).</param>
    /// <param name="meanCell">The mean length of a cell, in the unit the gap is wanted in.</param>
    /// <returns>The gap.</returns>
    internal static double SeparationOf(double share, double meanCell) => share * meanCell;
}
