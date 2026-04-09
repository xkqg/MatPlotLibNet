// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class ScatterSeriesRenderer : SeriesRenderer<ScatterSeries>
{
    public ScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(ScatterSeries series)
    {
        var color = ResolveColor(series.Color);
        // Viewport culling for scatter (no LTTB — scatter points don't need visual-shape preservation)
        var data = series.MaxDisplayPoints.HasValue
            ? ViewportCuller.Cull(series.XData, series.YData, Transform.DataXMin, Transform.DataXMax)
            : new XYData(series.XData, series.YData);

        for (int i = 0; i < data.X.Length; i++)
        {
            BeginTooltip($"x={data.X[i]:G5}, y={data.Y[i]:G5}");
            var pt = Transform.DataToPixel(data.X[i], data.Y[i]);
            double size = series.Sizes is not null ? Math.Sqrt(series.Sizes[i]) : Math.Sqrt(series.MarkerSize);
            var c = series.Colors is not null ? series.Colors[i] : color;
            Ctx.DrawCircle(pt, size / 2, c, null, 0);
            EndTooltip();
        }
    }
}
