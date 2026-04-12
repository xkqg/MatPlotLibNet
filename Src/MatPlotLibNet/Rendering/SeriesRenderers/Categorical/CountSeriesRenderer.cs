// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="CountSeries"/> as a frequency bar chart from raw categorical values.</summary>
internal sealed class CountSeriesRenderer : SeriesRenderer<CountSeries>
{
    /// <inheritdoc />
    public CountSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(CountSeries series)
    {
        if (series.Values.Length == 0) return;

        var color = ResolveColor(series.Color);
        var counts = series.ComputeCounts();
        var categories = counts.Keys.ToArray();
        double halfW = series.BarWidth / 2.0;

        for (int i = 0; i < categories.Length; i++)
        {
            double count = counts[categories[i]];
            if (series.Orientation == BarOrientation.Vertical)
            {
                var tl = Transform.DataToPixel(i - halfW, count);
                var br = Transform.DataToPixel(i + halfW, 0);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, null, 0);
            }
            else
            {
                var tl = Transform.DataToPixel(0, i + halfW);
                var br = Transform.DataToPixel(count, i - halfW);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, null, 0);
            }
        }
    }
}
