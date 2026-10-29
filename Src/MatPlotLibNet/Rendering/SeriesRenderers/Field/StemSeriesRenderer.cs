// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StemSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class StemSeriesRenderer : SeriesRenderer<StemSeries>
{
    /// <inheritdoc />
    public StemSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(StemSeries series, MarkObstacles.Builder marks)
    {
        foreach (var stem in Stems(series))
        {
            marks.AddPoint(stem.Head);
            if (stem.Foot is { } foot) marks.AddPath([foot, stem.Head]);
        }
    }

    /// <summary>One stem on the plot: the head and, when the axis can place it, the foot.</summary>
    private readonly record struct StemPixels(Point Head, Point? Foot);

    /// <summary>The stems that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend. A stem whose head the axes cannot place is left out whole; one whose foot it cannot place (the baseline 0 on a
    /// log axis) keeps its head.</summary>
    private IEnumerable<StemPixels> Stems(StemSeries series)
    {
        for (int i = 0; i < series.XData.Length; i++)
        {
            var head = Transform.DataToPixel(series.XData[i], series.YData[i]);
            if (!head.IsDrawable()) continue;
            var foot = Transform.DataToPixel(series.XData[i], 0);
            yield return new StemPixels(head, foot.IsDrawable() ? foot : null);
        }
    }

    /// <inheritdoc />
    public override void Render(StemSeries series)
    {
        var stemColor = series.StemColor ?? SeriesColor;
        var markerColor = series.MarkerColor ?? SeriesColor;
        foreach (var stem in Stems(series))
        {
            if (stem.Foot is { } foot) Ctx.DrawLine(foot, stem.Head, new StrokeStyle(stemColor, 1, LineStyle.Solid));
            Ctx.DrawCircle(stem.Head, 4, new ShapeStyle(markerColor, null, 0));
        }

        // The baseline runs at 0 under the stem locations the X axis can place, when the Y axis can place 0.
        if (series.XData.DrawableRange(Transform.XScale) is { } span)
        {
            var left = Transform.DataToPixel(span.Lo, 0);
            var right = Transform.DataToPixel(span.Hi, 0);
            if (left.CanDrawLineTo(right))
                Ctx.DrawLine(left, right, new StrokeStyle(series.BaselineColor ?? Context.Theme.Neutral, 1, LineStyle.Solid));
        }
    }
}
