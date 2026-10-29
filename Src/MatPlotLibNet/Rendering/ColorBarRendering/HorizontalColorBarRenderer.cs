// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>
/// Renders a horizontal colorbar BELOW the plot area. The bar runs left-to-right
/// with min on the left, max on the right; tick labels appear underneath; the
/// optional label appears below the ticks.
/// </summary>
internal sealed class HorizontalColorBarRenderer : ColorBarRenderer
{
    /// <inheritdoc />
    public HorizontalColorBarRenderer(ColorBar cb, IColorMap colorMap, double min, double max,
                                       Rect plotArea, IRenderContext ctx, Theme theme, int steps = 50)
        : base(cb, colorMap, min, max, plotArea, ctx, theme, steps) { }

    /// <inheritdoc />
    public override void Render()
    {
        // Horizontal: bar below the plot area, length = plot width * Shrink, centered
        double fullW = PlotArea.Width * Cb.Shrink;
        double barW  = Cb.Aspect > 0 ? fullW : Cb.Width * Cb.Shrink;
        double barH  = Cb.Aspect > 0 ? fullW / Cb.Aspect : Cb.Width;
        double barX  = PlotArea.X + (PlotArea.Width - fullW) / 2;
        double barY  = PlotArea.Y + PlotArea.Height + Cb.Padding;

        double extW  = fullW * ExtendFrac;
        bool drawXMin = ExtendMin;
        bool drawXMax = ExtendMax;
        double gradX = barX + (drawXMin ? extW : 0);
        double gradW = fullW - (drawXMin ? extW : 0) - (drawXMax ? extW : 0);
        var scale = ScaleAlong(fullW, vertical: false);

        if (drawXMin)
        {
            var underColor = ColorMap.GetUnderColor() ?? ColorMap.GetColor(0.0);
            Ctx.DrawRectangle(new Rect(barX, barY, extW, barH), new ShapeStyle(underColor, null, 0));
        }

        for (int i = 0; i < Steps; i++)
        {
            double frac = (double)i / Steps;
            var color = ColorMap.GetColor(scale.GradientFraction(frac, (i + 0.5) / Steps));
            double stepX = gradX + gradW * i / Steps;
            double stepW = gradW / Steps + 1;
            Ctx.DrawRectangle(new Rect(stepX, barY, stepW, barH), new ShapeStyle(color, null, 0));
            if (Cb.DrawEdges)
                Ctx.DrawLine(new Point(stepX, barY), new Point(stepX, barY + barH), new StrokeStyle(Theme.ForegroundText, 0.3, LineStyle.Solid));
        }

        if (drawXMax)
        {
            var overColor = ColorMap.GetOverColor() ?? ColorMap.GetColor(1.0);
            Ctx.DrawRectangle(new Rect(gradX + gradW, barY, extW, barH), new ShapeStyle(overColor, null, 0));
        }

        Ctx.DrawRectangle(new Rect(barX, barY, fullW, barH), new ShapeStyle(null, Theme.ForegroundText, 0.5));

        var tickFont = ThemedFontProvider.TickFont(Theme);
        double labelY = barY + barH + 4 + tickFont.Size;
        for (int i = 0; i < scale.Ticks.Length; i++)
            Ctx.DrawText(scale.Labels[i], new Point(gradX + gradW * scale.Fractions[i], labelY), tickFont, TextAlignment.Center);

        if (Cb.Label is not null)
            Ctx.DrawText(Cb.Label, new Point(barX + fullW / 2, labelY + tickFont.Size + 4), ThemedFontProvider.LabelFont(Theme), TextAlignment.Center);
    }
}
