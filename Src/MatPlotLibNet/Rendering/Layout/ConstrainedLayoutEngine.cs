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
    private const double XLabelBottomGap = 35;  // PlotArea.Y + Height + 35 for X axis label baseline
    private const double YLabelLeftPos   = 45;  // PlotArea.X - 45 for Y axis label centre

    /// <summary>Computes optimal <see cref="SubPlotSpacing"/> margins for the figure.</summary>
    internal SubPlotSpacing Compute(Figure figure, IRenderContext ctx)
    {
        var theme     = figure.Theme;
        var tickFont  = TickFont(theme);
        var labelFont = LabelFont(theme);
        var titleFont = TitleFont(theme);

        double left   = figure.Spacing.MarginLeft;
        double bottom = figure.Spacing.MarginBottom;
        double top    = figure.Spacing.MarginTop;
        double right  = figure.Spacing.MarginRight;

        int maxRows = GetMaxRows(figure);
        int maxCols = GetMaxCols(figure);

        foreach (var axes in figure.SubPlots)
        {
            var m   = Measure(axes, ctx, tickFont, labelFont, titleFont);
            var pos = GetEffectivePosition(axes, figure);

            // Only edge subplots contribute to the margin on that edge
            if (pos.ColStart == 0        && m.LeftNeeded   > left)   left   = m.LeftNeeded;
            if (pos.RowEnd   >= maxRows  && m.BottomNeeded > bottom) bottom = m.BottomNeeded;
            if (pos.RowStart == 0        && m.TopNeeded    > top)    top    = m.TopNeeded;
            if (pos.ColEnd   >= maxCols  && m.RightNeeded  > right)  right  = m.RightNeeded;
        }

        // Clamp to sane bounds
        return figure.Spacing with
        {
            MarginLeft   = Math.Clamp(left,   30, 120),
            MarginBottom = Math.Clamp(bottom, 30, 100),
            MarginTop    = Math.Clamp(top,    20, 80),
            MarginRight  = Math.Clamp(right,  10, 60),
        };
    }

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

    private LayoutMetrics Measure(Axes axes, IRenderContext ctx, Font tickFont, Font labelFont, Font titleFont)
    {
        // --- Left margin: Y-tick labels + optional Y-axis label ---
        string yTickProbe = EstimateYTickLabel(axes, tickFont);
        double maxYTickWidth = ctx.MeasureText(yTickProbe, tickFont).Width;
        double leftNeeded = maxYTickWidth + YTickRightGap + PadLeft;

        if (axes.YAxis.Label is not null)
        {
            // Y-label is drawn centre-aligned at PlotArea.X - YLabelLeftPos
            double halfLabelWidth = ctx.MeasureText(axes.YAxis.Label, labelFont).Width / 2;
            leftNeeded = Math.Max(leftNeeded, YLabelLeftPos + halfLabelWidth + PadLeft);
        }

        // --- Bottom margin: X-tick labels + optional X-axis label ---
        double tickH = ctx.MeasureText("0", tickFont).Height;  // single line height
        double bottomNeeded = XTickBottomGap + tickH + PadBottom;

        if (axes.XAxis.Label is not null)
        {
            double xLabelH = ctx.MeasureText(axes.XAxis.Label, labelFont).Height;
            double xLabelBottom = XLabelBottomGap + xLabelH;
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

        // --- Right margin: minimal; widen if secondary Y-axis label present ---
        double rightNeeded = 0;
        if (axes.SecondaryYAxis?.Label is not null)
        {
            double halfSecWidth = ctx.MeasureText(axes.SecondaryYAxis.Label, labelFont).Width / 2;
            rightNeeded = 45 + halfSecWidth + PadRight;
        }

        return new LayoutMetrics(leftNeeded, bottomNeeded, topNeeded, rightNeeded);
    }

    /// <summary>Returns a representative Y-tick label string for width estimation.
    /// Uses the configured axis bounds when available; falls back to a worst-case proxy.</summary>
    private static string EstimateYTickLabel(Axes axes, Font tickFont)
    {
        if (axes.YAxis.Max.HasValue)
            return AxesRenderer.FormatTickValue(axes.YAxis.Max.Value);
        if (axes.YAxis.Min.HasValue)
            return AxesRenderer.FormatTickValue(axes.YAxis.Min.Value);

        // Conservative fallback: 7-char string representative of "−9.999"
        return "\u22129.999";
    }

    // --- Font factories that mirror AxesRenderer ---

    private static Font TickFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size - 2,
        Color  = theme.ForegroundText,
    };

    private static Font LabelFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size,
        Color  = theme.ForegroundText,
    };

    private static Font TitleFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size   = theme.DefaultFont.Size + 2,
        Weight = FontWeight.Bold,
        Color  = theme.ForegroundText,
    };
}
