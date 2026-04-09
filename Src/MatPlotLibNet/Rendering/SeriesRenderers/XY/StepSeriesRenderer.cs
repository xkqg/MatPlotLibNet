// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class StepSeriesRenderer : SeriesRenderer<StepSeries>
{
    public StepSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(StepSeries series)
    {
        var color = ResolveColor(series.Color);
        var (xData, yData) = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = xData.Length;
        if (n == 0) return;
        var pts = new List<Point>();
        switch (series.StepPosition)
        {
            case StepPosition.Post:
                for (int i = 0; i < n - 1; i++) { pts.Add(Transform.DataToPixel(xData[i], yData[i])); pts.Add(Transform.DataToPixel(xData[i + 1], yData[i])); }
                pts.Add(Transform.DataToPixel(xData[n - 1], yData[n - 1]));
                break;
            case StepPosition.Pre:
                pts.Add(Transform.DataToPixel(xData[0], yData[0]));
                for (int i = 1; i < n; i++) { pts.Add(Transform.DataToPixel(xData[i - 1], yData[i])); pts.Add(Transform.DataToPixel(xData[i], yData[i])); }
                break;
            case StepPosition.Mid:
                for (int i = 0; i < n - 1; i++) { double mx = (xData[i] + xData[i + 1]) / 2; pts.Add(Transform.DataToPixel(xData[i], yData[i])); pts.Add(Transform.DataToPixel(mx, yData[i])); pts.Add(Transform.DataToPixel(mx, yData[i + 1])); }
                pts.Add(Transform.DataToPixel(xData[n - 1], yData[n - 1]));
                break;
        }
        Ctx.DrawLines(pts, color, series.LineWidth, series.LineStyle);
    }

    private (double[] X, double[] Y) ApplyDownsampling(double[] x, double[] y, int? maxPoints)
    {
        if (maxPoints is null || x.Length <= maxPoints.Value) return (x, y);
        var (cx, cy) = ViewportCuller.Cull(x, y, Transform.DataXMin, Transform.DataXMax);
        if (cx.Length <= maxPoints.Value) return (cx, cy);
        return new LttbDownsampler().Downsample(cx, cy, maxPoints.Value);
    }
}
