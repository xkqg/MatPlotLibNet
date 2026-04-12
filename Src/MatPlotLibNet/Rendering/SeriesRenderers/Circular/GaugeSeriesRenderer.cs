// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="GaugeSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class GaugeSeriesRenderer : SeriesRenderer<GaugeSeries>
{
    /// <inheritdoc />
    public GaugeSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(GaugeSeries series)
    {
        var bounds = Area.PlotBounds;
        double cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height * 0.75;
        double radius = Math.Min(bounds.Width, bounds.Height) * 0.4;
        double range = series.Max - series.Min;
        if (range <= 0) return;
        var ranges = series.Ranges ?? [(60, Colors.Green), (80, Colors.Amber), (100, Colors.Red)];

        double prevAngle = Math.PI;
        foreach (var (threshold, color) in ranges)
        {
            double frac = Math.Clamp((threshold - series.Min) / range, 0, 1);
            double endAngle = Math.PI - frac * Math.PI;
            int steps = 20;
            var points = new List<Point>();
            for (int i = 0; i <= steps; i++) { double a = prevAngle + (endAngle - prevAngle) * i / steps; points.Add(new Point(cx + radius * Math.Cos(a), cy - radius * Math.Sin(a))); }
            for (int i = steps; i >= 0; i--) { double a = prevAngle + (endAngle - prevAngle) * i / steps; points.Add(new Point(cx + radius * 0.7 * Math.Cos(a), cy - radius * 0.7 * Math.Sin(a))); }
            Ctx.DrawPolygon(points, color, null, 0);
            prevAngle = endAngle;
        }
        double valFrac = Math.Clamp((series.Value - series.Min) / range, 0, 1);
        double needleAngle = Math.PI - valFrac * Math.PI;
        Ctx.DrawLine(new Point(cx, cy), new Point(cx + radius * 0.85 * Math.Cos(needleAngle), cy - radius * 0.85 * Math.Sin(needleAngle)), series.NeedleColor, 2.5, LineStyle.Solid);
        Ctx.DrawCircle(new Point(cx, cy), 4, series.NeedleColor, null, 0);
        Ctx.DrawText(series.Value.ToString("G5"), new Point(cx, cy + 20), new Font { Size = 12, Weight = FontWeight.Bold }, TextAlignment.Center);
    }
}
