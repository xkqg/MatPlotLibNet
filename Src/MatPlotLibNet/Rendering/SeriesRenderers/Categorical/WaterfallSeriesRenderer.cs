// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class WaterfallSeriesRenderer : SeriesRenderer<WaterfallSeries>
{
    public WaterfallSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(WaterfallSeries series)
    {
        double cumulative = 0;
        double halfW = series.BarWidth / 2;
        for (int i = 0; i < series.Values.Length; i++)
        {
            double value = series.Values[i];
            double top = cumulative + value;
            var color = value >= 0 ? series.IncreaseColor : series.DecreaseColor;
            var tl = Transform.DataToPixel(i - halfW, Math.Max(top, cumulative));
            var br = Transform.DataToPixel(i + halfW, Math.Min(top, cumulative));
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, null, 0);
            if (i < series.Values.Length - 1)
            {
                var ls = Transform.DataToPixel(i + halfW, top);
                var le = Transform.DataToPixel(i + 1 - halfW, top);
                Ctx.DrawLine(ls, le, Color.Gray, 0.5, LineStyle.Dashed);
            }
            cumulative = top;
        }
    }
}
