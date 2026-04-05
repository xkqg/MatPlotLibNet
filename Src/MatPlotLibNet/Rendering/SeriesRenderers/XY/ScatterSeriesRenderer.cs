// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class ScatterSeriesRenderer : SeriesRenderer<ScatterSeries>
{
    public ScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(ScatterSeries series)
    {
        var color = ResolveColor(series.Color);
        for (int i = 0; i < series.XData.Length; i++)
        {
            BeginTooltip($"x={series.XData[i]:G5}, y={series.YData[i]:G5}");
            var pt = Transform.DataToPixel(series.XData[i], series.YData[i]);
            double size = series.Sizes is not null ? Math.Sqrt(series.Sizes[i]) : Math.Sqrt(series.MarkerSize);
            var c = series.Colors is not null ? series.Colors[i] : color;
            Ctx.DrawCircle(pt, size / 2, c, null, 0);
            EndTooltip();
        }
    }
}
