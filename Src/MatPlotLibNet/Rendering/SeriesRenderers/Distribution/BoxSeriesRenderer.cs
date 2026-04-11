// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BoxSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class BoxSeriesRenderer : SeriesRenderer<BoxSeries>
{
    /// <inheritdoc />
    public BoxSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(BoxSeries series)
    {
        var color = ResolveColor(series.Color);
        double halfW = series.Widths / 2;
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i].OrderBy(v => v).ToArray();
            if (data.Length == 0) continue;
            double pos = series.Positions is not null && i < series.Positions.Length ? series.Positions[i] : i;
            double q1 = MathHelpers.Percentile(data, 25), median = MathHelpers.Percentile(data, 50), q3 = MathHelpers.Percentile(data, 75);
            double iqr = q3 - q1;
            double whisLo = Math.Max(data[0], q1 - series.Whis * iqr);
            double whisHi = Math.Min(data[^1], q3 + series.Whis * iqr);

            if (series.Vert)
            {
                var tl = Transform.DataToPixel(pos - halfW, q3);
                var br = Transform.DataToPixel(pos + halfW, q1);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), null, color, 1.5);
                Ctx.DrawLine(Transform.DataToPixel(pos - halfW, median), Transform.DataToPixel(pos + halfW, median), series.MedianColor ?? Colors.Red, 2, LineStyle.Solid);
                Ctx.DrawLine(Transform.DataToPixel(pos, q3), Transform.DataToPixel(pos, whisHi), color, 1, LineStyle.Solid);
                Ctx.DrawLine(Transform.DataToPixel(pos, q1), Transform.DataToPixel(pos, whisLo), color, 1, LineStyle.Solid);
                if (series.ShowMeans)
                {
                    double mean = data.Average();
                    Ctx.DrawCircle(Transform.DataToPixel(pos, mean), 4, Colors.Green, null, 0);
                }
            }
            else
            {
                // Horizontal orientation — swap axes
                var tl = Transform.DataToPixel(q1, pos - halfW);
                var br = Transform.DataToPixel(q3, pos + halfW);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), null, color, 1.5);
                Ctx.DrawLine(Transform.DataToPixel(median, pos - halfW), Transform.DataToPixel(median, pos + halfW), series.MedianColor ?? Colors.Red, 2, LineStyle.Solid);
                Ctx.DrawLine(Transform.DataToPixel(q3, pos), Transform.DataToPixel(whisHi, pos), color, 1, LineStyle.Solid);
                Ctx.DrawLine(Transform.DataToPixel(q1, pos), Transform.DataToPixel(whisLo, pos), color, 1, LineStyle.Solid);
                if (series.ShowMeans)
                {
                    double mean = data.Average();
                    Ctx.DrawCircle(Transform.DataToPixel(mean, pos), 4, Colors.Green, null, 0);
                }
            }
        }
    }
}
