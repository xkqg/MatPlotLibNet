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

    /// <summary>One bubble as it stands on the plot: its centre and its radius in pixels.</summary>
    private readonly record struct Bubble(Point Centre, double Radius);

    /// <summary>The bubbles that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to
    /// a legend. A bubble at a point the axes cannot place, or whose size is no area (missing, infinite, negative), is left
    /// out: the scatter's rule (matplotlib masks x, y and s together).</summary>
    private IEnumerable<Bubble> Bubbles(BubbleSeries series)
    {
        var pts = Transform.TransformBatch(series.XData, series.YData);
        for (int i = 0; i < pts.Length; i++)
        {
            double radius = Math.Sqrt(series.Sizes[i]) / 2;
            if (pts[i].IsDrawable() && radius.IsDrawable()) yield return new Bubble(pts[i], radius);
        }
    }

    /// <inheritdoc />
    internal override void AddMarks(BubbleSeries series, MarkObstacles.Builder marks)
    {
        foreach (var bubble in Bubbles(series)) marks.AddPoint(bubble.Centre);
    }

    /// <inheritdoc />
    public override void Render(BubbleSeries series)
    {
        var color = ResolveColor(series.Color);
        var fill = ApplyAlpha(color, series.Alpha);
        foreach (var bubble in Bubbles(series))
            Ctx.DrawCircle(bubble.Centre, bubble.Radius, new ShapeStyle(fill, color, 1));
    }
}
