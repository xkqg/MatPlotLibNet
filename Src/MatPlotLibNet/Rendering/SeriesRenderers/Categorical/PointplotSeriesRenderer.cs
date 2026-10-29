// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PointplotSeries"/> as mean point + confidence interval whisker per category.</summary>
internal sealed class PointplotSeriesRenderer : SeriesRenderer<PointplotSeries>
{
    /// <inheritdoc />
    public PointplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PointplotSeries series, MarkObstacles.Builder marks)
    {
        foreach (var point in Points(series))
        {
            if (point.Mean.IsDrawable()) marks.AddPoint(point.Mean);
            AddSegment(marks, point.Interval.From, point.Interval.To);
        }
    }

    /// <summary>One category's point: the mean, the vertical interval and its two caps.</summary>
    private readonly record struct PointPixels(Point Mean, PixelSegment Interval, PixelSegment LowerCap, PixelSegment UpperCap);

    /// <summary>The points in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<PointPixels> Points(PointplotSeries series)
    {
        double halfCap = series.CapSize / 2.0;
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i];
            if (data.Length == 0) continue;

            Vec v = data;
            double mean = v.Mean();
            double ci = ComputeCI(v, series.ConfidenceLevel);
            double lower = mean - ci;
            double upper = mean + ci;

            yield return new PointPixels(
                Transform.DataToPixel(i, mean),
                new PixelSegment(Transform.DataToPixel(i, lower), Transform.DataToPixel(i, upper)),
                new PixelSegment(Transform.DataToPixel(i - halfCap, lower), Transform.DataToPixel(i + halfCap, lower)),
                new PixelSegment(Transform.DataToPixel(i - halfCap, upper), Transform.DataToPixel(i + halfCap, upper)));
        }
    }

    /// <inheritdoc />
    public override void Render(PointplotSeries series)
    {
        var color = ResolveColor(series.Color);
        double r = series.MarkerSize / 2.0;
        var stroke = new StrokeStyle(color, 1.5, Styling.LineStyle.Solid);

        foreach (var point in Points(series))
        {
            // Vertical CI line
            Ctx.DrawLine(point.Interval.From, point.Interval.To, stroke);

            // Cap lines
            Ctx.DrawLine(point.LowerCap.From, point.LowerCap.To, stroke);
            Ctx.DrawLine(point.UpperCap.From, point.UpperCap.To, stroke);

            // Mean circle
            Ctx.DrawCircle(point.Mean, r, new ShapeStyle(color, null, 0));
        }
    }

    private static double ComputeCI(Vec v, double level)
    {
        if (v.Length < 2) return 0;
        // 95th percentile range as approximation for CI width
        double p = (1.0 - level) / 2.0 * 100.0;
        double lower = v.Percentile(p);
        double upper = v.Percentile(100.0 - p);
        return (upper - lower) / 2.0;
    }
}
