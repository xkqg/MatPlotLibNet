// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
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
                                       Rect plotArea, IRenderContext ctx, Theme theme, int steps = ColorBarStrip.RestingSteps)
        : base(cb, colorMap, min, max, plotArea, ctx, theme, steps) { }

    /// <inheritdoc />
    public override void Render()
    {
        // Horizontal: the bar below the plot area, where the one rule for it puts it
        var (body, scale, gradient, _) = DrawBody(vertical: false);
        double fullW = body.Width;
        double barH  = body.Height;
        double barX  = body.X;
        double barY  = body.Y;

        // Where the text stands below the body is one rule with the room the layout keeps for it (ColorBarRoomExtensions).
        var tickFont = Theme.TickFont();
        double labelY = barY + barH + Cb.TickTopBelow(Theme);
        double minorLabelY = scale.LogMarks is null ? labelY : barY + barH + Cb.TickTopBelow(Theme, minor: true);
        for (int i = 0; i < scale.Ticks.Length; i++)
            Ctx.Draw(TextLabel.Of(scale.Labels[i]), tickFont, new TextPlacement(new Point(gradient.X + gradient.Width * scale.Fractions[i], scale.IsMinorLabel(i) ? minorLabelY : labelY), TextAlignment.Center)
                { Vertical = TextVerticalAlignment.Top });

        if (Cb.Label is not null)
            Ctx.Draw(TextLabel.Of(Cb.Label), Theme.LabelFont(), new TextPlacement(new Point(barX + fullW / 2, barY + barH + Cb.LabelTopBelow(Theme)), TextAlignment.Center)
                { Vertical = TextVerticalAlignment.Top });
    }
}
