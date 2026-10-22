// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        // Candle i stands in category i's slot (CategorySlot): wick and body on the slot's centre, the body BodyWidth wide.
        double halfW = series.BodyWidth / 2;
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            var color = isUp ? series.UpColor : series.DownColor;
            double bodyTop = Math.Max(series.Open[i], series.Close[i]);
            double bodyBottom = Math.Min(series.Open[i], series.Close[i]);
            double center = CategorySlot.Of(i).Centre;
            // The wick and the body are marks of their own: a price that cannot be placed skips the one it belongs to.
            var low = Transform.DataToPixel(center, series.Low[i]);
            var high = Transform.DataToPixel(center, series.High[i]);
            if (low.CanDrawLineTo(high))
                Ctx.DrawLine(low, high, new StrokeStyle(color, 1, LineStyle.Solid));
            var body = Rect.FromCorners(Transform.DataToPixel(center - halfW, bodyTop), Transform.DataToPixel(center + halfW, bodyBottom));
            if (body.IsDrawable())
                Ctx.DrawRectangle(body, new ShapeStyle(color, color, 1));
        }
    }
}
