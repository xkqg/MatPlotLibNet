// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ViolinSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ViolinSeriesRenderer : SeriesRenderer<ViolinSeries>
{
    /// <inheritdoc />
    public ViolinSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ViolinSeries series)
    {
        // Body fill: theme ViolinBodyColor if set (e.g. 'y'=#BFBF00 in MatplotlibClassic),
        // otherwise fall back to the normal series cycle color.
        var bodyColor  = Context.Theme.ViolinBodyColor  ?? ResolveColor(series.Color);
        // Stats lines: theme ViolinStatsColor if set (e.g. 'r'=#FF0000 in MatplotlibClassic),
        // otherwise fall back to the series cycle color.
        var statsColor = Context.Theme.ViolinStatsColor ?? ResolveColor(series.Color);

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
            Ctx.DrawPolygon(outline, ApplyAlpha(bodyColor, series.Alpha), bodyColor, 1);

            // ShowExtrema: draw vertical bar + horizontal ticks at min/max using stats color.
            if (series.ShowExtrema)
            {
                double extHalfW = series.Widths * 0.3;
                // Vertical center bar spanning min→max
                Ctx.DrawLine(Transform.DataToPixel(pos, min), Transform.DataToPixel(pos, max), statsColor, 1.5, LineStyle.Solid);
                // Horizontal tick at min
                Ctx.DrawLine(Transform.DataToPixel(pos - extHalfW, min), Transform.DataToPixel(pos + extHalfW, min), statsColor, 1.5, LineStyle.Solid);
                // Horizontal tick at max
                Ctx.DrawLine(Transform.DataToPixel(pos - extHalfW, max), Transform.DataToPixel(pos + extHalfW, max), statsColor, 1.5, LineStyle.Solid);
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
