// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="RadarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class RadarSeriesRenderer : SeriesRenderer<RadarSeries>
{
    /// <inheritdoc />
    public RadarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(RadarSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? ApplyAlpha(color, series.Alpha);
        int n = series.Categories.Length;
        if (n < 3) return;
        var bounds = Area.PlotBounds;
        double cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2;
        double radius = Math.Min(bounds.Width, bounds.Height) / 2 * 0.75;
        double maxVal = series.MaxValue ?? series.Values.Max();
        if (maxVal <= 0) maxVal = 1;
        var webColor = Colors.GridGray;

        for (int ring = 1; ring <= 5; ring++)
        {
            double frac = ring / 5.0;
            var ringPts = new List<Point>(n);
            for (int i = 0; i < n; i++) { double a = 2 * Math.PI * i / n - Math.PI / 2; ringPts.Add(new Point(cx + radius * frac * Math.Cos(a), cy + radius * frac * Math.Sin(a))); }
            Ctx.DrawPolygon(ringPts, null, webColor, 0.5);
        }
        var labelFont = new Font { Size = 10 };
        for (int i = 0; i < n; i++)
        {
            double a = 2 * Math.PI * i / n - Math.PI / 2;
            Ctx.DrawLine(new Point(cx, cy), new Point(cx + radius * Math.Cos(a), cy + radius * Math.Sin(a)), webColor, 0.5, LineStyle.Solid);
            Ctx.DrawText(series.Categories[i], new Point(cx + (radius + 15) * Math.Cos(a), cy + (radius + 15) * Math.Sin(a)), labelFont, TextAlignment.Center);
        }
        var dataPts = new List<Point>(n);
        for (int i = 0; i < n; i++) { double norm = Math.Min(series.Values[i] / maxVal, 1.0); double a = 2 * Math.PI * i / n - Math.PI / 2; dataPts.Add(new Point(cx + radius * norm * Math.Cos(a), cy + radius * norm * Math.Sin(a))); }
        Ctx.DrawPolygon(dataPts, fillColor, color, series.LineWidth);
    }
}
