// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
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
    /// (<see cref="ComputeSides"/>), what an axes draws past its left and right spines that does not depend on its plot's
    /// width (the Y tick labels, the secondary axes, the end labels); then the room the X tick labels need beside their plot,
    /// which does depend on it, since they turn when upright ones would collide at the width the plot is drawn at and a turned
    /// label stands out sideways (<see cref="SettleSides"/>); then the top and the bottom (<see cref="ComputeEnds"/>), measured
    /// at the width each plot is drawn at (<see cref="ChartRenderer.SubPlotRects"/>), and how far the labels reach below the
    /// plot (and above it, mirrored) follows.</summary>
    internal SubPlotSpacing Compute(Figure figure, IRenderContext ctx)
    {
        var sides = ComputeSides(figure, ctx);
        var settled = SettleSides(figure, ctx, sides);
        return ComputeEnds(figure, ctx, settled.Sides, settled.Plots);
    }

    /// <summary>The sides together with the plot areas they give, once the room the X tick labels need beside their plot is
    /// settled in.</summary>
    /// <param name="Sides">The spacing with its sides widened for the labels that stand out sideways.</param>
    /// <param name="Plots">The plot area of each axes under <paramref name="Sides"/>.</param>
    private readonly record struct SettledSides(SubPlotSpacing Sides, List<Rect> Plots);

    /// <summary>How many times <see cref="SettleSides"/> measures at the plots the last measure gave before it stops: the
    /// labels turn, never un-turn, as the plot narrows, so the width converges in two or three.</summary>
    private const int MaxSettlePasses = 4;

    /// <summary>How close two measures of a margin must come for the sides to be settled, in pixels.</summary>
    private const double SettledWithin = 0.01;

    /// <summary>Settles the sides for what the X tick labels need beside their plot: the first label of a bottom axis, hung from
    /// its end and turned, stands out to the left of its plot, and the last stands out to the right of it, so the margin (or the
    /// gutter) holds that by the one rule (<see cref="WidenForOverhang"/>). Whether the labels turn depends on the width of the
    /// plot, and the plot's width on the sides, so the room is measured at the width the sides give and the sides given at first
    /// are widened by it; that narrows the plot, where a label that did not turn may now turn (the labels turn, never un-turn,
    /// as the plot narrows) and the ticks stand a little differently, so it is measured again at the width that remains, a
    /// bounded number of times (<see cref="MaxSettlePasses"/>), until two measures agree to within <see cref="SettledWithin"/>.
    /// The sides come back as they were when nothing stands out.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="sides">The spacing with its sides settled for what does not depend on the plot's width.</param>
    private SettledSides SettleSides(Figure figure, IRenderContext ctx, SubPlotSpacing sides)
    {
        var plots = ChartRenderer.SubPlotRects(figure, sides.MarginTop, sides);
        var current = sides;
        for (int pass = 0; pass < MaxSettlePasses; pass++)
        {
            var widened = WidenForOverhang(figure, ctx, sides, plots);
            if (widened == sides) return new SettledSides(sides, ChartRenderer.SubPlotRects(figure, sides.MarginTop, sides));
            bool settled = Math.Abs(widened.MarginLeft - current.MarginLeft) < SettledWithin
                && Math.Abs(widened.MarginRight - current.MarginRight) < SettledWithin
                && Math.Abs(widened.HorizontalGap - current.HorizontalGap) < SettledWithin;
            current = widened;
            plots = ChartRenderer.SubPlotRects(figure, current.MarginTop, current);
            if (settled) break;
        }
        return new SettledSides(current, plots);
    }

    /// <summary>The sides widened for the labels that stand out sideways past their plot, measured at the plots
    /// <paramref name="plots"/>: an axes in the first column widens the left margin to the overhang on its left and the pad, one
    /// in the last column the right margin to the overhang on its right and the pad, and an interior axes the gutter by the
    /// overhangs that meet in it (the worst case, as the gutter rule is). An overhang within the margin the sides already give
    /// changes nothing.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="sides">The sides to widen.</param>
    /// <param name="plots">The plot area of each axes under the sides the labels are measured at.</param>
    /// <returns>The sides, widened; <paramref name="sides"/> itself when nothing stands out past them.</returns>
    private SubPlotSpacing WidenForOverhang(Figure figure, IRenderContext ctx, SubPlotSpacing sides, List<Rect> plots)
    {
        double left = sides.MarginLeft, right = sides.MarginRight, hGap = sides.HorizontalGap;
        double interiorLeft = 0, interiorRight = 0;
        int maxCols = GetMaxCols(figure);
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var overhang = EdgeBandsOf(axes, figure, ctx, plots[i]).Overhang;
            if (overhang.Left <= 0 && overhang.Right <= 0) continue;
            var pos = GetEffectivePosition(axes, figure);
            if (pos.ColStart == 0) left = Math.Max(left, overhang.Left + PadLeft);
            else interiorLeft = Math.Max(interiorLeft, overhang.Left);
            if (pos.ColEnd >= maxCols) right = Math.Max(right, overhang.Right + PadRight);
            else interiorRight = Math.Max(interiorRight, overhang.Right);
        }
        hGap = Math.Max(hGap, interiorLeft + interiorRight);
        return left == sides.MarginLeft && right == sides.MarginRight && hGap == sides.HorizontalGap
            ? sides
            : sides with { MarginLeft = left, MarginRight = right, HorizontalGap = hGap };
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
        // The Y tick labels, numbers and category names alike, are measured by the renderer that draws them (bands.Left,
        // the TickLabelBand of the widest label); a renderer that measures none (polar, 3-D) is estimated from a
        // representative label with the same band rule.
        var yMajor = axes.YAxis.MajorTicks;
        double yTickExtent = 0;
        if (yMajor.Visible)
        {
            yTickExtent = bands.Left > 0
                ? bands.Left
                : yMajor.TickLabelBand(ctx.MeasureText(EstimateYTickLabel(axes), tickFont).Width);
        }

        // The whole left stack of the container: the primary, then every slot stacked past it, each charged even when it
        // draws nothing (see SecondaryAxisBand's remarks). What was measured there lifts the left cap.
        var leftSlots = LeftAxisBands(axes, ctx, theme);
        double leftNeeded = LeftNeed(axes, theme, yTickExtent, leftSlots);
        double leftBand = bands.Left > 0 || leftSlots.Length > 0 ? leftNeeded : 0;

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
            // (rotated) label height + PadRight. The widest label is the wider of a 6-char number (the estimate the bar was
            // laid out by) and the widest label the bar's ticks can have: the layout does not know how long the bar turns out
            // to be, so it takes the widest over every number of ticks the bar can ask for.
            var barRange = cb.RangeOf(axes.Series.ColorBarProvider());
            double tickLabelW = Math.Max(ctx.MeasureText("0.0000", tickFont).Width,
                ColorBarScale.WidestLabel(barRange.Min, barRange.Max, ctx, tickFont));
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

    /// <summary>The one rule for what the left edge of <paramref name="axes"/> needs: the left stack of its Y axis
    /// container, the band the renderer draws there. Slot 0 is the primary Y axis: its tick label band
    /// (<paramref name="primaryTickBand"/>) and its title by the one band rule (<see cref="YAxisTitleExtensions"/>, the
    /// rule its title is placed by, so the title's outward ink is reserved where it is drawn); then every slot stacked past
    /// it (<paramref name="outwardBands"/>), with the container's gap between each pair
    /// (<see cref="AxisStackingExtensions.TotalStackedBand"/>); then <see cref="PadLeft"/>. Both layout paths reserve it:
    /// <see cref="Compute"/> and <see cref="ReserveEdgeBands"/>.</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="theme">The active theme (the title font).</param>
    /// <param name="primaryTickBand">From the left spine to the far side of the primary's tick labels; 0 when none are
    /// drawn.</param>
    /// <param name="outwardBands">The band of every left slot past the primary, nearest the plot first
    /// (<see cref="LeftAxisBands"/>).</param>
    /// <returns>The room the left edge needs.</returns>
    private static double LeftNeed(Axes axes, Theme theme, double primaryTickBand, double[] outwardBands) =>
        LeftStackBand(axes, theme, primaryTickBand, outwardBands) + PadLeft;

    /// <summary>From the left spine to the far side of everything the whole left stack of the Y axis container draws: the
    /// primary's tick label band and its title by the one band rule (<see cref="YAxisTitleExtensions"/>), then every slot
    /// past it with the container's gap between each pair (<see cref="AxisStackingExtensions.TotalStackedBand"/>). 0 when
    /// nothing is drawn there (hidden tick labels, no title, no slot past the primary).</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="theme">The active theme (the title font).</param>
    /// <param name="primaryTickBand">From the left spine to the far side of the primary's tick labels; 0 when none are
    /// drawn.</param>
    /// <param name="outwardBands">The band of every left slot past the primary, nearest the plot first.</param>
    /// <returns>The band of the left stack, without the pad.</returns>
    private static double LeftStackBand(Axes axes, Theme theme, double primaryTickBand, double[] outwardBands)
    {
        double gap = axes.YAxes.TitleGap;
        double primary = axes.YAxis.YTitleExtent(
            new YAxisTitleInput(primaryTickBand, AxisSide.Left, gap, ThemedFontProvider.LabelFont(theme))).Band;
        double[] stack = [primary, .. outwardBands];
        return stack.TotalStackedBand(gap);
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

    /// <summary>From the right spine to the far side of every right slot of the container stacked together, as the slots
    /// draw them: each slot's band (<see cref="SecondaryAxisBands"/>) with the container's gap between each pair
    /// (<see cref="AxisStackingExtensions.TotalStackedBand"/>).</summary>
    /// <remarks>A slot without series draws nothing, but it is still charged: what it would draw is what the figure asked
    /// for, and a reservation that is too wide never clips. The end-label column (<c>CartesianDirectLabelsPart</c>) starts
    /// past this band, so labels never sit on the slots' ticks.</remarks>
    internal static double SecondaryAxisBand(Axes axes, IRenderContext ctx, Theme theme) =>
        SecondaryAxisBands(axes, ctx, theme).TotalStackedBand(axes.YAxes.TitleGap);

    /// <summary>The band of every right slot of the container, nearest the plot first (<see cref="SlotBand"/>):
    /// <see cref="SecondaryAxisBand"/> stacks these for the layout's reservation, and the renderer stacks them
    /// (<see cref="AxisStackingExtensions.StackedOffsets"/>) to place each slot past every slot before it. Empty when the
    /// axes has no right slot.</summary>
    internal static double[] SecondaryAxisBands(Axes axes, IRenderContext ctx, Theme theme) =>
        SlotBands(axes.YAxes.Right, axes, ctx, theme);

    /// <summary>The band of every left slot of the container past the primary, nearest the plot first
    /// (<see cref="SlotBand"/>): stacked after the primary's own band for the left margin (<see cref="LeftNeed"/>) and for
    /// each slot's render-time offset. Empty when the axes has no left slot past the primary.</summary>
    internal static double[] LeftAxisBands(Axes axes, IRenderContext ctx, Theme theme) =>
        SlotBands(axes.YAxes.Left.Skip(axes.YAxes.Left.OutwardOffset), axes, ctx, theme);

    private static double[] SlotBands(IEnumerable<YAxisSlot> slots, Axes axes, IRenderContext ctx, Theme theme) =>
        [.. slots.Select(slot => SlotBand(axes, slot, ctx, theme))];

    /// <summary>From one stacked slot's edge to the far side of what it draws, as its part draws it: the slot placed where
    /// the render places it (<see cref="CartesianAxesRenderer.PlaceSlot"/>: its range, its ticks) and measured by the band
    /// rule the render stacks it by (<see cref="CartesianAxesRenderer.SlotBand"/>). A slot's band does not depend on the
    /// X extent, so the slot is placed over 0..1. The plot's height is not known yet: the slot is placed beside a plot of
    /// <see cref="AxesRenderer.FullTickDensityLength"/>, so its ticks are the densest it can draw and what is reserved holds
    /// the labels of a shorter plot, whose ticks are a coarser set of the same values.</summary>
    /// <param name="axes">The axes the slot belongs to.</param>
    /// <param name="slot">The slot.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The active theme (fonts).</param>
    /// <returns>The slot's band.</returns>
    private static double SlotBand(Axes axes, YAxisSlot slot, IRenderContext ctx, Theme theme)
    {
        var renderer = new CartesianAxesRenderer(axes, new Rect(0, 0, 0, AxesRenderer.FullTickDensityLength), ctx, theme);
        return renderer.SlotBand(renderer.PlaceSlot(slot, new Numerics.MinMaxRange(0, 1)));
    }

    /// <summary>What <paramref name="axes"/> draws past its four spines, measured by the renderer the axes is drawn with
    /// (<see cref="AxesRenderer.MeasureEdgeBands"/>), the owner that draws it. The plot's height is not laid out yet, so
    /// the renderer is handed the height of the axes' grid cell, the upper bound of its plot height: the number of Y ticks
    /// depends on that height only below 240 px. Its width is the drawn one once the sides are settled
    /// (<paramref name="plot"/>, from <see cref="ChartRenderer.SubPlotRects"/>): the X tick labels turn when upright ones
    /// would collide at that width, so the top and bottom bands are read from this measurement. Before that (no
    /// <paramref name="plot"/>) the plot has no width, and only the left and right bands, which do not depend on it, are
    /// read; with it, the sideways overhang of the X tick labels (<see cref="EdgeBands.Overhang"/>) is read too, which the sides
    /// are then widened for (<see cref="SettleSides"/>).</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="figure">The figure it belongs to.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="plot">The plot area the axes is drawn in under the settled sides; only its X and width are used.</param>
    private static EdgeBands EdgeBandsOf(Axes axes, Figure figure, IRenderContext ctx, Rect? plot = null)
    {
        var pos = GetEffectivePosition(axes, figure);
        double cellHeight = figure.Height * (pos.RowEnd - pos.RowStart) / GetMaxRows(figure);
        var area = plot is { } drawn ? new Rect(drawn.X, 0, drawn.Width, cellHeight) : new Rect(0, 0, 0, cellHeight);
        var renderer = AxesRenderer.Create(axes, area, ctx, figure.Theme);
        // Each pass measures only the edges it reads: the sides before the plot has a width, the ends once it has one.
        return plot is null ? renderer.MeasureSideBands() : renderer.MeasureEndBands();
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
    /// applies); in a lower row it widens the row gap by the band, the distance its title rises. The left stack of the Y
    /// axis container (the primary's measured tick labels, <see cref="EdgeBands.Left"/>, its title and every slot past it)
    /// widens the left margin or the gutter to <see cref="LeftNeed"/>; what is drawn
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
        var (sides, plots) = SettleSides(figure, ctx, ReserveSideBands(figure, resolved, ctx));
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
    /// left and right spines does not depend on its plot's width (the room the X tick labels need beside their plot does, and
    /// is settled after it, <see cref="SettleSides"/>). An axes in the last column widens
    /// <see cref="SubPlotSpacing.MarginRight"/> to its <see cref="MeasureRightBand"/>, one left of another the gutter by
    /// it; the left stack of the Y axis container widens the left margin or the gutter to <see cref="LeftNeed"/>.</summary>
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
            // Left of the left spine the whole left stack of the container as the renderer draws it (the primary's tick
            // labels measured by its renderer, its title, every slot past it): the margin holds it, and a gutter holds it
            // right of its neighbour's band. Nothing drawn there reserves nothing, not even the pad: the pad holds the
            // outermost ink off the figure's edge, and a figure that draws none (an icon that fills its square, a row of
            // tiles with no axes) keeps the margin and the gutter its caller asked for.
            double stack = LeftStackBand(axes, figure.Theme, bands.Left, LeftAxisBands(axes, ctx, figure.Theme));
            if (stack > 0)
            {
                double need = stack + PadLeft;
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
