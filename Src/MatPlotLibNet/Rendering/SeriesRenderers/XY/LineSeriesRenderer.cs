// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class LineSeriesRenderer : SeriesRenderer<LineSeries>
{
    public LineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(LineSeries series)
    {
        var color = ResolveColor(series.Color);
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        var points = new List<Point>(data.X.Length);
        for (int i = 0; i < data.X.Length; i++)
            points.Add(Transform.DataToPixel(data.X[i], data.Y[i]));
        Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);
        if (series.Marker is not null && series.Marker != MarkerStyle.None)
            foreach (var pt in points) Ctx.DrawCircle(pt, series.MarkerSize / 2, color, null, 0);
    }

    private XYData ApplyDownsampling(double[] x, double[] y, int? maxPoints)
    {
        if (maxPoints is null || x.Length <= maxPoints.Value) return new(x, y);
        var culled = ViewportCuller.Cull(x, y, Transform.DataXMin, Transform.DataXMax);
        if (culled.X.Length <= maxPoints.Value) return culled;
        return new LttbDownsampler().Downsample(culled.X, culled.Y, maxPoints.Value);
    }
}
