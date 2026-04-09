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
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = data.X.Length;
        if (n == 0) return;
        var pts = new List<Point>();
        switch (series.StepPosition)
        {
            case StepPosition.Post:
                for (int i = 0; i < n - 1; i++) { pts.Add(Transform.DataToPixel(data.X[i], data.Y[i])); pts.Add(Transform.DataToPixel(data.X[i + 1], data.Y[i])); }
                pts.Add(Transform.DataToPixel(data.X[n - 1], data.Y[n - 1]));
                break;
            case StepPosition.Pre:
                pts.Add(Transform.DataToPixel(data.X[0], data.Y[0]));
                for (int i = 1; i < n; i++) { pts.Add(Transform.DataToPixel(data.X[i - 1], data.Y[i])); pts.Add(Transform.DataToPixel(data.X[i], data.Y[i])); }
                break;
            case StepPosition.Mid:
                for (int i = 0; i < n - 1; i++) { double mx = (data.X[i] + data.X[i + 1]) / 2; pts.Add(Transform.DataToPixel(data.X[i], data.Y[i])); pts.Add(Transform.DataToPixel(mx, data.Y[i])); pts.Add(Transform.DataToPixel(mx, data.Y[i + 1])); }
                pts.Add(Transform.DataToPixel(data.X[n - 1], data.Y[n - 1]));
                break;
        }
        Ctx.DrawLines(pts, color, series.LineWidth, series.LineStyle);
    }

    private XYData ApplyDownsampling(double[] x, double[] y, int? maxPoints)
    {
        if (maxPoints is null || x.Length <= maxPoints.Value) return new(x, y);
        var culled = ViewportCuller.Cull(x, y, Transform.DataXMin, Transform.DataXMax);
        if (culled.X.Length <= maxPoints.Value) return culled;
        return new LttbDownsampler().Downsample(culled.X, culled.Y, maxPoints.Value);
    }
}
