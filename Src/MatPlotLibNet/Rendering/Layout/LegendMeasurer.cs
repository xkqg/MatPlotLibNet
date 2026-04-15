// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>
/// Measures the bounding box of an axes legend without drawing it. Shared between
/// <see cref="ConstrainedLayoutEngine"/> (which needs the size to reserve outside-legend
/// margin before layout is finalised) and <see cref="AxesRenderer"/> (which needs the
/// size to position the box during the actual draw pass).
/// </summary>
/// <remarks>
/// Extracting this into a single function guarantees both callers compute byte-identical
/// dimensions — if they drifted, outside legends would either clip (layout under-reserved)
/// or leave a gap (layout over-reserved). The measurement formulas here MUST stay in sync
/// with <see cref="AxesRenderer.RenderLegend"/>'s own layout math.
/// </remarks>
internal static class LegendMeasurer
{
    /// <summary>Padding inside the legend frame, in pixels.</summary>
    internal const double FramePadding = 8;

    /// <summary>Measures the legend box for an axes, returning zero if the legend is hidden
    /// or has no labelled series. The returned width / height are already in pixels and
    /// include frame padding on all sides.</summary>
    /// <param name="axes">The axes whose legend should be measured.</param>
    /// <param name="ctx">Render context used for <see cref="IRenderContext.MeasureText"/>.</param>
    /// <param name="tickFont">The base tick font — the same font the renderer uses when
    /// the legend has no explicit <see cref="Legend.FontSize"/> override.</param>
    internal static Size MeasureBox(Axes axes, IRenderContext ctx, Font tickFont)
    {
        if (!axes.Legend.Visible) return Size.Empty;

        // Collect labelled series — mirrors AxesRenderer.RenderLegend
        var labels = new List<string>();
        for (int i = 0; i < axes.Series.Count; i++)
        {
            var series = axes.Series[i];
            if (!string.IsNullOrEmpty(series.Label))
                labels.Add(series.Label);
        }
        if (labels.Count == 0) return Size.Empty;

        var legend = axes.Legend;
        var font = legend.FontSize.HasValue ? tickFont with { Size = legend.FontSize.Value } : tickFont;

        // Handle geometry — matches AxesRenderer.RenderLegend lines 240-247
        double handleWidth  = font.Size * 2.0 * legend.MarkerScale;
        double handleHeight = font.Size * 0.7 * legend.MarkerScale;
        double swatchSize   = handleHeight;
        double maxSwatchW   = handleWidth;
        double swatchGap    = font.Size * 0.8;
        double lineHeight   = swatchSize + Math.Max(0, legend.LabelSpacing * font.Size);

        int nCols = Math.Max(1, legend.NCols);
        int nRows = (int)Math.Ceiling((double)labels.Count / nCols);

        // Per-column max label width
        var colMaxWidths = new double[nCols];
        for (int i = 0; i < labels.Count; i++)
        {
            int col = i % nCols;
            var size = MathTextParser.ContainsMath(labels[i])
                ? ctx.MeasureRichText(MathTextParser.Parse(labels[i]), font)
                : ctx.MeasureText(labels[i], font);
            if (size.Width > colMaxWidths[col]) colMaxWidths[col] = size.Width;
        }

        double colSpacingPx = legend.ColumnSpacing * font.Size;
        double totalContentWidth = maxSwatchW + swatchGap + colMaxWidths.Sum()
            + (nCols - 1) * (maxSwatchW + swatchGap + colSpacingPx);

        // Title height
        var titleFont = legend.TitleFontSize.HasValue
            ? font with { Size = legend.TitleFontSize.Value, Weight = FontWeight.Bold }
            : font with { Size = font.Size + 1, Weight = FontWeight.Bold };
        double titleHeight = !string.IsNullOrEmpty(legend.Title) ? titleFont.Size + 4 : 0;

        double boxWidth = FramePadding + totalContentWidth + FramePadding;
        double boxHeight = FramePadding + titleHeight + nRows * lineHeight - (lineHeight - swatchSize) + FramePadding;

        return new Size(boxWidth, boxHeight);
    }

    /// <summary>Returns <see langword="true"/> if the legend position places the box
    /// outside the plot area — used by the layout engine to decide whether to reserve
    /// extra margin beyond what the tick labels / titles need.</summary>
    internal static bool IsOutsidePosition(LegendPosition position) => position switch
    {
        LegendPosition.OutsideRight  => true,
        LegendPosition.OutsideLeft   => true,
        LegendPosition.OutsideTop    => true,
        LegendPosition.OutsideBottom => true,
        _                            => false,
    };
}
