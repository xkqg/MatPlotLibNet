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
        foreach (var bar in Bars(series)) marks.AddBox(bar.Box);
    }

    /// <summary>A task's bar: which task it is and the rectangle it fills.</summary>
    /// <param name="Task">The task's index.</param>
    /// <param name="Box">The bar, in pixels.</param>
    private readonly record struct GanttBar(int Task, Rect Box);

    /// <summary>The task bars that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands
    /// to a legend. Task <c>i</c> stands in the category slot <c>i</c> from the top (<see cref="CategorySlot.FromTop"/>), where the Y
    /// axis names it; a task with an end that cannot be placed draws no bar.</summary>
    private IEnumerable<GanttBar> Bars(GanttSeries series)
    {
        double halfH = series.BarHeight / 2;
        int rows = series.Tasks.Length;
        for (int i = 0; i < rows; i++)
        {
            double centre = CategorySlot.FromTop(i, rows).Centre;
            var bar = Rect.FromCorners(Transform.DataToPixel(series.Starts[i], centre + halfH), Transform.DataToPixel(series.Ends[i], centre - halfH));
            if (bar.IsDrawable()) yield return new GanttBar(i, bar);
        }
    }

    /// <inheritdoc />
    public override void Render(GanttSeries series)
    {
        var edges = Context.Theme.PatchEdgesOf(series);
        foreach (var bar in Bars(series))
        {
            // The colour given to the chart, else the task's own colour of the cycle (one slot for each task, ColorSlotSpan); a bar's edge
            // is matplotlib's patch rule under the matplotlib themes (barh: black 1 pt under classic), none otherwise.
            var color = series.Color ?? PartColor(bar.Task);
            Ctx.DrawRectangle(bar.Box, edges.StyleOf(color, color));
        }
    }
}