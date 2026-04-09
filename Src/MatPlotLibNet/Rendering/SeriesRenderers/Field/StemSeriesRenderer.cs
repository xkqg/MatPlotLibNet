// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class StemSeriesRenderer : SeriesRenderer<StemSeries>
{
    public StemSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(StemSeries series)
    {
        var stemColor = series.StemColor ?? SeriesColor;
        var markerColor = series.MarkerColor ?? SeriesColor;
        for (int i = 0; i < series.XData.Length; i++)
        {
            Ctx.DrawLine(Transform.DataToPixel(series.XData[i], 0), Transform.DataToPixel(series.XData[i], series.YData[i]), stemColor, 1, LineStyle.Solid);
            Ctx.DrawCircle(Transform.DataToPixel(series.XData[i], series.YData[i]), 4, markerColor, null, 0);
        }
        if (series.XData.Length > 0)
            Ctx.DrawLine(Transform.DataToPixel(series.XData.Min(), 0), Transform.DataToPixel(series.XData.Max(), 0), series.BaselineColor ?? Colors.Gray, 1, LineStyle.Solid);
    }
}
