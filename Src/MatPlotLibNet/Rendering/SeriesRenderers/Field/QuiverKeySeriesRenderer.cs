// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="QuiverKeySeries"/> as a reference arrow with a label in axes-fraction coordinates.</summary>
internal sealed class QuiverKeySeriesRenderer : SeriesRenderer<QuiverKeySeries>
{
    /// <inheritdoc />
    public QuiverKeySeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(QuiverKeySeries series)
    {
        var color = ResolveColor(series.Color);
        var bounds = Area.PlotBounds;

        // Convert axes-fraction to pixel coordinates
        double px = bounds.X + series.X * bounds.Width;
        double py = bounds.Y + (1.0 - series.Y) * bounds.Height;

        // Arrow length in pixels (approximate scale from U data units to pixels)
        double pixelPerDataUnit = bounds.Width / (Transform.DataXMax - Transform.DataXMin);
        double arrowPx = series.U * pixelPerDataUnit;

        var tail = new Point(px, py);
        var head = new Point(px + arrowPx, py);
        Ctx.DrawLine(tail, head, color, 1.5, LineStyle.Solid);

        // Arrow head (simple triangle)
        double hs = 6; // arrowhead size
        Ctx.DrawLine(head, new Point(head.X - hs, head.Y - hs / 2), color, 1.5, LineStyle.Solid);
        Ctx.DrawLine(head, new Point(head.X - hs, head.Y + hs / 2), color, 1.5, LineStyle.Solid);

        // Label
        var font = new Font { Family = "sans-serif", Size = series.FontSize };
        Ctx.DrawText(series.Label, new Point(px + arrowPx / 2, py - 8), font, TextAlignment.Center);
    }
}
