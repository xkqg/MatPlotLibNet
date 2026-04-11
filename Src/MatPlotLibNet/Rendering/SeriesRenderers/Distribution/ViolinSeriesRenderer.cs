// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class ViolinSeriesRenderer : SeriesRenderer<ViolinSeries>
{
    public ViolinSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(ViolinSeries series)
    {
        var color = ResolveColor(series.Color);
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i].OrderBy(v => v).ToArray();
            if (data.Length == 0) continue;
            double pos = series.Positions is not null && i < series.Positions.Length ? series.Positions[i] : i;
            double min = data[0], max = data[^1], range = max - min;
            if (range == 0) range = 1;
            double bandwidth = range / 20;
            var left = new List<Point>(); var right = new List<Point>();
            for (int s = 0; s <= 20; s++)
            {
                double y = min + range * s / 20;
                int lo = MathHelpers.BisectLeft(data, y - bandwidth), hi = MathHelpers.BisectRight(data, y + bandwidth);
                double halfW = (hi - lo) / (double)data.Length * series.Widths;
                // Apply Side clipping
                double leftOff = series.Side == ViolinSide.High ? 0 : halfW;
                double rightOff = series.Side == ViolinSide.Low ? 0 : halfW;
                left.Add(Transform.DataToPixel(pos - leftOff, y));
                right.Add(Transform.DataToPixel(pos + rightOff, y));
            }
            right.Reverse();
            var outline = new List<Point>(); outline.AddRange(left); outline.AddRange(right);
            Ctx.DrawPolygon(outline, ApplyAlpha(color, series.Alpha), color, 1);

            // ShowExtrema: draw lines at min and max
            if (series.ShowExtrema)
            {
                double extHalfW = series.Widths * 0.3;
                Ctx.DrawLine(Transform.DataToPixel(pos - extHalfW, min), Transform.DataToPixel(pos + extHalfW, min), color, 1.5, LineStyle.Solid);
                Ctx.DrawLine(Transform.DataToPixel(pos - extHalfW, max), Transform.DataToPixel(pos + extHalfW, max), color, 1.5, LineStyle.Solid);
            }

            // ShowMedians: draw median line across full width
            if (series.ShowMedians)
            {
                double median = MathHelpers.Percentile(data, 50);
                double medHalfW = series.Widths * 0.5;
                Ctx.DrawLine(Transform.DataToPixel(pos - medHalfW, median), Transform.DataToPixel(pos + medHalfW, median), Colors.White, 2, LineStyle.Solid);
            }

            // ShowMeans: draw mean line across full width
            if (series.ShowMeans)
            {
                double mean = data.Average();
                double meanHalfW = series.Widths * 0.5;
                Ctx.DrawLine(Transform.DataToPixel(pos - meanHalfW, mean), Transform.DataToPixel(pos + meanHalfW, mean), Colors.Green, 1.5, LineStyle.Dashed);
            }
        }
    }
}
