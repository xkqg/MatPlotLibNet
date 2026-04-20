// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Immutable result of <see cref="LegendLayoutCalculator.Compute"/>: all the
/// pre-computed geometry a renderer needs to draw the legend (box dimensions,
/// fonts, column widths, per-entry X/Y offsets).
/// </summary>
/// <remarks>
/// Phase B.3 of the strict-90 floor plan (2026-04-20). Replaces the ~45 lines
/// of layout code that was previously duplicated verbatim in
/// <c>AxesRenderer.RenderLegend</c> (draw-time) and
/// <c>AxesRenderer.ComputeLegendBounds</c> (hit-test-time). Both call sites
/// now delegate to <see cref="LegendLayoutCalculator"/> which returns this
/// record; there is exactly ONE authoritative implementation of the formulas.
/// </remarks>
public sealed record LegendLayout(
    Font Font,
    Font TitleFont,
    double SwatchWidth,
    double SwatchHeight,
    double SwatchGap,
    double Padding,
    double LineHeight,
    int NCols,
    int NRows,
    double[] ColMaxWidths,
    double ColSpacingPx,
    double TotalContentWidth,
    double TitleHeight,
    double BoxWidth,
    double BoxHeight,
    double BoxX,
    double BoxY)
{
    /// <summary>Returns the X pixel offset of the given column (0-indexed).</summary>
    public double ColumnX(int col)
    {
        double colX = BoxX + Padding;
        for (int c = 0; c < col; c++)
            colX += SwatchWidth + SwatchGap + ColMaxWidths[c] + ColSpacingPx;
        return colX;
    }

    /// <summary>Returns the Y pixel offset of the given row (0-indexed).</summary>
    public double EntryY(int row) => BoxY + Padding + TitleHeight + row * LineHeight;

    /// <summary>Returns the hit-test width (swatch + gap + column-label) for the given column.</summary>
    public double ItemWidth(int col) => SwatchWidth + SwatchGap + ColMaxWidths[col];
}
