// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="CountSeries"/> as a frequency bar chart from raw categorical values.</summary>
internal sealed class CountSeriesRenderer : SeriesRenderer<CountSeries>
{
    /// <inheritdoc />
    public CountSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(CountSeries series, MarkObstacles.Builder marks)
    {
        foreach (var bar in Bars(series)) marks.AddBox(bar);
    }

    /// <summary>The bars that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend. A count's bar stands on 0, which a log axis clips to the edge where it begins, as matplotlib clips it; a bar
    /// the axes cannot place is left out. Along the category axis it stands centred in its category's slot
    /// (CategorySlot), where the axis writes the category's name.</summary>
    private IEnumerable<Rect> Bars(CountSeries series)
    {
        if (series.Values.Length == 0) yield break;

        var counts = series.ComputeCounts();
        var categories = counts.Keys.ToArray();
        for (int i = 0; i < categories.Length; i++)
        {
            double count = counts[categories[i]];
            var slot = CategorySlot.Of(i).Bar(series.BarWidth, 0, BarAlignment.Center);
            var bar = Transform.BoxOnFoot(slot, 0, count, rising: true, series.Orientation).Rect;
            if (bar.IsDrawable()) yield return bar;
        }
    }

    /// <inheritdoc />
    public override void Render(CountSeries series)
    {
        var color = ResolveColor(series.Color);
        foreach (var bar in Bars(series))
            Ctx.DrawRectangle(bar, new ShapeStyle(color, null, 0));
    }
}
