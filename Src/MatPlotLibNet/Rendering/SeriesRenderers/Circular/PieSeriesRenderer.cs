// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class PieSeriesRenderer : SeriesRenderer<PieSeries>
{
    public PieSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(PieSeries series)
    {
        double total = series.Sizes.Sum();
        if (total == 0) return;
        double cx = Area.PlotBounds.X + Area.PlotBounds.Width / 2;
        double cy = Area.PlotBounds.Y + Area.PlotBounds.Height / 2;
        double radius = Math.Min(Area.PlotBounds.Width, Area.PlotBounds.Height) / 2 * 0.8;
        double startAngle = series.StartAngle * Math.PI / 180;
        for (int i = 0; i < series.Sizes.Length; i++)
        {
            double sweep = series.Sizes[i] / total * 2 * Math.PI;
            double endAngle = startAngle + sweep;
            var sliceColor = series.Colors is not null && i < series.Colors.Length ? series.Colors[i] : SeriesColor;
            Ctx.DrawPath([
                new MoveToSegment(new Point(cx, cy)),
                new LineToSegment(new Point(cx + radius * Math.Cos(startAngle), cy - radius * Math.Sin(startAngle))),
                new ArcSegment(new Point(cx + radius * Math.Cos(endAngle), cy - radius * Math.Sin(endAngle)), radius, radius, startAngle, endAngle),
                new CloseSegment()
            ], sliceColor, Color.White, 1);
            startAngle = endAngle;
        }
    }
}
