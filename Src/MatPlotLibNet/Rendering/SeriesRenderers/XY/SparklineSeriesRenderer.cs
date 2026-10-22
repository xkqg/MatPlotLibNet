// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="SparklineSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class SparklineSeriesRenderer : SeriesRenderer<SparklineSeries>
{
    /// <inheritdoc />
    public SparklineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SparklineSeries series)
    {
        if (series.Values.Length < 2) return;
        var color = ResolveColor(series.Color);
        var bounds = Area.PlotBounds;
        // Scaled to the values that are there: a missing value (NaN) and an infinity are left out, and the line breaks
        // there (one NaN made the whole scale NaN, so nothing was drawn).
        if (series.Values.DrawableRange() is not { } values) return;
        var y = values.Normalized();
        double yMin = y.Lo, yMax = y.Hi;
        double xStep = bounds.Width / (series.Values.Length - 1);
        double yScale = bounds.Height / (yMax - yMin);
        var points = new List<Point>(series.Values.Length);
        for (int i = 0; i < series.Values.Length; i++)
            points.Add(new Point(bounds.X + i * xStep, bounds.Y + bounds.Height - (series.Values[i] - yMin) * yScale));
        // A sparkline has no dash pattern of its own (it is a solid line, as an error bar's bars are), so the slot decides only
        // the width.
        DrawPolyline(points, new StrokeStyle(color, Slot.LineWidthOf(series.LineWidth), LineStyle.Solid));
    }
}
