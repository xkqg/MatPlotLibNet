// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="CandlestickSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class CandlestickSeriesRenderer : SeriesRenderer<CandlestickSeries>
{
    /// <inheritdoc />
    public CandlestickSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(CandlestickSeries series)
    {
        // Each bar i occupies slot [i, i+1]; body centered at i+0.5 with bodyWidth fraction.
        double halfW = series.BodyWidth / 2;
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            var color = isUp ? series.UpColor : series.DownColor;
            double bodyTop = Math.Max(series.Open[i], series.Close[i]);
            double bodyBottom = Math.Min(series.Open[i], series.Close[i]);
            double center = i + 0.5;
            Ctx.DrawLine(Transform.DataToPixel(center, series.Low[i]), Transform.DataToPixel(center, series.High[i]), color, 1, LineStyle.Solid);
            var tl = Transform.DataToPixel(center - halfW, bodyTop);
            var br = Transform.DataToPixel(center + halfW, bodyBottom);
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, color, 1);
        }
    }
}
