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
    internal override void AddMarks(PolarBarSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(PolarBarSeries series)
    {
        var baseColor = ResolveColor(series.Color);
        var fillColor = ApplyAlpha(baseColor, series.Alpha);
        var transform = Context.PolarTransformFor(series);

        // Stacked bars stand on the wedges of the same index before them (the scale carries where), the others on the centre.
        var feet = Context.RadialScale?.FootOf(series);

        double halfWidth = series.BarWidth / 2;

        for (int i = 0; i < series.R.Length; i++)
        {
            double theta = series.Theta[i];
            double inner = feet is not null && i < feet.Length ? feet[i] : 0;
            double r = series.R[i];
            if (!double.IsFinite(r) || r < 0) continue;
            r += inner;

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
                pts.Add(transform.PolarToPixel(inner, a));
            }

            // A bar at a radius or an angle that cannot be placed is skipped whole, as a cartesian bar is.
            if (!pts.IsDrawable()) continue;
            Ctx.DrawPolygon(pts, new ShapeStyle(fillColor, baseColor, 0.5));
        }
    }
}
