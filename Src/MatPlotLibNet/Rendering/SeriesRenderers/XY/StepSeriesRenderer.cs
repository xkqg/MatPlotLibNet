// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class StepSeriesRenderer : SeriesRenderer<StepSeries>
{
    public StepSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(StepSeries series)
    {
        var color = ResolveColor(series.Color);
        int n = series.XData.Length;
        if (n == 0) return;
        var pts = new List<Point>();
        switch (series.StepPosition)
        {
            case StepPosition.Post:
                for (int i = 0; i < n - 1; i++) { pts.Add(Transform.DataToPixel(series.XData[i], series.YData[i])); pts.Add(Transform.DataToPixel(series.XData[i + 1], series.YData[i])); }
                pts.Add(Transform.DataToPixel(series.XData[n - 1], series.YData[n - 1]));
                break;
            case StepPosition.Pre:
                pts.Add(Transform.DataToPixel(series.XData[0], series.YData[0]));
                for (int i = 1; i < n; i++) { pts.Add(Transform.DataToPixel(series.XData[i - 1], series.YData[i])); pts.Add(Transform.DataToPixel(series.XData[i], series.YData[i])); }
                break;
            case StepPosition.Mid:
                for (int i = 0; i < n - 1; i++) { double mx = (series.XData[i] + series.XData[i + 1]) / 2; pts.Add(Transform.DataToPixel(series.XData[i], series.YData[i])); pts.Add(Transform.DataToPixel(mx, series.YData[i])); pts.Add(Transform.DataToPixel(mx, series.YData[i + 1])); }
                pts.Add(Transform.DataToPixel(series.XData[n - 1], series.YData[n - 1]));
                break;
        }
        Ctx.DrawLines(pts, color, series.LineWidth, series.LineStyle);
    }
}
