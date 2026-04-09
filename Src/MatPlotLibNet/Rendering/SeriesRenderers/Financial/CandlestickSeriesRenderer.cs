// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class CandlestickSeriesRenderer : SeriesRenderer<CandlestickSeries>
{
    public CandlestickSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(CandlestickSeries series)
    {
        double halfW = series.BodyWidth / 2;
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            var color = isUp ? series.UpColor : series.DownColor;
            double bodyTop = Math.Max(series.Open[i], series.Close[i]);
            double bodyBottom = Math.Min(series.Open[i], series.Close[i]);
            Ctx.DrawLine(Transform.DataToPixel(i, series.Low[i]), Transform.DataToPixel(i, series.High[i]), color, 1, LineStyle.Solid);
            var tl = Transform.DataToPixel(i - halfW, bodyTop); var br = Transform.DataToPixel(i + halfW, bodyBottom);
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, color, 1);
        }
    }
}
