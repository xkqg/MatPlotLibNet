// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="OhlcBarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class OhlcBarSeriesRenderer : SeriesRenderer<OhlcBarSeries>
{
    /// <inheritdoc />
    public OhlcBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(OhlcBarSeries series, MarkObstacles.Builder marks)
    {
        foreach (var bar in Bars(series))
        {
            if (bar.Low.CanDrawLineTo(bar.High)) marks.AddPath([bar.Low, bar.High]);
            if (bar.OpenTick.CanDrawLineTo(bar.Open)) marks.AddPath([bar.OpenTick, bar.Open]);
            if (bar.Close.CanDrawLineTo(bar.CloseTick)) marks.AddPath([bar.Close, bar.CloseTick]);
        }
    }

    /// <summary>One OHLC bar: the colour it wears, its range line and the two ticks with the prices they stand at.</summary>
    private readonly record struct OhlcPixels(Color Colour, Point Low, Point High, Point Open, Point OpenTick, Point Close, Point CloseTick);

    /// <summary>The bars in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.
    /// The line and the two ticks are marks of their own: a price that cannot be placed skips the one it belongs to.</summary>
    private IEnumerable<OhlcPixels> Bars(OhlcBarSeries series)
    {
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            var open = Transform.DataToPixel(i, series.Open[i]);
            var close = Transform.DataToPixel(i, series.Close[i]);
            yield return new OhlcPixels(isUp ? series.UpColor : series.DownColor,
                Transform.DataToPixel(i, series.Low[i]), Transform.DataToPixel(i, series.High[i]),
                open, new Point(open.X - series.TickWidth * 20, open.Y),
                close, new Point(close.X + series.TickWidth * 20, close.Y));
        }
    }

    /// <inheritdoc />
    public override void Render(OhlcBarSeries series)
    {
        foreach (var bar in Bars(series))
        {
            if (bar.Low.CanDrawLineTo(bar.High))
                Ctx.DrawLine(bar.Low, bar.High, new StrokeStyle(bar.Colour, 1.5, LineStyle.Solid));
            if (bar.OpenTick.CanDrawLineTo(bar.Open))
                Ctx.DrawLine(bar.OpenTick, bar.Open, new StrokeStyle(bar.Colour, 1.5, LineStyle.Solid));
            if (bar.Close.CanDrawLineTo(bar.CloseTick))
                Ctx.DrawLine(bar.Close, bar.CloseTick, new StrokeStyle(bar.Colour, 1.5, LineStyle.Solid));
        }
    }
}
