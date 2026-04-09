// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class BubbleSeriesRenderer : SeriesRenderer<BubbleSeries>
{
    public BubbleSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(BubbleSeries series)
    {
        var color = ResolveColor(series.Color);
        var fill = color.WithAlpha((byte)(series.Alpha * 255));
        var pts = Transform.TransformBatch(series.XData, series.YData);
        for (int i = 0; i < pts.Length; i++)
            Ctx.DrawCircle(pts[i], Math.Sqrt(series.Sizes[i]) / 2, fill, color, 1);
    }
}
