// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class LineSeriesRenderer : SeriesRenderer<LineSeries>
{
    public LineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(LineSeries series)
    {
        var color = ResolveColor(series.Color);
        var points = new List<Point>(series.XData.Length);
        for (int i = 0; i < series.XData.Length; i++)
            points.Add(Transform.DataToPixel(series.XData[i], series.YData[i]));
        Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);
        if (series.Marker is not null && series.Marker != MarkerStyle.None)
            foreach (var pt in points) Ctx.DrawCircle(pt, series.MarkerSize / 2, color, null, 0);
    }
}
