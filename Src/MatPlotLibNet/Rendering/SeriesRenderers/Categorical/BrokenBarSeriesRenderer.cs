// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="BrokenBarSeries"/> as horizontal bars with gaps per row.</summary>
internal sealed class BrokenBarSeriesRenderer : SeriesRenderer<BrokenBarSeries>
{
    /// <inheritdoc />
    public BrokenBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(BrokenBarSeries series, MarkObstacles.Builder marks)
    {
        foreach (var segment in Segments(series)) marks.AddBox(segment);
    }

    /// <summary>The segments that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to
    /// a legend. A segment that cannot be placed is left out.</summary>
    private IEnumerable<Rect> Segments(BrokenBarSeries series)
    {
        double halfH = series.BarHeight / 2.0;
        for (int i = 0; i < series.Ranges.Length; i++)
        {
            foreach (var (start, width) in series.Ranges[i])
            {
                var segment = Rect.FromCorners(Transform.DataToPixel(start, i + halfH), Transform.DataToPixel(start + width, i - halfH));
                if (segment.IsDrawable()) yield return segment;
            }
        }
    }

    /// <inheritdoc />
    public override void Render(BrokenBarSeries series)
    {
        var color = ResolveColor(series.Color);
        foreach (var segment in Segments(series))
            Ctx.DrawRectangle(segment, new ShapeStyle(color, null, 0));
    }
}
