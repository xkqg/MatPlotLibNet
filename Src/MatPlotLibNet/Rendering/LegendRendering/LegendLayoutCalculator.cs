// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Computes the pixel geometry of a legend (fonts, column widths, box
/// dimensions, position) from a <see cref="Legend"/> config + label list +
/// plot area. Encapsulates the formulas that were previously duplicated
/// verbatim in <c>AxesRenderer.RenderLegend</c> and
/// <c>AxesRenderer.ComputeLegendBounds</c>.
/// </summary>
/// <remarks>
/// Phase B.3 of the strict-90 floor plan (2026-04-20). Fixes the DRY
/// violation that had two copies of the same layout math — one for drawing,
/// one for hit-testing. Any future matplotlib-parity tweak now lives in
/// exactly one place.
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
    {
        // Font: merge FontSize override into tick font
        var baseFont = ThemedFontProvider.TickFont(_theme);
        var font = legend.FontSize.HasValue ? baseFont with { Size = legend.FontSize.Value } : baseFont;

        // matplotlib legend handles use `handlelength = 2.0 em` × `handleheight = 0.7 em`
        // where 1 em = legend font size.
        double handleWidth  = font.Size * 2.0 * legend.MarkerScale;
        double handleHeight = font.Size * 0.7 * legend.MarkerScale;
        double swatchGap    = font.Size * 0.8;
        double padding = 8;
        double lineHeight = handleHeight + Math.Max(0, legend.LabelSpacing * font.Size);

        int nCols = Math.Max(1, legend.NCols);
        int nRows = (int)Math.Ceiling((double)labels.Count / nCols);

        // Measure column widths — parse mathtext so $\alpha$ decay measures as "α decay"
        var colMaxWidths = new double[nCols];
        for (int i = 0; i < labels.Count; i++)
        {
            int col = i % nCols;
            var size = MathTextParser.ContainsMath(labels[i])
                ? _ctx.MeasureRichText(MathTextParser.Parse(labels[i]), font)
                : _ctx.MeasureText(labels[i], font);
            if (size.Width > colMaxWidths[col]) colMaxWidths[col] = size.Width;
        }

        double colSpacingPx = legend.ColumnSpacing * font.Size;
        double totalContentWidth = handleWidth + swatchGap + colMaxWidths.Sum()
            + (nCols - 1) * (handleWidth + swatchGap + colSpacingPx);

        // Title height
        var titleFont = legend.TitleFontSize.HasValue
            ? baseFont with { Size = legend.TitleFontSize.Value, Weight = FontWeight.Bold }
            : baseFont with { Size = baseFont.Size + 1, Weight = FontWeight.Bold };
        double titleHeight = !string.IsNullOrEmpty(legend.Title) ? titleFont.Size + 4 : 0;

        double boxWidth = padding + totalContentWidth + padding;
        double boxHeight = padding + titleHeight + nRows * lineHeight - (lineHeight - handleHeight) + padding;

        // Polymorphic position dispatch (Phase B.2)
        var (boxX, boxY) = LegendPositionStrategyFactory
            .Create(legend.Position)
            .ComputeBox(plotArea, boxWidth, boxHeight);

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
            TitleHeight: titleHeight,
            BoxWidth: boxWidth,
            BoxHeight: boxHeight,
            BoxX: boxX,
            BoxY: boxY);
    }
}
