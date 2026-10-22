// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ViolinSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ViolinSeriesRenderer : SeriesRenderer<ViolinSeries>
{
    /// <inheritdoc />
    public ViolinSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ViolinSeries series)
    {
        // The body's colour and the colour of the lines over it come from the slot, the one owner the legend swatch asks
        // too: the colour the series names, else the colour its theme gives a violin (e.g. 'y'=#BFBF00 and 'r'=#FF0000 in
        // MatplotlibClassic), else the theme's colour at the series' slot.
        var bodyColor  = Slot.ColorOf(series);
        var statsColor = Slot.StatsColorOf(series);

        for (int i = 0; i < series.Datasets.Length; i++)
        {
            // The violin is drawn from the observations that are there: a missing one (NaN) and an infinity are left
            // out, as they are left out of the range.
            var data = series.Datasets[i].SortedDrawableValues();
            if (data.Length == 0) continue;
            double pos = series.Positions.PositionOf(i);
            double min = data[0], max = data[^1];

            // The body and the extrema bar stand on the sample's lowest value and reach its highest, as a bar stands on
            // its foot: a value the axis cannot place (0 or below on a log axis) is clipped to the edge where the axis
            // begins, as matplotlib clips it, and a violin whose highest value it cannot place lies wholly beyond the axis
            // and is skipped. The ticks at the extrema, the median and the mean stand at one value: skipped when it cannot
            // be placed.
            if (Transform.DataToPixel(pos, max).IsDrawable())
            {
                var left = new List<Point>(); var right = new List<Point>();
                foreach (var slice in data.Outline(series.Widths))
                {
                    // Apply Side clipping
                    left.Add(Transform.DataToPixelClippedY(pos - series.BodyLeft(slice.HalfWidth), slice.Y));
                    right.Add(Transform.DataToPixelClippedY(pos + series.BodyRight(slice.HalfWidth), slice.Y));
                }
                right.Reverse();
                var outline = new List<Point>(); outline.AddRange(left); outline.AddRange(right);
                if (outline.IsDrawable())
                    Ctx.DrawPolygon(outline, new ShapeStyle(ApplyAlpha(bodyColor, series.Alpha), bodyColor, 1));
            }

            var statsStroke = new StrokeStyle(statsColor, 1.5, LineStyle.Solid);

            // ShowExtrema: draw vertical bar + horizontal ticks at min/max using stats color.
            if (series.ShowExtrema)
            {
                double extHalfW = series.ExtremaHalfWidth;
                // Vertical center bar spanning min→max
                DrawSegment(Transform.DataToPixelClippedY(pos, min), Transform.DataToPixel(pos, max), statsStroke);
                // Horizontal tick at min
                DrawSegment(Transform.DataToPixel(pos - extHalfW, min), Transform.DataToPixel(pos + extHalfW, min), statsStroke);
                // Horizontal tick at max
                DrawSegment(Transform.DataToPixel(pos - extHalfW, max), Transform.DataToPixel(pos + extHalfW, max), statsStroke);
            }

            // ShowMedians: draw median line across full width
            if (series.ShowMedians)
            {
                double median = data.Percentile(50);
                double medHalfW = series.StatsHalfWidth;
                DrawSegment(Transform.DataToPixel(pos - medHalfW, median), Transform.DataToPixel(pos + medHalfW, median), new StrokeStyle(Colors.White, 2, LineStyle.Solid));
            }

            // ShowMeans: draw mean line across full width
            if (series.ShowMeans)
            {
                double mean = data.Average();
                double meanHalfW = series.StatsHalfWidth;
                DrawSegment(Transform.DataToPixel(pos - meanHalfW, mean), Transform.DataToPixel(pos + meanHalfW, mean), new StrokeStyle(Colors.Green, 1.5, LineStyle.Dashed));
            }
        }
    }
}
