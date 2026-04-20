// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>
/// Renders a vertical colorbar to the RIGHT of the plot area. The bar runs
/// top-to-bottom with max on top, min on bottom; tick labels appear to the
/// right; the optional label appears further right (rotated 90°).
/// </summary>
/// <remarks>
/// Pure copy of the vertical-orientation block from
/// <c>AxesRenderer.RenderColorBar</c> (lines 647-709 pre-extraction). Identity
/// of byte-output is pinned by <c>ColorBarRendererBaselineTests</c> + the
/// existing AxesRendererCoverageTests suite; the flip-over (B.1.e) verifies
/// no behavioral change via full test + fidelity run.
/// </remarks>
internal sealed class VerticalColorBarRenderer : ColorBarRenderer
{
    /// <inheritdoc />
    public VerticalColorBarRenderer(ColorBar cb, IColorMap colorMap, double min, double max,
                                     Rect plotArea, IRenderContext ctx, Theme theme, int steps = 50)
        : base(cb, colorMap, min, max, plotArea, ctx, theme, steps) { }

    /// <inheritdoc />
    public override void Render()
    {
        // Vertical (default): bar to the right of the plot area
        double fullH = PlotArea.Height * Cb.Shrink;
        double barW  = Cb.Width;
        double barX  = PlotArea.X + PlotArea.Width + Cb.Padding;
        double barY  = PlotArea.Y + (PlotArea.Height - fullH) / 2;

        double extH  = fullH * ExtendFrac;
        double gradY = barY + (ExtendMax ? extH : 0);
        double gradH = fullH - (ExtendMin ? extH : 0) - (ExtendMax ? extH : 0);

        if (ExtendMax)
        {
            var overColor = ColorMap.GetOverColor() ?? ColorMap.GetColor(1.0);
            Ctx.DrawRectangle(new Rect(barX, barY, barW, extH), overColor, null, 0);
        }

        for (int i = 0; i < Steps; i++)
        {
            double frac = 1.0 - (double)i / Steps;
            var color = ColorMap.GetColor(frac);
            double stepY = gradY + gradH * i / Steps;
            double stepH = gradH / Steps + 1;
            Ctx.DrawRectangle(new Rect(barX, stepY, barW, stepH), color, null, 0);
            if (Cb.DrawEdges)
                Ctx.DrawLine(new Point(barX, stepY), new Point(barX + barW, stepY), Theme.ForegroundText, 0.3, LineStyle.Solid);
        }

        if (ExtendMin)
        {
            var underColor = ColorMap.GetUnderColor() ?? ColorMap.GetColor(0.0);
            Ctx.DrawRectangle(new Rect(barX, gradY + gradH, barW, extH), underColor, null, 0);
        }

        Ctx.DrawRectangle(new Rect(barX, barY, barW, fullH), null, Theme.ForegroundText, 0.5);

        var tickFont = ThemedFontProvider.TickFont(Theme);
        double labelX = barX + barW + 4;
        double maxTickWidth = 0;
        var vCbTicks = new double[TickCount];
        for (int i = 0; i < TickCount; i++) vCbTicks[i] = Max - ((double)i / (TickCount - 1)) * (Max - Min);
        var vCbFormat = AxesRenderer.BuildUniformTickFormatter(vCbTicks);
        for (int i = 0; i < TickCount; i++)
        {
            double frac = (double)i / (TickCount - 1);
            double value = Max - frac * (Max - Min);
            var tickText = vCbFormat(value);
            Ctx.DrawText(tickText, new Point(labelX, barY + fullH * frac + 4), tickFont, TextAlignment.Left);
            var w = Ctx.MeasureText(tickText, tickFont).Width;
            if (w > maxTickWidth) maxTickWidth = w;
        }

        if (Cb.Label is not null)
        {
            // Rotate the label 90° (vertical, reading bottom-to-top) so it sits in a
            // narrow gutter beside the colorbar instead of sprawling horizontally and
            // getting clipped by the figure right edge. Matches matplotlib defaults.
            var labelFont = ThemedFontProvider.LabelFont(Theme);
            double labelGutter = labelX + maxTickWidth + 8;
            Ctx.DrawText(Cb.Label, new Point(labelGutter, barY + fullH / 2), labelFont, TextAlignment.Center, rotation: 90);
        }
    }
}
