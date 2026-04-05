// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class AreaSeriesRenderer : SeriesRenderer<AreaSeries>
{
    public AreaSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(AreaSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? color.WithAlpha((byte)(series.Alpha * 255));
        int n = series.XData.Length;
        if (n == 0) return;
        var polygon = new List<Point>(n * 2);
        for (int i = 0; i < n; i++) polygon.Add(Transform.DataToPixel(series.XData[i], series.YData[i]));
        if (series.YData2 is not null) for (int i = n - 1; i >= 0; i--) polygon.Add(Transform.DataToPixel(series.XData[i], series.YData2[i]));
        else for (int i = n - 1; i >= 0; i--) polygon.Add(Transform.DataToPixel(series.XData[i], 0));
        Ctx.DrawPolygon(polygon, fillColor, null, 0);
        var top = new List<Point>(n);
        for (int i = 0; i < n; i++) top.Add(Transform.DataToPixel(series.XData[i], series.YData[i]));
        Ctx.DrawLines(top, color, series.LineWidth, series.LineStyle);
    }
}
