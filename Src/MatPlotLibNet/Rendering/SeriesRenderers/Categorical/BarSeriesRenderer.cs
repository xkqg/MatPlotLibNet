// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class BarSeriesRenderer : SeriesRenderer<BarSeries>
{
    public BarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(BarSeries series)
    {
        var color = ResolveColor(series.Color);
        for (int i = 0; i < series.Categories.Length; i++)
        {
            double halfW = series.BarWidth / 2;
            double baseline = series.StackBaseline is not null ? series.StackBaseline[i] : 0;
            if (series.Orientation == BarOrientation.Vertical)
            {
                var tl = Transform.DataToPixel(i - halfW, baseline + Math.Max(series.Values[i], 0));
                var br = Transform.DataToPixel(i + halfW, baseline + Math.Min(series.Values[i], 0));
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, series.EdgeColor, series.EdgeColor.HasValue ? 1 : 0);
            }
            else
            {
                var tl = Transform.DataToPixel(baseline + Math.Min(series.Values[i], 0), i + halfW);
                var br = Transform.DataToPixel(baseline + Math.Max(series.Values[i], 0), i - halfW);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, series.EdgeColor, series.EdgeColor.HasValue ? 1 : 0);
            }
        }
    }
}
