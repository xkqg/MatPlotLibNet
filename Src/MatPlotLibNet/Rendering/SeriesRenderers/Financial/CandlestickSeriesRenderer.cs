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
    internal override void AddMarks(CandlestickSeries series, MarkObstacles.Builder marks)
    {
        foreach (var candle in Candles(series))
        {
            if (candle.Low.CanDrawLineTo(candle.High)) marks.AddPath([candle.Low, candle.High]);
            if (candle.Body.IsDrawable()) marks.AddBox(candle.Body);
        }
    }

    /// <summary>One candle: the colour it wears, its wick and its body.</summary>
    private readonly record struct Candle(Color Colour, Point Low, Point High, Rect Body);

    /// <summary>The candles in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.
    /// Candle i stands in category i's slot (CategorySlot): wick and body on the slot's centre, the body BodyWidth wide. The
    /// wick and the body are marks of their own: a price that cannot be placed skips the one it belongs to.</summary>
    private IEnumerable<Candle> Candles(CandlestickSeries series)
    {
        double halfW = series.BodyWidth / 2;
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            double bodyTop = Math.Max(series.Open[i], series.Close[i]);
            double bodyBottom = Math.Min(series.Open[i], series.Close[i]);
            double center = CategorySlot.Of(i).Centre;
            yield return new Candle(isUp ? series.UpColor : series.DownColor,
                Transform.DataToPixel(center, series.Low[i]), Transform.DataToPixel(center, series.High[i]),
                Rect.FromCorners(Transform.DataToPixel(center - halfW, bodyTop), Transform.DataToPixel(center + halfW, bodyBottom)));
        }
    }

    /// <inheritdoc />
    public override void Render(CandlestickSeries series)
    {
        foreach (var candle in Candles(series))
        {
            if (candle.Low.CanDrawLineTo(candle.High))
                Ctx.DrawLine(candle.Low, candle.High, new StrokeStyle(candle.Colour, 1, LineStyle.Solid));
            if (candle.Body.IsDrawable())
                Ctx.DrawRectangle(candle.Body, new ShapeStyle(candle.Colour, candle.Colour, 1));
        }
    }
}
