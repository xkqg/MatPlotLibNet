// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Computes the pixel geometry of a legend (fonts, column widths, box
/// dimensions, position) from a <see cref="Legend"/> config + label list +
/// plot area. Encapsulates the formulas that drawing and hit-testing
/// (<c>LegendRenderer.Draw</c> and <c>LegendRenderer.ItemBounds</c>) share.
/// </summary>
/// <remarks>
/// Drawing and hit-testing read one copy of the layout math, so a change to the
/// legend's geometry is made in exactly one place.
/// </remarks>
public sealed class LegendLayoutCalculator
{
    private readonly Theme _theme;
    private readonly IRenderContext _ctx;

    /// <summary>Constructs a calculator bound to the theme + render context it will
    /// query for font and text-measurement services.</summary>
    public LegendLayoutCalculator(Theme theme, IRenderContext ctx)
    {
        _theme = theme;
        _ctx = ctx;
    }

    /// <summary>Computes the full legend layout for the given inputs.</summary>
    /// <param name="legend">The Legend config (font size, title, position, n-cols, frame, etc.).</param>
    /// <param name="labels">The label strings for the entries to be laid out.</param>
    /// <param name="plotArea">The pixel-space plot-area rectangle (used for box positioning).</param>
    public LegendLayout Compute(Legend legend, IReadOnlyList<string> labels, Rect plotArea)
        => Compute(legend, labels, plotArea, MarkObstacles.None);

    /// <summary>Computes the full legend layout knowing the marks the chart drew, which a legend at
    /// <see cref="LegendPosition.Best"/> keeps clear of; every other position stands where it names whatever is there.</summary>
    /// <param name="legend">The Legend config (font size, title, position, n-cols, frame, etc.).</param>
    /// <param name="labels">The label strings for the entries to be laid out.</param>
    /// <param name="plotArea">The pixel-space plot-area rectangle (used for box positioning).</param>
    /// <param name="obstacles">The marks the chart drew on the axes.</param>
    /// <param name="drawnPast">What the axes draws past each edge of its plot, which a legend outside the plot stands its gap past
    /// (<see cref="Layout.AxesEdgeNeeds.DrawnPast"/>); 0 on every edge for a legend of the figure.</param>
    /// <returns>The layout of the legend.</returns>
    internal LegendLayout Compute(Legend legend, IReadOnlyList<string> labels, Rect plotArea, MarkObstacles obstacles,
        Layout.Sides<double> drawnPast = default)
    {
        // The entries' font: the size the legend names, else the theme's legend size (ThemeFontExtensions.LegendFontOf)
        var font = _theme.LegendFontOf(legend);
        var look = _theme.Looks.Legend;
        var box = look.Box;

        // The handle and its gap to the text in ems of the legend's font (matplotlib's legend.handlelength, handleheight and
        // handletextpad), the frame's padding and the rows as the theme's box lays them out.
        double handleWidth  = font.Size * look.HandleLength * legend.MarkerScale;
        double handleHeight = font.Size * look.HandleHeight * legend.MarkerScale;
        double swatchGap    = font.Size * look.HandleTextPad;
        double padding = box.PaddingOf(font.Size);
        double labelSpacing = Math.Max(0, legend.LabelSpacing * font.Size);

        int nCols = box.ColumnsOf(labels.Count, legend.NCols);
        int nRows = (int)Math.Ceiling((double)labels.Count / nCols);

        // Measure each entry through the text seam — parse mathtext so $\alpha$ decay measures as "α decay" — for the width of its
        // column and its own row (the box's RowOf), then stack the rows of each column labelspacing apart, top first (matplotlib's
        // VPacker per column): per column, the baseline of its first row below the column's top (none while it is empty), how far its
        // next row starts and how far its last row ends below it.
        var colMaxWidths = new double[nCols];
        var slots = new LegendSlot[labels.Count];
        var firstBaseline = new double?[nCols];
        var next = new double[nCols];
        var bottom = new double[nCols];
        var floor = FloorOf(box, font);
        for (int i = 0; i < labels.Count; i++)
        {
            int col = box.CellOf(i, labels.Count, nCols).Column;
            var extent = _ctx.ExtentOf(TextLabel.Of(labels[i]), font).AtLeast(floor);
            if (extent.Width > colMaxWidths[col]) colMaxWidths[col] = extent.Width;
            var row = box.RowOf(handleHeight, extent);
            double top = next[col];
            firstBaseline[col] ??= row.Baseline;
            bottom[col] = top + row.Height;
            next[col] = top + (row.Height + labelSpacing);
            slots[i] = new LegendSlot(top, row, box.ItemHeightOf(row, row.Height + labelSpacing));
        }

        // The columns stand on the baseline of their first rows (the HPacker of the columns, align 'baseline'): the entries are as tall
        // as the highest rise above that baseline and the deepest a column reaches below it, and a column whose first row rises less
        // than another's drops by the difference.
        double rise = 0;
        foreach (var baseline in firstBaseline) rise = Math.Max(rise, baseline ?? 0);
        double depth = 0;
        for (int c = 0; c < nCols; c++)
            if (firstBaseline[c] is { } baseline) depth = Math.Max(depth, bottom[c] - baseline);
        double entriesHeight = rise + depth;

        double colSpacingPx = legend.ColumnSpacing * font.Size;
        double totalContentWidth = handleWidth + swatchGap + colMaxWidths.Sum()
            + (nCols - 1) * (handleWidth + swatchGap + colSpacingPx);

        // The title at the size the legend names or the look's step above the tick labels, in the look's weight, measured through the
        // text seam, in the band the box gives it
        var titleFont = _theme.LegendTitleFontOf(legend);
        var titleExtent = string.IsNullOrEmpty(legend.Title) ? default : _ctx.ExtentOf(TextLabel.Of(legend.Title), titleFont).AtLeast(FloorOf(box, titleFont));
        var title = string.IsNullOrEmpty(legend.Title) ? default : box.TitleOf(titleExtent, labelSpacing);
        double innerWidth = LegendBox.InnerWidthOf(totalContentWidth, titleExtent.Width);

        double boxWidth = padding + innerWidth + padding;
        double boxHeight = padding + title.Height + entriesHeight + padding;

        // Polymorphic position dispatch (Phase B.2), inset as the box says
        var (boxX, boxY) = LegendPositionStrategyFactory
            .Create(legend.PositionUnder(_theme))
            .Place(plotArea, new Size(boxWidth, boxHeight), box.InsetOf(font.Size), obstacles, drawnPast);

        // Each row's top, from the top of the entries: the column's place on the baseline the first rows share, and the row's in its
        // column
        double entriesTop = boxY + padding + title.Height;
        for (int i = 0; i < slots.Length; i++)
            slots[i] = slots[i] with { Top = entriesTop + (rise - (firstBaseline[box.CellOf(i, labels.Count, nCols).Column] ?? 0)) + slots[i].Top };

        // The pitch of the first row (its row and a label spacing), which a layout made by hand puts every row apart by
        double lineHeight = (slots.Length > 0 ? slots[0].Row.Height : box.RowOf(handleHeight, default).Height) + labelSpacing;

        return new LegendLayout(
            Font: font,
            TitleFont: titleFont,
            SwatchWidth: handleWidth,
            SwatchHeight: handleHeight,
            SwatchGap: swatchGap,
            Padding: padding,
            LineHeight: lineHeight,
            NCols: nCols,
            NRows: nRows,
            ColMaxWidths: colMaxWidths,
            ColSpacingPx: colSpacingPx,
            TotalContentWidth: totalContentWidth,
            TitleHeight: title.Height,
            BoxWidth: boxWidth,
            BoxHeight: boxHeight,
            BoxX: boxX,
            BoxY: boxY)
        {
            Slots = slots,
            Title = title,
            ContentOffset = Range1D.CentredIn(0, innerWidth, totalContentWidth).Lo,
            Box = box,
            Count = labels.Count,
        };
    }

    /// <summary>The extent every text of the legend set in <paramref name="font"/> is boxed at least as tall as: the box's
    /// <see cref="LegendBox.LineFloor"/> measured through the text seam, or <see langword="null"/> for a box that has none.</summary>
    /// <param name="box">The legend's box.</param>
    /// <param name="font">The font of the text.</param>
    /// <returns>The floor, or <see langword="null"/>.</returns>
    private TextExtent? FloorOf(LegendBox box, Font font) => box.LineFloor is { } text ? _ctx.ExtentOf(TextLabel.Of(text), font) : null;

    /// <summary>Measures the legend box of this axes, returning zero if the legend is hidden
    /// or has no row (no labelled series or patch). The returned width / height are already in pixels and
    /// include frame padding on all sides.</summary>
    /// <remarks>
    /// The size is the one this calculator lays the box out at, the calculator the draw pass and the
    /// hit-test read as well, so the margin an outside legend reserves and the box that is drawn can not drift apart: if
    /// they did, outside legends would either clip (layout under-reserved) or leave a gap (layout over-reserved). The
    /// calculator builds the legend font itself from the theme, so a caller cannot pass a size other than the one the
    /// legend is drawn at: measuring with a font two points smaller than the drawn one under-reports a 4-entry legend by
    /// about 20 px, enough to clip an outside legend.
    /// </remarks>
    /// <param name="ctx">Render context used for <see cref="IRenderContext.MeasureText"/>.</param>
    /// <param name="theme">The active theme — used to derive the same legend font
    /// <see cref="AxesRenderer.RenderLegend"/> will use at draw time.</param>
    /// <param name="axes">The <see cref="Axes"/> it works on.</param>
    internal static Size MeasureLegendBox(Axes axes, IRenderContext ctx, Theme theme)
    {
        if (!axes.Legend.Visible) return Size.Empty;

        // The SAME row list AxesRenderer.RenderLegend draws and ComputeLegendBounds hit-tests, asked of the renderer the factory
        // builds for these axes — the one that will draw them, which says whether an end-label series leaves the legend and
        // whether its patches have rows — so the margin is reserved for exactly the box that is drawn.
        var labels = AxesRenderer.Create(axes, default, ctx, theme).LegendEntries(axes.Legend.LegendValues).Select(e => e.Label).ToList();
        if (labels.Count == 0) return Size.Empty;

        // The box is the one the legend is laid out with, so what is reserved is what is drawn. Where it stands is not
        // asked here, only how big it is.
        var layout = new LegendLayoutCalculator(theme, ctx).Compute(axes.Legend, labels, default);
        return new Size(layout.BoxWidth, layout.BoxHeight);
    }
}
