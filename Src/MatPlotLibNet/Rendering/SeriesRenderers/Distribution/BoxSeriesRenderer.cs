// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class BoxSeriesRenderer : SeriesRenderer<BoxSeries>
{
    public BoxSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(BoxSeries series)
    {
        var color = ResolveColor(series.Color);
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i].OrderBy(v => v).ToArray();
            if (data.Length == 0) continue;
            double q1 = MathHelpers.Percentile(data, 25), median = MathHelpers.Percentile(data, 50), q3 = MathHelpers.Percentile(data, 75);
            double halfW = 0.35;
            var tl = Transform.DataToPixel(i - halfW, q3); var br = Transform.DataToPixel(i + halfW, q1);
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), null, color, 1.5);
            Ctx.DrawLine(Transform.DataToPixel(i - halfW, median), Transform.DataToPixel(i + halfW, median), series.MedianColor ?? Colors.Red, 2, LineStyle.Solid);
            Ctx.DrawLine(Transform.DataToPixel(i, q3), Transform.DataToPixel(i, data[^1]), color, 1, LineStyle.Solid);
            Ctx.DrawLine(Transform.DataToPixel(i, q1), Transform.DataToPixel(i, data[0]), color, 1, LineStyle.Solid);
        }
    }
}
