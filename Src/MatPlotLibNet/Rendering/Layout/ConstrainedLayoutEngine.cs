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
    internal const double PadLeft  = 5;
    private const double PadBottom = 5;
    private const double PadTop    = 8;
    internal const double PadRight = 8;

    // The tight layout's floor below drawn X tick labels: 15 px for the mark and the pad, a label's height in the theme's
    // tick font, PadBottom. It was the estimate of the band before the band was measured (EdgeBands.Bottom); it stays a
    // floor so a figure whose labels fit keeps its margin, numbers and category names alike, and it is the estimate a
    // renderer that measures nothing below its bottom spine (polar, 3-D) is still laid out by.
    private const double XTickBottomGap  = 15;
    private const double XLabelGap       = AxisTickExtensions.XLabelGap;   // gap between X tick-label cell and X-axis label cell

    /// <summary>Computes optimal <see cref="SubPlotSpacing"/> margins for the figure: the sides first
    /// (<see cref="ComputeSides"/>), since what an axes draws past its left and right spines does not depend on its plot's
    /// width; then the top and the bottom (<see cref="ComputeEnds"/>), measured at the width each plot is drawn at
    /// (<see cref="ChartRenderer.SubPlotRects"/>), since the X tick labels turn when upright ones would collide at that
    /// width, and how far they reach below the plot (and above it, mirrored) follows.</summary>
    internal SubPlotSpacing Compute(Figure figure, IRenderContext ctx)
    {
        var sides = ComputeSides(figure, ctx);
        return ComputeEnds(figure, ctx, sides, ChartRenderer.SubPlotRects(figure, sides.MarginTop, sides));
    }

    /// <summary>The left and right margins and the horizontal gutter of <see cref="Compute"/>: each edge axes' need on its
    /// edge, an interior axes' need in the gutter (the worst-case left and right needs stacked), clamped to caps that
    /// measured content and an outside legend lift.</summary>
    /// <returns>The figure's spacing with <see cref="SubPlotSpacing.MarginLeft"/>, <see cref="SubPlotSpacing.MarginRight"/>
    /// and <see cref="SubPlotSpacing.HorizontalGap"/> settled.</returns>
    private SubPlotSpacing ComputeSides(Figure figure, IRenderContext ctx)
    {
        double left  = figure.Spacing.MarginLeft;
        double right = figure.Spacing.MarginRight;
        double hGap  = figure.Spacing.HorizontalGap;
        int maxCols = GetMaxCols(figure);

        // The widest measured right band (secondary axis + end labels) anywhere, and among the interior axes; the widest
        // measured left band (category names on the Y axis, with the title past them) in the first column, and right of
        // it. Interior subplots widen the gutter, which must hold both the left axes' right need and the right axes' left
        // need: we do not know which pair is adjacent, so the worst-case sum.
        double maxNonRightRight = 0, maxNonLeftLeft = 0;
        double maxBand = 0, maxNonRightBand = 0, maxLeftBand = 0, maxNonLeftBand = 0;
        foreach (var axes in figure.SubPlots)
        {
            var m   = Measure(axes, figure, ctx, EdgeBandsOf(axes, figure, ctx));
            var pos = GetEffectivePosition(axes, figure);

            // Only edge subplots contribute to the margin on that edge
            if (pos.ColStart == 0       && m.LeftNeeded  > left)  left  = m.LeftNeeded;
            if (pos.ColEnd   >= maxCols && m.RightNeeded > right) right = m.RightNeeded;
            if (pos.ColStart > 0        && m.LeftNeeded  > hGap)  hGap  = m.LeftNeeded;
            if (pos.ColEnd   < maxCols  && m.RightNeeded > hGap)  hGap  = m.RightNeeded;
            if (pos.ColEnd   < maxCols  && m.RightNeeded > maxNonRightRight) maxNonRightRight = m.RightNeeded;
            if (pos.ColStart > 0        && m.LeftNeeded  > maxNonLeftLeft)   maxNonLeftLeft = m.LeftNeeded;
            maxBand = Math.Max(maxBand, m.RightBand);
            if (pos.ColEnd < maxCols) maxNonRightBand = Math.Max(maxNonRightBand, m.RightBand);
            if (pos.ColStart == 0) maxLeftBand = Math.Max(maxLeftBand, m.LeftBand);
            else maxNonLeftBand = Math.Max(maxNonLeftBand, m.LeftBand);
        }
        double stackedH = maxNonRightRight + maxNonLeftLeft;
        if (stackedH > hGap) hGap = stackedH;

        // Upper-clamp bounds — the defaults are tuned for figures without outside legends. Measured right-edge content
        // lifts the caps the way an outside legend does: the secondary axis and the end labels are drawn whatever the
        // margin, so a margin or gutter cut below them clips them; category names measured on the left edge lift the left
        // caps the same way. Without a measured band the caps stay as they were.
        double maxLeft = Math.Max(Math.Max(120, maxLeftBand), OutsideLegendCap(figure, ctx, LegendPosition.OutsideLeft));
        double maxRight = Math.Max(Math.Max(140, maxBand), OutsideLegendCap(figure, ctx, LegendPosition.OutsideRight));
        double maxHGap = maxNonRightBand > 0 || maxNonLeftBand > 0 ? Math.Max(150, stackedH) : 150;

        return figure.Spacing with
        {
            MarginLeft    = Math.Clamp(left,  30, maxLeft),
            MarginRight   = Math.Clamp(right, 10, maxRight),
            HorizontalGap = Math.Clamp(hGap,  0, maxHGap),
        };
    }

    /// <summary>The top and bottom margins and the row gap of <see cref="Compute"/>, measured with each axes' plot at the
    /// width it is drawn at (<paramref name="plots"/>, laid out under <paramref name="sides"/>): each edge axes' need on its
    /// edge (the suptitle stacked above the top row), an interior axes' need in the row gap (the worst-case upper bottom
    /// need and lower top need stacked), clamped to caps that measured content and an outside legend lift.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="sides">The spacing with its sides settled (<see cref="ComputeSides"/>).</param>
    /// <param name="plots">The plot area of each axes under <paramref name="sides"/>; only its X and width are read.</param>
    /// <returns><paramref name="sides"/> with <see cref="SubPlotSpacing.MarginTop"/>,
    /// <see cref="SubPlotSpacing.MarginBottom"/> and <see cref="SubPlotSpacing.VerticalGap"/> settled.</returns>
    private SubPlotSpacing ComputeEnds(Figure figure, IRenderContext ctx, SubPlotSpacing sides, List<Rect> plots)
    {
        double bottom = figure.Spacing.MarginBottom;
        double top    = figure.Spacing.MarginTop;
        double vGap   = figure.Spacing.VerticalGap;
        int maxRows = GetMaxRows(figure);

        // Figure-level suptitle reservation — stacked ABOVE any subplot title, not maxed
        // with it. A figure with both a suptitle and subplot titles needs space for both
        // to coexist, so we add the suptitle height on top of whatever the subplots need.
        double supReserved = 0;
        if (figure.Title is not null)
        {
            double supH = ctx.MeasureText(figure.Title, ThemedFontProvider.SupTitleFont(figure.Theme)).Height;
            supReserved = supH + SupTitleTopPad + SupTitleBottomPad;
        }

        // The tallest measured top band (with its title) in the top row (under the suptitle), and below it; the tallest
        // measured bottom need (the X tick labels, with the X axis title past them) in the last row, and above it. The
        // vertical gap between two adjacent rows must hold BOTH the upper row's bottom need (X tick labels and X axis
        // title) AND the lower row's top need (its title): we do not know which pair is adjacent, so the worst-case sum.
        double maxNonBottomBottom = 0, maxNonTopTop = 0;
        double maxTopBand = 0, maxNonTopTopBand = 0, maxBottomBand = 0, maxNonBottomBand = 0;
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var m   = Measure(axes, figure, ctx, EdgeBandsOf(axes, figure, ctx, plots[i]));
            var pos = GetEffectivePosition(axes, figure);

            if (pos.RowEnd   >= maxRows && m.BottomNeeded > bottom) bottom = m.BottomNeeded;
            // Top row subplots: stack the suptitle reservation + the subplot's TopNeeded.
            if (pos.RowStart == 0       && supReserved + m.TopNeeded > top) top = supReserved + m.TopNeeded;
            if (pos.RowStart > 0        && m.TopNeeded    > vGap) vGap = m.TopNeeded;
            if (pos.RowEnd   < maxRows  && m.BottomNeeded > vGap) vGap = m.BottomNeeded;
            if (pos.RowEnd   < maxRows  && m.BottomNeeded > maxNonBottomBottom) maxNonBottomBottom = m.BottomNeeded;
            if (pos.RowStart > 0        && m.TopNeeded    > maxNonTopTop)       maxNonTopTop = m.TopNeeded;
            if (m.TopBand > 0)
            {
                if (pos.RowStart == 0) maxTopBand = Math.Max(maxTopBand, supReserved + m.TopBand);
                else maxNonTopTopBand = Math.Max(maxNonTopTopBand, m.TopBand);
            }
            if (pos.RowEnd >= maxRows) maxBottomBand = Math.Max(maxBottomBand, m.BottomBand);
            else maxNonBottomBand = Math.Max(maxNonBottomBand, m.BottomBand);
        }
        double stackedV = maxNonBottomBottom + maxNonTopTop;
        if (stackedV > vGap) vGap = stackedV;

        // If the suptitle reservation is larger than whatever top-row subplots asked for,
        // honour it as the floor (handles figures with a suptitle but no subplot titles).
        if (supReserved > top) top = supReserved;

        // The measured top band (mirrored X tick labels or a secondary X axis) and the title above it lift the top caps,
        // and the X tick labels and title measured on the bottom edge the bottom caps, the way an outside legend does.
        double maxTop = Math.Max(Math.Max(120, maxTopBand), OutsideLegendCap(figure, ctx, LegendPosition.OutsideTop));
        double maxBottom = Math.Max(Math.Max(100, maxBottomBand), OutsideLegendCap(figure, ctx, LegendPosition.OutsideBottom));
        double maxVGap = maxNonTopTopBand > 0 || maxNonBottomBand > 0 ? Math.Max(120, stackedV) : 120;

        return sides with
        {
            MarginBottom = Math.Clamp(bottom, 30, maxBottom),
            MarginTop    = Math.Clamp(top,    20, maxTop),
            VerticalGap  = Math.Clamp(vGap,   0, maxVGap),
        };
    }

    /// <summary>How far an outside legend on <paramref name="edge"/> lifts that edge's cap: the widest (left, right) or
    /// tallest (top, bottom) legend box placed there, in whole pixels, plus 40 px; 0 when no visible legend with a box is
    /// placed there. Without it the legend would clip at the figure's edge and the reservation would be defeated.</summary>
    private static double OutsideLegendCap(Figure figure, IRenderContext ctx, LegendPosition edge)
    {
        double cap = 0;
        foreach (var axes in figure.SubPlots)
        {
            if (!axes.Legend.Visible || axes.Legend.Position != edge) continue;
            var legendBox = LegendMeasurer.MeasureBox(axes, ctx, figure.Theme);
            if (legendBox.Width <= 0 || legendBox.Height <= 0) continue;
            double extent = edge is LegendPosition.OutsideLeft or LegendPosition.OutsideRight ? legendBox.Width : legendBox.Height;
            cap = Math.Max(cap, (int)extent + 40);
        }
        return cap;
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

    /// <summary>What <paramref name="axes"/> needs on each edge, from the bands its renderer measured past its spines
    /// (<paramref name="bands"/>, <see cref="EdgeBandsOf"/>) and the estimates and legend boxes beside them.</summary>
    private LayoutMetrics Measure(Axes axes, Figure figure, IRenderContext ctx, EdgeBands bands)
    {
        var theme = figure.Theme;

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
        // The Y tick labels: the category names on the Y axis are measured by the renderer that draws them (bands.Left,
        // the TickLabelBand of the widest name); a numeric axis is estimated from a representative label with the same
        // band rule.
        var yMajor = axes.YAxis.MajorTicks;
        double yTickExtent = 0;
        if (yMajor.Visible)
        {
            yTickExtent = bands.Left > 0
                ? bands.Left
                : yMajor.TickLabelBand(ctx.MeasureText(EstimateYTickLabel(axes), tickFont).Width);
        }

        // A left-stacked outward secondary axis (ROADMAP.md row 101) adds past the primary Y's own band, the same
        // "a reservation that is too wide never clips" rule the right side's secondary axis follows — charged even
        // when a slot draws nothing (see SecondaryAxisBand's remarks).
        double leftSecondaryBand = LeftAxisBands(axes, ctx, theme).TotalStackedBand();
        double leftNeeded = LeftNeed(axes, ctx, theme, yTickExtent) + leftSecondaryBand;
        // leftNeeded already folds leftSecondaryBand in when category names drove a measured band; otherwise the
        // secondary band is still what was actually drawn there, so it alone counts.
        double leftBand = bands.Left > 0 ? leftNeeded : leftSecondaryBand;

        // --- Bottom margin: X-tick labels + optional X-axis label ---
        // What the axes draws below its bottom spine is measured by the renderer that draws it (bands.Bottom, the tick
        // labels; bands.BottomTitle, the X axis title past them) and reserved by BottomNeed, the rule the default path
        // applies too, never below the floor the tight layout has always kept under drawn tick labels. A renderer that
        // measures nothing there (polar, 3-D) keeps the estimate it was always laid out by: the Cartesian rule on an
        // estimated band.
        var xMajor = axes.XAxis.MajorTicks;
        double tickH = xMajor.Visible ? ctx.MeasureText("0", tickFont).Height : 0;
        double floor = xMajor.Visible ? XTickBottomGap + tickH + PadBottom : PadBottom;
        double bottomNeeded, bottomBand = 0;
        if (DrawsBelow(bands))
        {
            bottomNeeded = bottomBand = Math.Max(floor, BottomNeed(bands));
        }
        else
        {
            double tickCellBottom = xMajor.Visible ? xMajor.Length + xMajor.Pad + tickH : 0;
            bottomNeeded = floor;

            if (axes.XAxis.Label is not null)
            {
                double xLabelH = ctx.MeasureText(axes.XAxis.Label, labelFont).Height;
                double xLabelBottom = tickCellBottom + XLabelGap + xLabelH;
                bottomNeeded = Math.Max(bottomNeeded, xLabelBottom + PadBottom);
            }
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

        return new LayoutMetrics(leftNeeded, bottomNeeded, topNeeded, rightNeeded, rightBand, bands.Top > 0 ? titleBand : 0,
                                 leftBand, bottomBand);
    }

    /// <summary>Whether the renderer measured anything it draws below the bottom spine: X tick labels
    /// (<see cref="EdgeBands.Bottom"/>) or an X axis title (<see cref="EdgeBands.BottomTitle"/>). Then the bottom edge
    /// is reserved by <see cref="BottomNeed"/> in both layout paths.</summary>
    private static bool DrawsBelow(EdgeBands bands) => bands.Bottom > 0 || bands.BottomTitle > 0;

    /// <summary>The one rule for what the bottom edge of an axes needs, from what its renderer measured below the bottom
    /// spine: the far side of the X axis title when one is drawn (<see cref="EdgeBands.BottomTitle"/>: the tick label
    /// band, <see cref="AxisTickExtensions.XLabelGap"/>, the title's pad and its full height, or the title at its fixed
    /// offset when no tick label is drawn), otherwise the X tick label band (<see cref="EdgeBands.Bottom"/>); then
    /// <see cref="PadBottom"/>. Both layout paths reserve it: <see cref="Compute"/> and <see cref="ReserveEdgeBands"/>; the
    /// mirror of <see cref="LeftNeed"/>.</summary>
    /// <param name="bands">What the renderer measured past the spines.</param>
    /// <returns>The room the bottom edge needs.</returns>
    private static double BottomNeed(EdgeBands bands) =>
        (bands.BottomTitle > 0 ? bands.BottomTitle : bands.Bottom) + PadBottom;

    /// <summary>The one rule for what the left edge of <paramref name="axes"/> needs: the Y tick label band
    /// (<paramref name="yTickExtent"/>), then the rotated Y axis title, which the renderer centres 12 px past that band
    /// (<see cref="AxisTickExtensions.AxisLabelOffset"/>) and which is charged its full height (the safe direction), then
    /// <see cref="PadLeft"/>. Both layout paths reserve it: <see cref="Compute"/> always, <see cref="ReserveEdgeBands"/>
    /// when the band was measured (category names on the Y axis).</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The active theme (the label font).</param>
    /// <param name="yTickExtent">From the left spine to the far side of the Y tick labels; 0 when none are drawn.</param>
    /// <returns>The room the left edge needs.</returns>
    private static double LeftNeed(Axes axes, IRenderContext ctx, Theme theme, double yTickExtent)
    {
        if (axes.YAxis.Label is null) return yTickExtent + PadLeft;
        // Rotated y-label: its horizontal footprint is the LABEL HEIGHT (font size), not the text width.
        // Renderer formula: x = PlotArea.X - tickLength - tickPad - maxTickWidth - YLabelGap(12)
        // We add labelHeight + PadLeft so the rotated text fits inside the figure left margin.
        const double YLabelGap = 12;
        double labelHeight = ctx.MeasureText(axes.YAxis.Label, ThemedFontProvider.LabelFont(theme)).Height;
        return yTickExtent + YLabelGap + labelHeight + PadLeft;
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

    /// <summary>From the right spine to the far side of every right-side slot stacked together, as
    /// <c>CartesianSecondaryYAxisPart</c> draws them: tick labels left-aligned at <see cref="TickConfig.Length"/> +
    /// <see cref="TickConfig.Pad"/> past each slot's own edge (<see cref="AxisTickExtensions.TickLabelBand"/>), then
    /// the rotated title centred at <see cref="AxisTickExtensions.AxisLabelOffset"/> plus the label style's pad. The
    /// title is charged its full height past that centre (the rule the left margin uses for the primary title),
    /// which over-reserves a little: the safe direction. The total across every slot is <see cref="SecondaryAxisBands"/>
    /// summed (<see cref="AxisStackingExtensions.TotalStackedBand"/>).</summary>
    /// <remarks>A slot without secondary series draws nothing, but it is still charged: what it would draw is what
    /// the figure asked for, and a reservation that is too wide never clips. The end-label column
    /// (<c>CartesianDirectLabelsPart</c>) starts past this band, so labels never sit on the secondary ticks.</remarks>
    internal static double SecondaryAxisBand(Axes axes, IRenderContext ctx, Theme theme) =>
        SecondaryAxisBands(axes, ctx, theme).TotalStackedBand();

    /// <summary>The band of every right-side outward-stacked slot, nearest-to-the-plot (<see cref="Axes.SecondaryYAxis"/>,
    /// slot 0) first — <see cref="SecondaryAxisBand"/> sums these for the layout's total reservation, and
    /// <c>CartesianSecondaryYAxisPart</c> stacks them (<see cref="AxisStackingExtensions.StackedOffsets"/>) to place
    /// each slot's ticks past every slot before it. Empty when the axes has no right-side slot at all.</summary>
    internal static double[] SecondaryAxisBands(Axes axes, IRenderContext ctx, Theme theme) =>
        SlotBands(axes.RightYAxes, axes, ctx, theme);

    /// <summary>The band of every left-side outward-stacked slot, nearest-to-the-plot (past the primary <see cref="Axes.YAxis"/>)
    /// first — summed (<see cref="AxisStackingExtensions.TotalStackedBand"/>) for the left margin's extra reservation,
    /// stacked (<see cref="AxisStackingExtensions.StackedOffsets"/>) for each slot's own render-time outward offset.
    /// Empty when the axes has no left-side slot at all.</summary>
    internal static double[] LeftAxisBands(Axes axes, IRenderContext ctx, Theme theme) =>
        SlotBands(axes.LeftYAxes, axes, ctx, theme);

    private static double[] SlotBands(IReadOnlyList<Models.SecondaryAxisSlot> slots, Axes axes, IRenderContext ctx, Theme theme)
    {
        if (slots.Count == 0) return [];
        var renderer = new CartesianAxesRenderer(axes, default, ctx, theme);
        var bands = new double[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            // The same range, tick values, formatter and font the secondary part draws this slot with.
            var range = renderer.ComputeSecondaryDataRanges(0, 1, slot.Axis, slot.Series);
            bands[i] = SlotBand(slot.Axis, range, ctx, theme);
        }
        return bands;
    }

    private static double SlotBand(Axis axis, DataRange axisRange, IRenderContext ctx, Theme theme)
    {
        var ticks = axis.MajorTicks;
        double widest = 0, band = 0;
        if (ticks.Visible)
        {
            var values = AxesRenderer.ComputeTickValuesInternal(axisRange.YMin, axisRange.YMax);
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

    /// <summary>What <paramref name="axes"/> draws past its four spines, measured by the renderer the axes is drawn with
    /// (<see cref="AxesRenderer.MeasureEdgeBands"/>), the owner that draws it. The plot's height is not laid out yet, so
    /// the renderer is handed the height of the axes' grid cell, the upper bound of its plot height: the number of Y ticks
    /// depends on that height only below 240 px. Its width is the drawn one once the sides are settled
    /// (<paramref name="plot"/>, from <see cref="ChartRenderer.SubPlotRects"/>): the X tick labels turn when upright ones
    /// would collide at that width, so the top and bottom bands are read from this measurement. Before that (no
    /// <paramref name="plot"/>) the plot has no width, and only the left and right bands, which do not depend on it, are
    /// read.</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="figure">The figure it belongs to.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="plot">The plot area the axes is drawn in under the settled sides; only its X and width are used.</param>
    private static EdgeBands EdgeBandsOf(Axes axes, Figure figure, IRenderContext ctx, Rect? plot = null)
    {
        var pos = GetEffectivePosition(axes, figure);
        double cellHeight = figure.Height * (pos.RowEnd - pos.RowStart) / GetMaxRows(figure);
        var area = plot is { } drawn ? new Rect(drawn.X, 0, drawn.Width, cellHeight) : new Rect(0, 0, 0, cellHeight);
        return AxesRenderer.Create(axes, area, ctx, figure.Theme).MeasureEdgeBands();
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
    /// theme's spacing in pixels) widened so what is drawn past the right and the top spines, the category names measured
    /// past the left spine and the X tick labels and X axis title measured below the bottom spine, fit. An axes in the last
    /// column widens <see cref="SubPlotSpacing.MarginRight"/> to its <see cref="MeasureRightBand"/>; an axes left of
    /// another widens the gutter by its band, on top of the gutter the neighbour's Y tick labels already use. An axes with
    /// a top band
    /// (<see cref="EdgeBands.Top"/>: mirrored X tick labels or a drawn secondary X axis) in the top row widens
    /// <see cref="SubPlotSpacing.MarginTop"/> to the band and the title above it (the rule <see cref="Compute"/>
    /// applies); in a lower row it widens the row gap by the band, the distance its title rises. Category names on the Y
    /// axis (<see cref="EdgeBands.Left"/>) widen the left margin or the gutter to <see cref="LeftNeed"/>; what is drawn
    /// below the bottom spine (<see cref="EdgeBands.Bottom"/>, <see cref="EdgeBands.BottomTitle"/>) widens
    /// <see cref="SubPlotSpacing.MarginBottom"/> of an axes in the last row to <see cref="BottomNeed"/>, and the row gap
    /// below an axes in an upper row to that need plus the room the lower row's title takes (<see cref="TitleBand"/>). The
    /// sides are settled first (<see cref="ReserveSideBands"/>) and the top and bottom bands measured with each plot at the
    /// width it is drawn at (<see cref="ChartRenderer.SubPlotRects"/>), the order <see cref="Compute"/> follows: the X tick
    /// labels turn when upright ones would collide at that width. A figure whose resolved spacing already holds all of
    /// that gets <paramref name="resolved"/> back unchanged.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="resolved">The spacing the default path resolved, in pixels.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <returns>The spacing with room for the edge bands.</returns>
    internal SubPlotSpacing ReserveEdgeBands(Figure figure, SubPlotSpacing resolved, IRenderContext ctx)
    {
        var sides = ReserveSideBands(figure, resolved, ctx);
        var plots = ChartRenderer.SubPlotRects(figure, sides.MarginTop, sides);
        double top = resolved.MarginTop, vGap = resolved.VerticalGap;
        double bottom = resolved.MarginBottom, interiorBottomNeed = 0, lowerTopNeed = 0;
        int maxRows = GetMaxRows(figure);
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var pos = GetEffectivePosition(axes, figure);
            // Measured with the plot at the width it is drawn at: the X tick labels turn by it.
            var bands = EdgeBandsOf(axes, figure, ctx, plots[i]);
            // Above the top spine only the top band (the mirrored X tick labels or the secondary X axis) is new; without
            // it the resolved spacing stands.
            if (bands.Top > 0)
            {
                if (pos.RowStart == 0) top = Math.Max(top, TitleBand(axes, ctx, figure.Theme, bands.Top));
                else vGap = Math.Max(vGap, resolved.VerticalGap + bands.Top);
            }
            // Below the bottom spine the measured X tick labels and the X axis title past them (the theme's margin is a
            // fraction of the height and does not grow with them): the margin holds them, and a row gap holds them above
            // the lower row's title.
            if (DrawsBelow(bands))
            {
                double need = BottomNeed(bands);
                if (pos.RowEnd >= maxRows) bottom = Math.Max(bottom, need);
                else interiorBottomNeed = Math.Max(interiorBottomNeed, need);
            }
            if (pos.RowStart > 0) lowerTopNeed = Math.Max(lowerTopNeed, TitleBand(axes, ctx, figure.Theme, bands.Top));
        }
        if (interiorBottomNeed > 0) vGap = Math.Max(vGap, interiorBottomNeed + lowerTopNeed);
        return sides with { MarginTop = top, VerticalGap = vGap, MarginBottom = bottom };
    }

    /// <summary>The sides of <see cref="ReserveEdgeBands"/>, settled before its top and bottom: what an axes draws past its
    /// left and right spines does not depend on its plot's width. An axes in the last column widens
    /// <see cref="SubPlotSpacing.MarginRight"/> to its <see cref="MeasureRightBand"/>, one left of another the gutter by
    /// it; category names on the Y axis widen the left margin or the gutter to <see cref="LeftNeed"/>.</summary>
    /// <returns><paramref name="resolved"/> with its left and right margins and its horizontal gutter settled.</returns>
    private SubPlotSpacing ReserveSideBands(Figure figure, SubPlotSpacing resolved, IRenderContext ctx)
    {
        double right = resolved.MarginRight, hGap = resolved.HorizontalGap;
        double left = resolved.MarginLeft, interiorRightBand = 0, interiorLeftNeed = 0;
        int maxCols = GetMaxCols(figure);
        foreach (var axes in figure.SubPlots)
        {
            var pos = GetEffectivePosition(axes, figure);
            var bands = EdgeBandsOf(axes, figure, ctx);
            double band = MeasureRightBand(axes, ctx, figure.Theme, bands.Right).Width;
            if (pos.ColEnd >= maxCols) right = Math.Max(right, band);
            else
            {
                hGap = Math.Max(hGap, resolved.HorizontalGap + band);
                interiorRightBand = Math.Max(interiorRightBand, band);
            }
            // Left of the left spine only measured category names, or a left-stacked outward secondary axis
            // (ROADMAP.md row 101), are new (a plain numeric Y axis fits the resolved spacing): the margin holds
            // them and the title past them, and a gutter holds them right of its neighbour's band.
            double leftSecondary = LeftAxisBands(axes, ctx, figure.Theme).TotalStackedBand();
            if (bands.Left > 0 || leftSecondary > 0)
            {
                double need = LeftNeed(axes, ctx, figure.Theme, bands.Left) + leftSecondary;
                if (pos.ColStart == 0) left = Math.Max(left, need);
                else interiorLeftNeed = Math.Max(interiorLeftNeed, need);
            }
        }
        if (interiorLeftNeed > 0) hGap = Math.Max(hGap, interiorRightBand + interiorLeftNeed);
        return resolved with { MarginLeft = left, MarginRight = right, HorizontalGap = hGap };
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
