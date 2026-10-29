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
            Ctx.DrawRectangle(new Rect(barX, barY, barW, extH), new ShapeStyle(overColor, null, 0));
        }

        for (int i = 0; i < Steps; i++)
        {
            double frac = 1.0 - (double)i / Steps;
            var color = ColorMap.GetColor(frac);
            double stepY = gradY + gradH * i / Steps;
            double stepH = gradH / Steps + 1;
            Ctx.DrawRectangle(new Rect(barX, stepY, barW, stepH), new ShapeStyle(color, null, 0));
            if (Cb.DrawEdges)
                Ctx.DrawLine(new Point(barX, stepY), new Point(barX + barW, stepY), new StrokeStyle(Theme.ForegroundText, 0.3, LineStyle.Solid));
        }

        if (ExtendMin)
        {
            var underColor = ColorMap.GetUnderColor() ?? ColorMap.GetColor(0.0);
            Ctx.DrawRectangle(new Rect(barX, gradY + gradH, barW, extH), new ShapeStyle(underColor, null, 0));
        }

        Ctx.DrawRectangle(new Rect(barX, barY, barW, fullH), new ShapeStyle(null, Theme.ForegroundText, 0.5));

        var tickFont = ThemedFontProvider.TickFont(Theme);
        double labelX = barX + barW + 4;
        double maxTickWidth = 0;
        var scale = ScaleAlong(fullH, vertical: true);
        // Top to bottom: the end of the gradient is on top, and a tick stands over the gradient, not over the wedges at its ends.
        foreach (int i in Enumerable.Range(0, scale.Ticks.Length).OrderByDescending(i => scale.FractionOf(scale.Ticks[i])))
        {
            double y = gradY + gradH * (1 - scale.FractionOf(scale.Ticks[i]));
            Ctx.DrawText(scale.Labels[i], new Point(labelX, y + 4), tickFont, TextAlignment.Left);
            var w = Ctx.MeasureText(scale.Labels[i], tickFont).Width;
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
