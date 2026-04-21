// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="DonutSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class DonutSeriesRenderer : CircularRenderer<DonutSeries>
{
    /// <inheritdoc />
    public DonutSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
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
            // Donut uses screen-Y convention (0° = right, positive sweeps clockwise on screen).
            // BuildWedgePath uses math-Y (cy + r·sin), so negate angles to preserve visual output.
            double startDeg = -angle * 180 / Math.PI;
            double endDeg = -(angle + sweep) * 180 / Math.PI;
            Ctx.DrawPath(BuildWedgePath(cx, cy, innerR, outerR, startDeg, endDeg), color, null, 0);
            angle += sweep;
        }
        if (series.CenterText is not null)
            Ctx.DrawText(series.CenterText, new Point(cx, cy + 5), new Font { Size = 14, Weight = FontWeight.Bold }, TextAlignment.Center);
    }
}
