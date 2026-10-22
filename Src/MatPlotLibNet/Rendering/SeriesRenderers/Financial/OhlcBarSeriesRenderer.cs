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
    public override void Render(OhlcBarSeries series)
    {
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            var color = isUp ? series.UpColor : series.DownColor;
            // The line and the two ticks are marks of their own: a price that cannot be placed skips the one it belongs to.
            var low = Transform.DataToPixel(i, series.Low[i]);
            var high = Transform.DataToPixel(i, series.High[i]);
            if (low.CanDrawLineTo(high))
                Ctx.DrawLine(low, high, new StrokeStyle(color, 1.5, LineStyle.Solid));
            var openPt = Transform.DataToPixel(i, series.Open[i]);
            var openTick = new Point(openPt.X - series.TickWidth * 20, openPt.Y);
            if (openTick.CanDrawLineTo(openPt))
                Ctx.DrawLine(openTick, openPt, new StrokeStyle(color, 1.5, LineStyle.Solid));
            var closePt = Transform.DataToPixel(i, series.Close[i]);
            var closeTick = new Point(closePt.X + series.TickWidth * 20, closePt.Y);
            if (closePt.CanDrawLineTo(closeTick))
                Ctx.DrawLine(closePt, closeTick, new StrokeStyle(color, 1.5, LineStyle.Solid));
        }
    }
}
