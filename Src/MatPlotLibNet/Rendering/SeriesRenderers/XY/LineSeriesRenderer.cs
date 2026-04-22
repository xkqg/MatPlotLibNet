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

    /// <inheritdoc />
    public override void Render(LineSeries series)
    {
        var color = ResolveColor(series.Color);
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);

        // Apply DrawStyle step interpolation before transforming
        var drawn = DrawStyleInterpolation.Apply(data.X, data.Y, series.DrawStyle);

        // Apply monotone-cubic smoothing if requested (after step interpolation, before markers)
        double[] drawX = drawn.X, drawY = drawn.Y;
        if (series.Smooth && drawX.Length >= 3)
            (drawX, drawY) = MonotoneCubicSpline.Interpolate(drawX, drawY, series.SmoothResolution);

        Point[] points = Transform.TransformBatch(drawX, drawY);
        Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);

        if (series.Marker is not null && series.Marker != MarkerStyle.None)
        {
            var markerFill = series.MarkerFaceColor ?? color;
            var markerStroke = series.MarkerEdgeColor;
            var markerStrokeWidth = series.MarkerEdgeColor is not null ? series.MarkerEdgeWidth : 0;

            // For step-interpolated data, draw markers only on original data points
            Point[] markerPoints = series.DrawStyle is not null and not DrawStyle.Default
                ? Transform.TransformBatch(data.X, data.Y)
                : points;

            for (int i = 0; i < markerPoints.Length; i++)
            {
                if (series.MarkEvery is not null && i % series.MarkEvery.Value != 0) continue;
                MarkerRenderer.Draw(Ctx, series.Marker ?? MarkerStyle.Circle,
                    markerPoints[i], series.MarkerSize, markerFill, markerStroke, markerStrokeWidth);
            }
        }
    }

}
