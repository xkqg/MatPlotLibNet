// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        {
            // A bubble at a point the axes cannot place, or whose size is no area (missing, infinite, negative), is not
            // drawn: the scatter's rule (matplotlib masks x, y and s together).
            double radius = Math.Sqrt(series.Sizes[i]) / 2;
            if (!pts[i].IsDrawable() || !radius.IsDrawable()) continue;
            Ctx.DrawCircle(pts[i], radius, new ShapeStyle(fill, color, 1));
        }
    }
}
