// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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
            double min = data[0], max = data[^1], range = max - min;
            if (range == 0) range = 1;
            double bandwidth = range / 20;
            var left = new List<Point>(); var right = new List<Point>();
            for (int s = 0; s <= 20; s++)
            {
                double y = min + range * s / 20;
                int lo = MathHelpers.BisectLeft(data, y - bandwidth), hi = MathHelpers.BisectRight(data, y + bandwidth);
                double halfW = (hi - lo) / (double)data.Length * 2;
                left.Add(Transform.DataToPixel(i - halfW, y));
                right.Add(Transform.DataToPixel(i + halfW, y));
            }
            right.Reverse();
            var outline = new List<Point>(); outline.AddRange(left); outline.AddRange(right);
            Ctx.DrawPolygon(outline, color.WithAlpha(180), color, 1);
        }
    }
}
