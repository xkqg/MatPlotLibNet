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
    private readonly record struct Bubble(Point Centre, double Radius, int Row);

    /// <summary>The bubbles that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to
    /// a legend. A bubble at a point the axes cannot place, or whose size is no area (missing, infinite, negative), is left
    /// out: the scatter's rule (matplotlib masks x, y and s together). A bubble's size is an area in points squared, so it is
    /// sqrt(size) points across and a point is a fixed number of pixels: the one rule a scatter's size is drawn by
    /// (<c>AreaAcrossInPoints</c>).</summary>
    private IEnumerable<Bubble> Bubbles(BubbleSeries series)
    {
        var pts = Transform.TransformBatch(series.XData, series.YData);
        for (int i = 0; i < pts.Length; i++)
        {
            double radius = series.Sizes[i].AreaAcrossInPoints.PointsInPixels / 2;
            if (pts[i].IsDrawable() && radius.IsDrawable()) yield return new Bubble(pts[i], radius, i);
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
        // The colour is the series' own, else the theme's scatter face, else the cycle's (matplotlib's scatter takes "b" in the
        // classic style and never asks the cycle). A theme that names a scatter edge strokes the bubble with it, faded to the series'
        // opacity as the marker's face is; the library's own themes name none and keep the edge in the bubble's colour.
        var style = Slot.BubbleStyleOf(ColorOf(series), series.Alpha);
        foreach (var bubble in Bubbles(series))
        {
            // The bubble is one row of the data: say which, so a script can find the same row in another chart.
            if (series.RowKeys is { } keys) Ctx.SetNextElementData("row", keys[bubble.Row]);
            Ctx.DrawCircle(bubble.Centre, bubble.Radius, style);
        }
    }
}
