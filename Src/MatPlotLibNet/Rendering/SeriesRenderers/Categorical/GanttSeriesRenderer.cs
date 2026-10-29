// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="GanttSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class GanttSeriesRenderer : SeriesRenderer<GanttSeries>
{
    /// <inheritdoc />
    public GanttSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(GanttSeries series, MarkObstacles.Builder marks)
    {
        foreach (var bar in Bars(series)) marks.AddBox(bar);
    }

    /// <summary>The task bars that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands
    /// to a legend. A task with an end that cannot be placed draws no bar.</summary>
    private IEnumerable<Rect> Bars(GanttSeries series)
    {
        double halfH = series.BarHeight / 2;
        for (int i = 0; i < series.Tasks.Length; i++)
        {
            var bar = Rect.FromCorners(Transform.DataToPixel(series.Starts[i], i + halfH), Transform.DataToPixel(series.Ends[i], i - halfH));
            if (bar.IsDrawable()) yield return bar;
        }
    }

    /// <inheritdoc />
    public override void Render(GanttSeries series)
    {
        var color = ResolveColor(series.Color);
        foreach (var bar in Bars(series))
            Ctx.DrawRectangle(bar, new ShapeStyle(color, null, 0));
    }
}
