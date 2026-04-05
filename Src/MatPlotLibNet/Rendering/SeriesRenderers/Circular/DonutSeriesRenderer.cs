// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class DonutSeriesRenderer : SeriesRenderer<DonutSeries>
{
    public DonutSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(DonutSeries series)
    {
        var bounds = Area.PlotBounds;
        double cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2;
        double outerR = Math.Min(bounds.Width, bounds.Height) / 2 * 0.8;
        double innerR = outerR * series.InnerRadius;
        double total = series.Sizes.Sum();
        if (total <= 0) return;
        double angle = series.StartAngle * Math.PI / 180;
        for (int i = 0; i < series.Sizes.Length; i++)
        {
            double sweep = series.Sizes[i] / total * 2 * Math.PI;
            var color = series.Colors is not null && i < series.Colors.Length ? series.Colors[i] : Theme.Default.CycleColors[i % Theme.Default.CycleColors.Length];
            Ctx.DrawPath([
                new MoveToSegment(new Point(cx + outerR * Math.Cos(angle), cy - outerR * Math.Sin(angle))),
                new ArcSegment(new Point(cx, cy), outerR, outerR, -angle * 180 / Math.PI, -(angle + sweep) * 180 / Math.PI),
                new LineToSegment(new Point(cx + innerR * Math.Cos(angle + sweep), cy - innerR * Math.Sin(angle + sweep))),
                new ArcSegment(new Point(cx, cy), innerR, innerR, -(angle + sweep) * 180 / Math.PI, -angle * 180 / Math.PI),
                new CloseSegment()
            ], color, null, 0);
            angle += sweep;
        }
        if (series.CenterText is not null)
            Ctx.DrawText(series.CenterText, new Point(cx, cy + 5), new Font { Size = 14, Weight = FontWeight.Bold }, TextAlignment.Center);
    }
}
