// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class FunnelSeriesRenderer : SeriesRenderer<FunnelSeries>
{
    public FunnelSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(FunnelSeries series)
    {
        double maxVal = series.Values.Max();
        if (maxVal <= 0) return;
        var bounds = Area.PlotBounds;
        double rowH = bounds.Height / series.Values.Length;
        for (int i = 0; i < series.Values.Length; i++)
        {
            double frac = series.Values[i] / maxVal;
            double barW = bounds.Width * frac;
            double x = bounds.X + (bounds.Width - barW) / 2;
            double y = bounds.Y + i * rowH;
            var color = series.Colors is not null && i < series.Colors.Length ? series.Colors[i]
                : Theme.Default.CycleColors[i % Theme.Default.CycleColors.Length];
            Ctx.DrawRectangle(new Rect(x, y + 2, barW, rowH - 4), color, null, 0);
            Ctx.DrawText(series.Labels[i], new Point(bounds.X + bounds.Width / 2, y + rowH / 2 + 4), new Font { Size = 10 }, TextAlignment.Center);
        }
    }
}
