// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class AreaSeriesRenderer : SeriesRenderer<AreaSeries>
{
    public AreaSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(AreaSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? color.WithAlpha((byte)(series.Alpha * 255));
        var (xData, yData) = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = xData.Length;
        if (n == 0) return;
        var polygon = new List<Point>(n * 2);
        for (int i = 0; i < n; i++) polygon.Add(Transform.DataToPixel(xData[i], yData[i]));
        if (series.YData2 is not null) for (int i = n - 1; i >= 0; i--) polygon.Add(Transform.DataToPixel(xData[i], series.YData2[i]));
        else for (int i = n - 1; i >= 0; i--) polygon.Add(Transform.DataToPixel(xData[i], 0));
        Ctx.DrawPolygon(polygon, fillColor, null, 0);
        var top = new List<Point>(n);
        for (int i = 0; i < n; i++) top.Add(Transform.DataToPixel(xData[i], yData[i]));
        Ctx.DrawLines(top, color, series.LineWidth, series.LineStyle);
    }

    private (double[] X, double[] Y) ApplyDownsampling(double[] x, double[] y, int? maxPoints)
    {
        if (maxPoints is null || x.Length <= maxPoints.Value) return (x, y);
        var (cx, cy) = ViewportCuller.Cull(x, y, Transform.DataXMin, Transform.DataXMax);
        if (cx.Length <= maxPoints.Value) return (cx, cy);
        return new LttbDownsampler().Downsample(cx, cy, maxPoints.Value);
    }
}
