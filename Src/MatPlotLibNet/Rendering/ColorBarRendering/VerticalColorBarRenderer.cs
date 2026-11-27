// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
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
                                     Rect plotArea, IRenderContext ctx, Theme theme, int steps = ColorBarStrip.RestingSteps)
        : base(cb, colorMap, min, max, plotArea, ctx, theme, steps) { }

    /// <inheritdoc />
    public override void Render()
    {
        // Vertical (default): the bar to the right of the plot area, where the one rule for it puts it
        var (body, scale, gradient, marks) = DrawBody(vertical: true);
        double fullH = body.Height;
        double barW  = body.Width;
        double barX  = body.X;
        double barY  = body.Y;

        var tickFont = Theme.TickFont();
        double labelX = barX + barW + marks.LabelGap();
        double minorLabelX = barX + barW + marks.LabelGap(minor: true);
        double maxTickWidth = 0;
        // Top to bottom: the end of the gradient is on top, and a tick stands over the gradient, not over the wedges at its ends.
        foreach (int i in scale.Fractions.DescendingOrder())
        {
            double y = gradient.Y + gradient.Height * (1 - scale.Fractions[i]);
            Ctx.Draw(TextLabel.Of(scale.Labels[i]), tickFont, new TextPlacement(new Point(scale.IsMinorLabel(i) ? minorLabelX : labelX, y), TextAlignment.Left) { Vertical = TextVerticalAlignment.Center });
            var w = Ctx.ExtentOf(TextLabel.Of(scale.Labels[i]), tickFont).Width;
            if (w > maxTickWidth) maxTickWidth = w;
        }

        if (Cb.Label is not null)
        {
            // Rotate the label 90° (vertical, reading bottom-to-top) so it sits in a
            // narrow gutter beside the colorbar instead of sprawling horizontally and
            // getting clipped by the figure right edge. Matches matplotlib defaults.
            var labelFont = Theme.LabelFont();
            double labelGutter = labelX + maxTickWidth + ColorBarRoomExtensions.TextGutterBeside;
            Ctx.Draw(TextLabel.Of(Cb.Label), labelFont, new TextPlacement(new Point(labelGutter, barY + fullH / 2), TextAlignment.Center) { Rotation = 90 });
        }
    }
}
