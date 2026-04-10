// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PointplotSeries"/> as mean point + confidence interval whisker per category.</summary>
internal sealed class PointplotSeriesRenderer : SeriesRenderer<PointplotSeries>
{
    public PointplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(PointplotSeries series)
    {
        if (series.Datasets.Length == 0) return;

        var color = ResolveColor(series.Color);
        double r = series.MarkerSize / 2.0;
        double halfCap = series.CapSize / 2.0;

        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i];
            if (data.Length == 0) continue;

            Vec v = data;
            double mean = v.Mean();
            double ci = ComputeCI(v, series.ConfidenceLevel);
            double lower = mean - ci;
            double upper = mean + ci;

            var meanPx = Transform.DataToPixel(i, mean);
            var lowerPx = Transform.DataToPixel(i, lower);
            var upperPx = Transform.DataToPixel(i, upper);

            // Vertical CI line
            Ctx.DrawLine(lowerPx, upperPx, color, 1.5, Styling.LineStyle.Solid);

            // Cap lines
            var capLeft = Transform.DataToPixel(i - halfCap, lower);
            var capRight = Transform.DataToPixel(i + halfCap, lower);
            Ctx.DrawLine(capLeft, capRight, color, 1.5, Styling.LineStyle.Solid);

            capLeft = Transform.DataToPixel(i - halfCap, upper);
            capRight = Transform.DataToPixel(i + halfCap, upper);
            Ctx.DrawLine(capLeft, capRight, color, 1.5, Styling.LineStyle.Solid);

            // Mean circle
            Ctx.DrawCircle(meanPx, r, color, null, 0);
        }
    }

    private static double ComputeCI(Vec v, double level)
    {
        if (v.Length < 2) return 0;
        // 95th percentile range as approximation for CI width
        double p = (1.0 - level) / 2.0 * 100.0;
        double lower = v.Percentile(p);
        double upper = v.Percentile(100.0 - p);
        return (upper - lower) / 2.0;
    }
}
