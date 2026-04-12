// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarBarSeries"/> as wedge-shaped bars in polar coordinates.</summary>
internal sealed class PolarBarSeriesRenderer : SeriesRenderer<PolarBarSeries>
{
    /// <inheritdoc />
    public PolarBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PolarBarSeries series)
    {
        var baseColor = ResolveColor(series.Color);
        var fillColor = ApplyAlpha(baseColor, series.Alpha);
        var bounds = Area.PlotBounds;
        double rMax = series.R.Length > 0 ? series.R.Max() * 1.1 : 1;
        var transform = new PolarTransform(bounds, rMax);

        double halfWidth = series.BarWidth / 2;

        for (int i = 0; i < series.R.Length; i++)
        {
            double theta = series.Theta[i];
            double r = series.R[i];

            // Draw wedge as a path: inner arc -> outer arc -> close
            int segments = 12;
            var pts = new List<Point>();

            // Outer arc (from theta - halfWidth to theta + halfWidth)
            for (int s = 0; s <= segments; s++)
            {
                double a = theta - halfWidth + (series.BarWidth * s / segments);
                pts.Add(transform.PolarToPixel(r, a));
            }

            // Back to center along inner arc
            for (int s = segments; s >= 0; s--)
            {
                double a = theta - halfWidth + (series.BarWidth * s / segments);
                pts.Add(transform.PolarToPixel(0, a));
            }

            Ctx.DrawPolygon(pts, fillColor, baseColor, 0.5);
        }
    }
}
