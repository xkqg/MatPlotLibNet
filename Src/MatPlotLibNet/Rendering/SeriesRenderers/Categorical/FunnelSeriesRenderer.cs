// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="FunnelSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class FunnelSeriesRenderer : SeriesRenderer<FunnelSeries>
{
    /// <inheritdoc />
    public FunnelSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(FunnelSeries series)
    {
        // Scaled to the widest stage that can be drawn; with none (no stage at all, or none that can be drawn) the plot
        // stays empty.
        if (series.Values.DrawableRange() is not { } range || range.Hi <= 0) return;
        double maxVal = range.Hi;
        var bounds = Area.PlotBounds;
        double rowH = bounds.Height / series.Values.Length;
        for (int i = 0; i < series.Values.Length; i++)
        {
            double frac = series.Values[i] / maxVal;
            double barW = bounds.Width * frac;
            double x = bounds.X + (bounds.Width - barW) / 2;
            double y = bounds.Y + i * rowH;
            // A stage that cannot be drawn is skipped with its label; the stages around it keep their rows.
            var bar = new Rect(x, y + 2, barW, rowH - 4);
            if (!bar.IsDrawable()) continue;
            Ctx.DrawRectangle(bar, new ShapeStyle(PartColor(series.Colors, i), null, 0));
            Ctx.DrawText(series.Labels[i], new Point(bounds.X + bounds.Width / 2, y + rowH / 2 + 4), new Font { Size = 10 }, TextAlignment.Center);
        }
    }
}
