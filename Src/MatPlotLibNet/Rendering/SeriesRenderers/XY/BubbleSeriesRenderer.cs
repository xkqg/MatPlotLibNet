// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BubbleSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class BubbleSeriesRenderer : SeriesRenderer<BubbleSeries>
{
    /// <inheritdoc />
    public BubbleSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(BubbleSeries series)
    {
        var color = ResolveColor(series.Color);
        var fill = ApplyAlpha(color, series.Alpha);
        var pts = Transform.TransformBatch(series.XData, series.YData);
        for (int i = 0; i < pts.Length; i++)
            Ctx.DrawCircle(pts[i], Math.Sqrt(series.Sizes[i]) / 2, fill, color, 1);
    }
}
