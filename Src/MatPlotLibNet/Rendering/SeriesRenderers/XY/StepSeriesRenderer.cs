// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StepSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class StepSeriesRenderer : SeriesRenderer<StepSeries>
{
    /// <inheritdoc />
    public StepSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(StepSeries series)
    {
        var color = ResolveColor(series.Color);
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = data.X.Length;
        if (n == 0) return;
        // Precompute pixel coordinates for all data points (SIMD batch)
        var pxArr = Transform.TransformX(data.X);
        var pyArr = Transform.TransformY(data.Y);
        var pts = new List<Point>();
        switch (series.StepPosition)
        {
            case StepPosition.Post:
                for (int i = 0; i < n - 1; i++) { pts.Add(new Point(pxArr[i], pyArr[i])); pts.Add(new Point(pxArr[i + 1], pyArr[i])); }
                pts.Add(new Point(pxArr[n - 1], pyArr[n - 1]));
                break;
            case StepPosition.Pre:
                pts.Add(new Point(pxArr[0], pyArr[0]));
                for (int i = 1; i < n; i++) { pts.Add(new Point(pxArr[i - 1], pyArr[i])); pts.Add(new Point(pxArr[i], pyArr[i])); }
                break;
            case StepPosition.Mid:
                // Mid-step midpoints are computed X values not in the original array → TransformX on the fly
                var midX = new double[n - 1];
                for (int i = 0; i < n - 1; i++) midX[i] = (data.X[i] + data.X[i + 1]) / 2;
                var pxMid = Transform.TransformX(midX);
                for (int i = 0; i < n - 1; i++) { pts.Add(new Point(pxArr[i], pyArr[i])); pts.Add(new Point(pxMid[i], pyArr[i])); pts.Add(new Point(pxMid[i], pyArr[i + 1])); }
                pts.Add(new Point(pxArr[n - 1], pyArr[n - 1]));
                break;
        }
        Ctx.DrawLines(pts, color, series.LineWidth, series.LineStyle);
    }

}
