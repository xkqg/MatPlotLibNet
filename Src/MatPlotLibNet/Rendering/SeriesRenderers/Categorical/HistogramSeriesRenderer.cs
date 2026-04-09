// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class HistogramSeriesRenderer : SeriesRenderer<HistogramSeries>
{
    public HistogramSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(HistogramSeries series)
    {
        var color = ResolveColor(series.Color);
        if (series.Data.Length == 0) return;
        var bins = series.ComputeBins();
        for (int i = 0; i < bins.Counts.Length; i++)
        {
            double x0 = bins.Min + i * bins.BinWidth;
            var tl = Transform.DataToPixel(x0, bins.Counts[i]);
            var br = Transform.DataToPixel(x0 + bins.BinWidth, 0);
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, series.EdgeColor ?? Colors.White, 0.5);
        }
    }
}
