// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class OhlcBarSeriesRenderer : SeriesRenderer<OhlcBarSeries>
{
    public OhlcBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(OhlcBarSeries series)
    {
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            var color = isUp ? series.UpColor : series.DownColor;
            Ctx.DrawLine(Transform.DataToPixel(i, series.Low[i]), Transform.DataToPixel(i, series.High[i]), color, 1.5, LineStyle.Solid);
            var openPt = Transform.DataToPixel(i, series.Open[i]);
            Ctx.DrawLine(new Point(openPt.X - series.TickWidth * 20, openPt.Y), openPt, color, 1.5, LineStyle.Solid);
            var closePt = Transform.DataToPixel(i, series.Close[i]);
            Ctx.DrawLine(closePt, new Point(closePt.X + series.TickWidth * 20, closePt.Y), color, 1.5, LineStyle.Solid);
        }
    }
}
