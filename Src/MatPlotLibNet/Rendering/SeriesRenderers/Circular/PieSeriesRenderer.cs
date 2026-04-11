// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="PieSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class PieSeriesRenderer : SeriesRenderer<PieSeries>
{
    /// <inheritdoc />
    public PieSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PieSeries series)
    {
        double total = series.Sizes.Sum();
        if (total == 0) return;
        double cx = Area.PlotBounds.X + Area.PlotBounds.Width / 2;
        double cy = Area.PlotBounds.Y + Area.PlotBounds.Height / 2;
        double baseRadius = Math.Min(Area.PlotBounds.Width, Area.PlotBounds.Height) / 2 * 0.8;
        double radius = series.Radius.HasValue
            ? series.Radius.Value * Math.Min(Area.PlotBounds.Width, Area.PlotBounds.Height) / 2
            : baseRadius;
        double startAngle = series.StartAngle * Math.PI / 180;
        double direction = series.CounterClockwise ? -1 : 1;

        for (int i = 0; i < series.Sizes.Length; i++)
        {
            double sweep = direction * series.Sizes[i] / total * 2 * Math.PI;
            double midAngle = startAngle + sweep / 2;
            double explode = series.Explode is not null && i < series.Explode.Length ? series.Explode[i] : 0.0;
            double sliceCx = cx + explode * radius * Math.Cos(midAngle);
            double sliceCy = cy - explode * radius * Math.Sin(midAngle);

            // Shadow: draw offset gray slice before the main slice
            if (series.Shadow)
            {
                double shadowOffset = radius * 0.03;
                double scx = sliceCx + shadowOffset;
                double scy = sliceCy + shadowOffset;
                double endAngleShadow = startAngle + sweep;
                Ctx.DrawPath([
                    new MoveToSegment(new Point(scx, scy)),
                    new LineToSegment(new Point(scx + radius * Math.Cos(startAngle), scy - radius * Math.Sin(startAngle))),
                    new ArcSegment(new Point(scx + radius * Math.Cos(endAngleShadow), scy - radius * Math.Sin(endAngleShadow)), radius, radius, startAngle, endAngleShadow),
                    new CloseSegment()
                ], new Color(0, 0, 0, 80), null, 0);
            }

            double endAngle = startAngle + sweep;
            var sliceColor = series.Colors is not null && i < series.Colors.Length ? series.Colors[i] : SeriesColor;
            Ctx.DrawPath([
                new MoveToSegment(new Point(sliceCx, sliceCy)),
                new LineToSegment(new Point(sliceCx + radius * Math.Cos(startAngle), sliceCy - radius * Math.Sin(startAngle))),
                new ArcSegment(new Point(sliceCx + radius * Math.Cos(endAngle), sliceCy - radius * Math.Sin(endAngle)), radius, radius, startAngle, endAngle),
                new CloseSegment()
            ], sliceColor, Colors.White, 1);

            // AutoPct: draw percentage text at the centroid of the slice
            if (series.AutoPct is not null)
            {
                double pct = series.Sizes[i] / total * 100;
                string label = string.Format(series.AutoPct, pct);
                double textAngle = startAngle + sweep / 2;
                double textR = radius * 0.6;
                double tx = sliceCx + textR * Math.Cos(textAngle);
                double ty = sliceCy - textR * Math.Sin(textAngle);
                Ctx.DrawText(label, new Point(tx, ty), new Font { Size = 10, Color = Colors.White }, TextAlignment.Center);
            }

            startAngle = endAngle;
        }
    }
}
