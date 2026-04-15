// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>
/// Computes <see cref="SubPlotSpacing"/> margins based on actual text extents measured via
/// <see cref="IRenderContext.MeasureText"/>.  Called by <see cref="ChartRenderer"/> when
/// <c>TightLayout</c> or <c>ConstrainedLayout</c> is enabled.
/// </summary>
internal sealed class ConstrainedLayoutEngine
{
    // Padding constants (px) that buffer measured text against the plot-area boundary.
    private const double PadLeft   = 5;
    private const double PadBottom = 5;
    private const double PadTop    = 8;
    private const double PadRight  = 8;

    // Fixed offsets that CartesianAxesRenderer hard-codes (do NOT change here — mirrors the renderer).
    private const double YTickRightGap   = 8;   // PlotArea.X - 8 for right-aligned Y tick labels
    private const double XTickBottomGap  = 15;  // PlotArea.Y + Height + 15 for X tick label baseline
    private const double XLabelGap       = 6;   // gap between X tick-label cell and X-axis label cell
    private const double YLabelLeftPos   = 45;  // PlotArea.X - 45 for Y axis label centre

    /// <summary>Computes optimal <see cref="SubPlotSpacing"/> margins for the figure.</summary>
    internal SubPlotSpacing Compute(Figure figure, IRenderContext ctx)
    {
        var theme = figure.Theme;

        double left   = figure.Spacing.MarginLeft;
        double bottom = figure.Spacing.MarginBottom;
        double top    = figure.Spacing.MarginTop;
        double right  = figure.Spacing.MarginRight;
        double hGap   = figure.Spacing.HorizontalGap;
        double vGap   = figure.Spacing.VerticalGap;

        int maxRows = GetMaxRows(figure);
        int maxCols = GetMaxCols(figure);

        // Figure-level suptitle reservation — stacked ABOVE any subplot title, not maxed
        // with it. A figure with both a suptitle and subplot titles needs space for both
        // to coexist, so we add the suptitle height on top of whatever the subplots need.
        double supReserved = 0;
        if (figure.Title is not null)
        {
            double supH = ctx.MeasureText(figure.Title, ThemedFontProvider.SupTitleFont(theme)).Height;
            supReserved = supH + SupTitleTopPad + SupTitleBottomPad;
        }

        foreach (var axes in figure.SubPlots)
        {
            var m   = Measure(axes, ctx, theme);
            var pos = GetEffectivePosition(axes, figure);

            // Only edge subplots contribute to the margin on that edge
            if (pos.ColStart == 0        && m.LeftNeeded   > left)   left   = m.LeftNeeded;
            if (pos.RowEnd   >= maxRows  && m.BottomNeeded > bottom) bottom = m.BottomNeeded;
            // Top row subplots: stack the suptitle reservation + the subplot's TopNeeded.
            if (pos.RowStart == 0        && supReserved + m.TopNeeded > top) top = supReserved + m.TopNeeded;
            if (pos.ColEnd   >= maxCols  && m.RightNeeded  > right)  right  = m.RightNeeded;

            // Interior subplots widen the inter-subplot gutter. The vertical gap between
            // two adjacent rows must accommodate BOTH the upper row's BottomNeeded (x-tick
            // labels + optional x-axis label) AND the lower row's TopNeeded (subplot title).
            // Same logic for horizontal gaps.
            if (pos.ColStart > 0 && m.LeftNeeded > hGap) hGap = m.LeftNeeded;
            if (pos.ColEnd   < maxCols && m.RightNeeded > hGap) hGap = m.RightNeeded;
            if (pos.RowStart > 0 && m.TopNeeded  > vGap) vGap = m.TopNeeded;
            if (pos.RowEnd   < maxRows && m.BottomNeeded > vGap) vGap = m.BottomNeeded;
        }

        // Stack the upper-row BottomNeeded with the lower-row TopNeeded — both must fit
        // inside vGap. We don't know which exact pair of rows are adjacent, so use the
        // worst-case sum: max non-bottom BottomNeeded + max non-top TopNeeded.
        double maxNonBottomBottom = 0, maxNonTopTop = 0;
        double maxNonRightRight = 0, maxNonLeftLeft = 0;
        foreach (var axes in figure.SubPlots)
        {
            var m = Measure(axes, ctx, theme);
            var pos = GetEffectivePosition(axes, figure);
            if (pos.RowEnd   < maxRows && m.BottomNeeded > maxNonBottomBottom) maxNonBottomBottom = m.BottomNeeded;
            if (pos.RowStart > 0       && m.TopNeeded    > maxNonTopTop)       maxNonTopTop = m.TopNeeded;
            if (pos.ColEnd   < maxCols && m.RightNeeded  > maxNonRightRight)   maxNonRightRight = m.RightNeeded;
            if (pos.ColStart > 0       && m.LeftNeeded   > maxNonLeftLeft)     maxNonLeftLeft = m.LeftNeeded;
        }
        double stackedV = maxNonBottomBottom + maxNonTopTop;
        double stackedH = maxNonRightRight + maxNonLeftLeft;
        if (stackedV > vGap) vGap = stackedV;
        if (stackedH > hGap) hGap = stackedH;

        // If the suptitle reservation is larger than whatever top-row subplots asked for,
        // honour it as the floor (handles figures with a suptitle but no subplot titles).
        if (supReserved > top) top = supReserved;

        // Upper-clamp bounds — the defaults are tuned for figures without outside legends.
        // When an outside legend is present on a given edge, the corresponding clamp has to
        // allow for the full legend width/height; otherwise the legend clips at the figure
        // boundary and the whole point of the reservation is defeated.
        double maxLeft = 120, maxBottom = 100, maxTop = 120, maxRight = 140;
        foreach (var axes in figure.SubPlots)
        {
            if (!axes.Legend.Visible) continue;
            if (!LegendMeasurer.IsOutsidePosition(axes.Legend.Position)) continue;
            var legendBox = LegendMeasurer.MeasureBox(axes, ctx, theme);
            if (legendBox.Width <= 0 || legendBox.Height <= 0) continue;
            switch (axes.Legend.Position)
            {
                case LegendPosition.OutsideRight:
                    maxRight = Math.Max(maxRight, (int)legendBox.Width + 40);
                    break;
                case LegendPosition.OutsideLeft:
                    maxLeft = Math.Max(maxLeft, (int)legendBox.Width + 40);
                    break;
                case LegendPosition.OutsideTop:
                    maxTop = Math.Max(maxTop, (int)legendBox.Height + 40);
                    break;
                case LegendPosition.OutsideBottom:
                    maxBottom = Math.Max(maxBottom, (int)legendBox.Height + 40);
                    break;
            }
        }

        return figure.Spacing with
        {
            MarginLeft     = Math.Clamp(left,   30, maxLeft),
            MarginBottom   = Math.Clamp(bottom, 30, maxBottom),
            MarginTop      = Math.Clamp(top,    20, maxTop),
            MarginRight    = Math.Clamp(right,  10, maxRight),
            HorizontalGap  = Math.Clamp(hGap,   0, 150),
            VerticalGap    = Math.Clamp(vGap,   0, 120),
        };
    }

    // Suptitle reservation constants — mirror ChartRenderer.RenderBackground padding.
    private const double SupTitleTopPad    = 8;
    private const double SupTitleBottomPad = 12;

    // ── Grid position helpers ────────────────────────────────────────────────

    /// <summary>Returns the effective grid cell range for the axes, normalised to 0-based row/col indices.</summary>
    private static GridPosition GetEffectivePosition(Axes axes, Figure figure)
    {
        // GridSpec-based positioning takes highest priority
        if (axes.GridPosition.HasValue)
            return axes.GridPosition.Value;

        // Legacy 1-based GridRows/GridCols/GridIndex (rows × cols grid, 1-based)
        if (axes.GridRows > 0 && axes.GridCols > 0 && axes.GridIndex > 0)
        {
            int zeroIdx = axes.GridIndex - 1;
            int row = zeroIdx / axes.GridCols;
            int col = zeroIdx % axes.GridCols;
            return new GridPosition(row, row + 1, col, col + 1);
        }

        // Fallback: treat as a single-cell 1×1 grid (touches all four edges)
        int maxRows = GetMaxRows(figure);
        int maxCols = GetMaxCols(figure);
        return new GridPosition(0, maxRows, 0, maxCols);
    }

    private static int GetMaxRows(Figure figure)
    {
        if (figure.GridSpec is { Rows: > 0 } gs) return gs.Rows;
        int max = 1;
        foreach (var ax in figure.SubPlots)
        {
            if (ax.GridPosition.HasValue && ax.GridPosition.Value.RowEnd > max)
                max = ax.GridPosition.Value.RowEnd;
            else if (ax.GridRows > max)
                max = ax.GridRows;
        }
        return max;
    }

    private static int GetMaxCols(Figure figure)
    {
        if (figure.GridSpec is { Cols: > 0 } gs) return gs.Cols;
        int max = 1;
        foreach (var ax in figure.SubPlots)
        {
            if (ax.GridPosition.HasValue && ax.GridPosition.Value.ColEnd > max)
                max = ax.GridPosition.Value.ColEnd;
            else if (ax.GridCols > max)
                max = ax.GridCols;
        }
        return max;
    }

    private LayoutMetrics Measure(Axes axes, IRenderContext ctx, Theme theme)
    {
        // All themed fonts come from ThemedFontProvider — the single source of truth shared
        // with AxesRenderer / CartesianAxesRenderer / LegendMeasurer. Pre-1.2.1 this engine
        // maintained its own duplicate factories with a drifted -2 tick-font size formula,
        // which caused the outside-legend clipping bug. The provider guarantees measurer
        // and renderer cannot disagree.
        var tickFont  = ThemedFontProvider.TickFont(theme);
        var labelFont = ThemedFontProvider.LabelFont(theme);
        var titleFont = ThemedFontProvider.TitleFont(theme);

        // --- Left margin: Y-tick labels + optional Y-axis label ---
        // Use the same dynamic formula CartesianAxesRenderer.RenderAxisLabels uses, so the
        // layout reserves exactly the space the renderer needs (no clipping, no waste).
        // Tick mark length + tick label pad mirror Axis.MajorTicks defaults from the active theme.
        var yMajor = axes.YAxis.MajorTicks;
        string yTickProbe = EstimateYTickLabel(axes);
        double maxYTickWidth = ctx.MeasureText(yTickProbe, tickFont).Width;
        double leftNeeded = yMajor.Length + yMajor.Pad + maxYTickWidth + PadLeft;

        if (axes.YAxis.Label is not null)
        {
            // Rotated y-label: its horizontal footprint is the LABEL HEIGHT (font size), not the text width.
            // Renderer formula: x = PlotArea.X - tickLength - tickPad - maxTickWidth - YLabelGap(12)
            // We add labelHeight + PadLeft so the rotated text fits inside the figure left margin.
            const double YLabelGap = 12;
            double labelHeight = ctx.MeasureText(axes.YAxis.Label, labelFont).Height;
            leftNeeded = yMajor.Length + yMajor.Pad + maxYTickWidth + YLabelGap + labelHeight + PadLeft;
        }

        // --- Bottom margin: X-tick labels + optional X-axis label ---
        // Mirrors AxesRenderer.RenderAxisLabels exactly:
        //   tickCellBottom = tickLength + tickPad + tickLabelHeight
        //   xLabelBaseline = tickCellBottom + gap + labelAscent (≈ 0.8 × labelFont.Size)
        //   xLabelBottom   = xLabelBaseline + labelDescent (≈ 0.2 × labelFont.Size)
        var xMajor = axes.XAxis.MajorTicks;
        double tickH = ctx.MeasureText("0", tickFont).Height;
        double tickCellBottom = xMajor.Length + xMajor.Pad + tickH;
        double bottomNeeded = XTickBottomGap + tickH + PadBottom;

        if (axes.XAxis.Label is not null)
        {
            double xLabelH = ctx.MeasureText(axes.XAxis.Label, labelFont).Height;
            double xLabelBottom = tickCellBottom + XLabelGap + xLabelH;
            bottomNeeded = Math.Max(bottomNeeded, xLabelBottom + PadBottom);
        }

        // --- Top margin: axes title + optional secondary X-axis label (TwinY) ---
        double topNeeded = 0;
        if (axes.Title is not null)
        {
            double titleH = ctx.MeasureText(axes.Title, titleFont).Height;
            topNeeded = titleH + PadTop + PadTop;  // gap above + gap below to plot edge
        }
        if (axes.SecondaryXAxis?.Label is not null)
        {
            // Secondary X label is drawn at PlotArea.Y - 28; add enough top margin
            double secLabelH = ctx.MeasureText(axes.SecondaryXAxis.Label, labelFont).Height;
            topNeeded = Math.Max(topNeeded, 28 + secLabelH + PadTop);
        }

        // --- Right margin: minimal; widen if secondary Y-axis label or vertical colorbar present ---
        double rightNeeded = 0;
        if (axes.SecondaryYAxis?.Label is not null)
        {
            double halfSecWidth = ctx.MeasureText(axes.SecondaryYAxis.Label, labelFont).Width / 2;
            rightNeeded = 45 + halfSecWidth + PadRight;
        }
        if (axes.ColorBar is { Visible: true, Orientation: ColorBarOrientation.Vertical } cb)
        {
            // Reserve: padding gap + bar width + 4-px gap + widest tick label + 8-px gap +
            // (rotated) label height + PadRight. Tick label width estimated from worst-case 8-char number.
            double tickLabelW = ctx.MeasureText("0.0000", tickFont).Width;
            double labelH = cb.Label is not null ? ctx.MeasureText(cb.Label, labelFont).Height : 0;
            double cbWidth = cb.Padding + cb.Width + 4 + tickLabelW + 8 + labelH + PadRight;
            if (cbWidth > rightNeeded) rightNeeded = cbWidth;
        }

        // --- Outside legend reservation: measure the legend box via the shared LegendMeasurer
        // (same formulas the renderer uses) and add its width/height to the appropriate edge
        // when the legend's Position is one of the Outside* values. This is the concrete
        // constrained-layout improvement over the previous engine, which ignored legend
        // geometry entirely and let outside legends fall off the figure. ---
        if (axes.Legend.Visible && LegendMeasurer.IsOutsidePosition(axes.Legend.Position))
        {
            var legendBox = LegendMeasurer.MeasureBox(axes, ctx, theme);
            if (legendBox.Width > 0 && legendBox.Height > 0)
            {
                const double LegendOuterGap = 16;  // 8 px past the spine + 8 px past the box
                switch (axes.Legend.Position)
                {
                    case LegendPosition.OutsideRight:
                        rightNeeded = Math.Max(rightNeeded, legendBox.Width + LegendOuterGap);
                        break;
                    case LegendPosition.OutsideLeft:
                        leftNeeded = Math.Max(leftNeeded, leftNeeded + legendBox.Width + LegendOuterGap);
                        break;
                    case LegendPosition.OutsideTop:
                        topNeeded = Math.Max(topNeeded, topNeeded + legendBox.Height + LegendOuterGap);
                        break;
                    case LegendPosition.OutsideBottom:
                        bottomNeeded = Math.Max(bottomNeeded, bottomNeeded + legendBox.Height + LegendOuterGap);
                        break;
                }
            }
        }

        return new LayoutMetrics(leftNeeded, bottomNeeded, topNeeded, rightNeeded);
    }

    /// <summary>Returns a representative Y-tick label string for width estimation.
    /// Uses the configured axis bounds when available; falls back to a worst-case proxy.</summary>
    private static string EstimateYTickLabel(Axes axes)
    {
        if (axes.YAxis.Max.HasValue)
            return AxesRenderer.FormatTickValue(axes.YAxis.Max.Value);
        if (axes.YAxis.Min.HasValue)
            return AxesRenderer.FormatTickValue(axes.YAxis.Min.Value);

        // Conservative fallback: 7-char string representative of "−9.999"
        return "\u22129.999";
    }
}
