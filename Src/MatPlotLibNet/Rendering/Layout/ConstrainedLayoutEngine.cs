// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>
/// Computes <see cref="SubPlotSpacing"/> margins based on actual text extents measured via
/// <see cref="IRenderContext.MeasureText"/>.  Called by <see cref="ChartRenderer"/> when
/// <c>TightLayout</c> or <c>ConstrainedLayout</c> is enabled; the default layout path calls its right-edge rule
/// (<see cref="ReserveEdgeBands"/>) only.
/// </summary>
internal sealed class ConstrainedLayoutEngine
{
    // Padding constants (px) that buffer measured text against the plot-area boundary.
    private const double PadLeft   = 5;
    private const double PadBottom = 5;
    private const double PadTop    = 8;
    internal const double PadRight = 8;

    // Fixed offsets that CartesianAxesRenderer hard-codes (do NOT change here — mirrors the renderer).
    private const double XTickBottomGap  = 15;  // PlotArea.Y + Height + 15 for X tick label baseline
    private const double XLabelGap       = 6;   // gap between X tick-label cell and X-axis label cell

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
            var m   = Measure(axes, figure, ctx);
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
        // The widest measured right band (secondary axis + end labels) anywhere, and among the interior axes; the
        // tallest measured top band (with its title) in the top row (under the suptitle), and below it.
        double maxBand = 0, maxNonRightBand = 0, maxTopBand = 0, maxNonTopTopBand = 0;
        foreach (var axes in figure.SubPlots)
        {
            var m = Measure(axes, figure, ctx);
            var pos = GetEffectivePosition(axes, figure);
            if (pos.RowEnd   < maxRows && m.BottomNeeded > maxNonBottomBottom) maxNonBottomBottom = m.BottomNeeded;
            if (pos.RowStart > 0       && m.TopNeeded    > maxNonTopTop)       maxNonTopTop = m.TopNeeded;
            if (pos.ColEnd   < maxCols && m.RightNeeded  > maxNonRightRight)   maxNonRightRight = m.RightNeeded;
            if (pos.ColStart > 0       && m.LeftNeeded   > maxNonLeftLeft)     maxNonLeftLeft = m.LeftNeeded;
            maxBand = Math.Max(maxBand, m.RightBand);
            if (pos.ColEnd < maxCols) maxNonRightBand = Math.Max(maxNonRightBand, m.RightBand);
            if (m.TopBand > 0)
            {
                if (pos.RowStart == 0) maxTopBand = Math.Max(maxTopBand, supReserved + m.TopBand);
                else maxNonTopTopBand = Math.Max(maxNonTopTopBand, m.TopBand);
            }
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
        // Measured right-edge content lifts the caps the way an outside legend does: the secondary axis and the end
        // labels are drawn whatever the margin, so a margin or gutter cut below them clips them. Without a measured
        // band the caps stay as they were.
        maxRight = Math.Max(maxRight, maxBand);
        double maxHGap = maxNonRightBand > 0 ? Math.Max(150, stackedH) : 150;
        // The measured top band (mirrored X tick labels or a secondary X axis) and the title above it lift the top caps
        // the same way.
        maxTop = Math.Max(maxTop, maxTopBand);
        double maxVGap = maxNonTopTopBand > 0 ? Math.Max(120, stackedV) : 120;
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
            HorizontalGap  = Math.Clamp(hGap,   0, maxHGap),
            VerticalGap    = Math.Clamp(vGap,   0, maxVGap),
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

    private LayoutMetrics Measure(Axes axes, Figure figure, IRenderContext ctx)
    {
        var theme = figure.Theme;
        // What is drawn past the top spine below the title and past the right spine by the mirrored ticks, measured by
        // the renderer that draws it.
        var bands = EdgeBandsOf(axes, figure, ctx);

        // All themed fonts come from ThemedFontProvider — the single source of truth shared
        // with AxesRenderer / CartesianAxesRenderer / LegendMeasurer. Pre-1.2.1 this engine
        // maintained its own duplicate factories with a drifted -2 tick-font size formula,
        // which caused the outside-legend clipping bug. The provider guarantees measurer
        // and renderer cannot disagree.
        var tickFont  = ThemedFontProvider.TickFont(theme);
        var labelFont = ThemedFontProvider.LabelFont(theme);

        // --- Left margin: Y-tick labels + optional Y-axis label ---
        // Use the same dynamic formula CartesianAxesRenderer.RenderAxisLabels uses, so the
        // layout reserves exactly the space the renderer needs (no clipping, no waste).
        // Tick mark length + tick label pad mirror Axis.MajorTicks defaults from the active theme.
        // Hidden major ticks draw NOTHING in this margin (CartesianAxesRenderer skips mark AND label on
        // TickConfig.Visible=false), so an axes that hid them must not be charged for their width. The
        // reservation is not only a margin: Compute widens the inter-subplot GUTTER to the same number, so a
        // row of tiles paid it between every pair of cards. Measured 2026-09-01 on the nine-tile ops wall —
        // cards 218 pt wide with a 57,5 pt gutter against the 12 the template asked for.
        var yMajor = axes.YAxis.MajorTicks;
        double yTickExtent = 0;
        if (yMajor.Visible)
        {
            string yTickProbe = EstimateYTickLabel(axes);
            yTickExtent = yMajor.Length + yMajor.Pad + ctx.MeasureText(yTickProbe, tickFont).Width;
        }

        double leftNeeded = yTickExtent + PadLeft;

        if (axes.YAxis.Label is not null)
        {
            // Rotated y-label: its horizontal footprint is the LABEL HEIGHT (font size), not the text width.
            // Renderer formula: x = PlotArea.X - tickLength - tickPad - maxTickWidth - YLabelGap(12)
            // We add labelHeight + PadLeft so the rotated text fits inside the figure left margin.
            const double YLabelGap = 12;
            double labelHeight = ctx.MeasureText(axes.YAxis.Label, labelFont).Height;
            leftNeeded = yTickExtent + YLabelGap + labelHeight + PadLeft;
        }

        // --- Bottom margin: X-tick labels + optional X-axis label ---
        // Mirrors AxesRenderer.RenderAxisLabels exactly:
        //   tickCellBottom = tickLength + tickPad + tickLabelHeight
        //   xLabelBaseline = tickCellBottom + gap + labelAscent (≈ 0.8 × labelFont.Size)
        //   xLabelBottom   = xLabelBaseline + labelDescent (≈ 0.2 × labelFont.Size)
        var xMajor = axes.XAxis.MajorTicks;
        double tickH = xMajor.Visible ? ctx.MeasureText("0", tickFont).Height : 0;
        double tickCellBottom = xMajor.Visible ? xMajor.Length + xMajor.Pad + tickH : 0;
        double bottomNeeded = xMajor.Visible ? XTickBottomGap + tickH + PadBottom : PadBottom;

        if (axes.XAxis.Label is not null)
        {
            double xLabelH = ctx.MeasureText(axes.XAxis.Label, labelFont).Height;
            double xLabelBottom = tickCellBottom + XLabelGap + xLabelH;
            bottomNeeded = Math.Max(bottomNeeded, xLabelBottom + PadBottom);
        }

        // --- Top margin: the top band (the mirrored X tick labels, or the secondary X axis with its title) and the axes
        // title above it (TitleBand) ---
        double titleBand = TitleBand(axes, ctx, theme, bands.Top);
        double topNeeded = titleBand;

        // --- Right margin: the secondary Y axis or the mirrored Y tick labels, then the end labels, stacked
        // (MeasureRightBand); a vertical colorbar or an outside-right legend takes the edge instead when it needs more ---
        double rightBand = MeasureRightBand(axes, ctx, theme, bands.Right).Width;
        double rightNeeded = rightBand;
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

        return new LayoutMetrics(leftNeeded, bottomNeeded, topNeeded, rightNeeded, rightBand, bands.Top > 0 ? titleBand : 0);
    }

    /// <summary>The one rule for what the right edge of <paramref name="axes"/> draws past its right spine, measured
    /// with the functions the drawing uses: the axis side (the secondary Y axis with its tick marks, pad, widest tick
    /// label and title, or the mirrored primary Y tick labels) and, stacked after it, the end-of-line labels
    /// (<see cref="DirectLabelMetrics"/>). Both layout paths reserve it: <see cref="Compute"/> for tight and
    /// constrained layout, and <see cref="ReserveEdgeBands"/> for the default path.</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The active theme (fonts).</param>
    /// <param name="mirroredYTicks">The mirrored Y tick band the axes' renderer measured
    /// (<see cref="EdgeBands.Right"/>); 0 when none is drawn.</param>
    /// <returns>The bands; <see cref="RightBand.Width"/> is the room the edge needs.</returns>
    internal RightBand MeasureRightBand(Axes axes, IRenderContext ctx, Theme theme, double mirroredYTicks)
    {
        var labels = new DirectLabelMetrics(axes, theme, ctx);
        double widestLabel = labels.WidestLabel();
        return new RightBand(SecondaryAxisBand(axes, ctx, theme), mirroredYTicks,
                             widestLabel > 0 ? DirectLabelMetrics.Gap + widestLabel : 0);
    }

    /// <summary>From the right spine to the far side of the secondary Y axis, as
    /// <c>CartesianSecondaryYAxisPart</c> draws it: tick labels left-aligned at <see cref="TickConfig.Length"/> +
    /// <see cref="TickConfig.Pad"/> past the spine (<see cref="AxisTickExtensions.TickLabelBand"/>), then the rotated
    /// title centred at <see cref="AxisTickExtensions.AxisLabelOffset"/> plus the label style's pad. The title is
    /// charged its full height past that centre (the rule the left margin uses for the primary title), which
    /// over-reserves a little: the safe direction.</summary>
    /// <remarks>An axis without secondary series draws nothing, but it is still charged: what it would draw is what
    /// the figure asked for, and a reservation that is too wide never clips. The end-label column
    /// (<c>CartesianDirectLabelsPart</c>) starts past this band, so labels never sit on the secondary ticks.</remarks>
    internal static double SecondaryAxisBand(Axes axes, IRenderContext ctx, Theme theme)
    {
        if (axes.SecondaryYAxis is not { } axis) return 0;
        var ticks = axis.MajorTicks;
        double widest = 0, band = 0;
        if (ticks.Visible)
        {
            // The same range, tick values, formatter and font the secondary part draws with.
            var range = new CartesianAxesRenderer(axes, default, ctx, theme).ComputeSecondaryDataRanges(0, 1);
            var values = AxesRenderer.ComputeTickValuesInternal(range.YMin, range.YMax);
            var uniform = AxesRenderer.BuildUniformTickFormatter(values);
            var font = ticks.LabelFont(ThemedFontProvider.TickFont(theme));
            foreach (var value in values)
                widest = Math.Max(widest, ctx.MeasureText(axis.TickFormatter?.Format(value) ?? uniform(value), font).Width);
            band = ticks.TickLabelBand(widest);
        }
        if (axis.Label is not null)
        {
            var labelFont = ThemedFontProvider.LabelFont(theme);
            var font = axis.LabelStyle?.ApplyTo(labelFont) ?? labelFont;
            band = ticks.AxisLabelOffset(widest) + (axis.LabelStyle?.Pad ?? 0) + ctx.MeasureText(axis.Label, font).Height;
        }
        return band;
    }

    /// <summary>What <paramref name="axes"/> draws past the top spine below its title and past the right spine by its
    /// mirrored ticks, measured by the renderer the axes is drawn with (<see cref="AxesRenderer.MeasureEdgeBands"/>),
    /// the owner that draws it. The plot is not laid out yet, so the renderer is handed the height of the axes' grid
    /// cell, the upper bound of its plot height: the number of Y ticks depends on that height only below 240 px.</summary>
    private static EdgeBands EdgeBandsOf(Axes axes, Figure figure, IRenderContext ctx)
    {
        var pos = GetEffectivePosition(axes, figure);
        double cellHeight = figure.Height * (pos.RowEnd - pos.RowStart) / GetMaxRows(figure);
        return AxesRenderer.Create(axes, new Rect(0, 0, 0, cellHeight), ctx, figure.Theme).MeasureEdgeBands();
    }

    /// <summary>From the top spine to the top of the axes title, as <c>AxesRenderer.RenderTitle</c> draws it: the
    /// title stands <see cref="PadTop"/> above the top band (<paramref name="topBand"/>: the mirrored X tick labels or
    /// the secondary X axis, <see cref="EdgeBands.Top"/>) and the margin keeps <see cref="PadTop"/> above the title.
    /// Without a title, the band and one pad; 0 when neither is drawn.</summary>
    private static double TitleBand(Axes axes, IRenderContext ctx, Theme theme, double topBand)
    {
        if (axes.Title is null) return topBand > 0 ? topBand + PadTop : 0;
        return topBand + ctx.MeasureText(axes.Title, ThemedFontProvider.TitleFont(theme)).Height + PadTop + PadTop;
    }

    /// <summary>The default layout path's share of the edge rules: <paramref name="resolved"/> (the figure's or the
    /// theme's spacing in pixels) widened so what is drawn past the right and the top spines fits. An axes in the last
    /// column widens <see cref="SubPlotSpacing.MarginRight"/> to its <see cref="MeasureRightBand"/>; an axes left of
    /// another widens the gutter by its band, on top of the gutter the neighbour's Y tick labels already use. An axes
    /// with a top band (<see cref="EdgeBands.Top"/>: mirrored X tick labels or a drawn secondary X axis) in the top row
    /// widens <see cref="SubPlotSpacing.MarginTop"/> to the band and the title above it (the rule <see cref="Compute"/>
    /// applies); in a lower row it widens the row gap by the band, the distance its title rises. A figure with nothing
    /// past those spines gets <paramref name="resolved"/> back unchanged.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="resolved">The spacing the default path resolved, in pixels.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <returns>The spacing with room for the edge bands.</returns>
    internal SubPlotSpacing ReserveEdgeBands(Figure figure, SubPlotSpacing resolved, IRenderContext ctx)
    {
        double right = resolved.MarginRight, hGap = resolved.HorizontalGap;
        double top = resolved.MarginTop, vGap = resolved.VerticalGap;
        int maxCols = GetMaxCols(figure);
        foreach (var axes in figure.SubPlots)
        {
            var pos = GetEffectivePosition(axes, figure);
            var bands = EdgeBandsOf(axes, figure, ctx);
            double band = MeasureRightBand(axes, ctx, figure.Theme, bands.Right).Width;
            if (pos.ColEnd >= maxCols) right = Math.Max(right, band);
            else hGap = Math.Max(hGap, resolved.HorizontalGap + band);
            // Above the top spine only the top band (the mirrored X tick labels or the secondary X axis) is new; without
            // it the resolved spacing stands.
            if (bands.Top > 0)
            {
                if (pos.RowStart == 0) top = Math.Max(top, TitleBand(axes, ctx, figure.Theme, bands.Top));
                else vGap = Math.Max(vGap, resolved.VerticalGap + bands.Top);
            }
        }
        return resolved with { MarginRight = right, HorizontalGap = hGap, MarginTop = top, VerticalGap = vGap };
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
