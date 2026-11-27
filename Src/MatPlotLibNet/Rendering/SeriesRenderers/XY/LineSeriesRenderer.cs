// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="LineSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class LineSeriesRenderer : SeriesRenderer<LineSeries>
{
    /// <inheritdoc />
    public LineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>What a line stands on in pixels: the samples it keeps after the viewport cull and the point budget, the
    /// samples after the draw style's steps and the smoothing (<paramref name="Drawn"/>), and the path through those, cut where
    /// it leaves the plot towards a value a log axis cannot place (<see cref="DataTransform.TransformPath"/>).</summary>
    private readonly record struct LinePixels(XYData Data, XYData Drawn, IReadOnlyList<Point> Path);

    /// <summary>Works the line's pixels out: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to
    /// a legend, so a legend keeps clear of the line that is drawn.</summary>
    private LinePixels Pixels(LineSeries series)
    {
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);

        // Apply DrawStyle step interpolation before transforming
        var drawn = data.SteppedBy(series.DrawStyle);

        // Apply monotone-cubic smoothing if requested (after step interpolation, before markers)
        double[] drawX = drawn.X, drawY = drawn.Y;
        if (series.Smooth && drawX.Length >= 3)
            (drawX, drawY) = MonotoneCubicSpline.Interpolate(drawX, drawY, series.SmoothResolution);

        return new LinePixels(data, new XYData(drawX, drawY), Transform.TransformPath(drawX, drawY));
    }

    /// <inheritdoc />
    internal override void AddMarks(LineSeries series, MarkObstacles.Builder marks) =>
        AddPolyline(marks, Pixels(series).Path);

    /// <inheritdoc />
    public override void Render(LineSeries series)
    {
        var color = ColorOf(series);
        var (data, drawn, path) = Pixels(series);
        DrawSeriesLine(path, series, color);

        if (Slot.LineMarkerOf(series.Marker) is { } style and not MarkerStyle.None)
        {
            var marker = MarkerShape.Of(style, Context.Theme);
            var markerStyle = Slot.LineMarkerStyleOf(style, color, series.MarkerFaceColor, series.MarkerEdgeColor, series.GivenMarkerEdgeWidth);

            // For step-interpolated data, draw markers only on original data points. A path that was cut is not one point per
            // sample: the markers are placed on their own then (a value a log axis cannot place stays masked).
            Point[] markerPoints = series.DrawStyle is not null and not DrawStyle.Default
                ? Transform.TransformBatch(data.X, data.Y)
                : path as Point[] ?? Transform.TransformBatch(drawn.X, drawn.Y);

            // MarkerSize is in points, drawn that many points across.
            double markerSize = series.MarkerSize.PointsInPixels;
            for (int i = 0; i < markerPoints.Length; i++)
            {
                if (series.MarkEvery is not null && i % series.MarkEvery.Value != 0) continue;
                // A marker at a point the axes cannot place is skipped, as matplotlib masks it.
                if (!markerPoints[i].IsDrawable()) continue;
                marker.Draw(Ctx, markerPoints[i], markerSize, markerStyle);
            }
        }
    }

}
