// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="AreaSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class AreaSeriesRenderer : SeriesRenderer<AreaSeries>
{
    /// <inheritdoc />
    public AreaSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(AreaSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? ApplyAlpha(color, series.Alpha);
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = data.X.Length;
        if (n == 0) return;

        // Apply step interpolation to the top edge if requested
        var top = DrawStyleInterpolation.Apply(data.X, data.Y, series.StepMode);

        // Apply monotone-cubic smoothing to the top edge if requested
        if (series.Smooth && top.X.Length >= 3)
        {
            var (sx, sy) = MonotoneCubicSpline.Interpolate(top.X, top.Y, series.SmoothResolution);
            top = new XYData(sx, sy);
        }

        // When a Where predicate is set, split into contiguous runs of included points
        if (series.Where is not null)
        {
            RenderWithWhere(series, top, fillColor, color);
            return;
        }

        // Batch-transform the top edge; reuse pixel X for the reverse bottom edge
        var topPts = Transform.TransformBatch(top.X, top.Y);
        var pxX = Transform.TransformX(top.X);
        var polygon = new List<Point>(topPts.Length * 2);
        polygon.AddRange(topPts);

        if (series.YData2 is not null)
        {
            var bot = DrawStyleInterpolation.Apply(data.X, series.YData2, series.StepMode);
            var botPxX = Transform.TransformX(bot.X);
            var botPxY = Transform.TransformY(bot.Y);
            for (int i = bot.X.Length - 1; i >= 0; i--) polygon.Add(new Point(botPxX[i], botPxY[i]));
        }
        else
        {
            double pyZero = Transform.TransformY([0.0])[0];
            for (int i = pxX.Length - 1; i >= 0; i--) polygon.Add(new Point(pxX[i], pyZero));
        }

        Ctx.DrawPolygon(polygon, fillColor, null, 0);

        // Draw boundary with EdgeColor override if set
        var edgeColor = series.EdgeColor ?? color;
        Ctx.DrawLines(new List<Point>(topPts), edgeColor, series.LineWidth, series.LineStyle);
    }

    /// <summary>Renders the area with a Where predicate, splitting into contiguous fill segments.</summary>
    private void RenderWithWhere(AreaSeries series, XYData top, Color fillColor, Color color)
    {
        var where = series.Where!;
        int len = top.X.Length;

        // Build a mask of which points pass the predicate
        var mask = new bool[len];
        for (int i = 0; i < len; i++)
            mask[i] = where(top.X[i], top.Y[i]);

        // Walk through contiguous runs of true values and render each as a separate polygon
        int runStart = -1;
        for (int i = 0; i <= len; i++)
        {
            bool included = i < len && mask[i];
            if (included && runStart < 0)
            {
                runStart = i;
            }
            else if (!included && runStart >= 0)
            {
                RenderSegment(series, top, runStart, i - 1, fillColor, color);
                runStart = -1;
            }
        }

        // Always draw the full top-edge line (unmasked) so the boundary is continuous
        var allTopPts = Transform.TransformBatch(top.X, top.Y);
        var edgeColor = series.EdgeColor ?? color;
        Ctx.DrawLines(new List<Point>(allTopPts), edgeColor, series.LineWidth, series.LineStyle);
    }

    /// <summary>Renders a single contiguous fill segment from index <paramref name="start"/> to <paramref name="end"/> (inclusive).</summary>
    private void RenderSegment(AreaSeries series, XYData top, int start, int end, Color fillColor, Color color)
    {
        int count = end - start + 1;
        var segX = top.X[start..(end + 1)];
        var segY = top.Y[start..(end + 1)];

        var topPts = Transform.TransformBatch(segX, segY);
        var pxX = Transform.TransformX(segX);
        var polygon = new List<Point>(count * 2);
        polygon.AddRange(topPts);

        if (series.YData2 is not null)
        {
            // Use corresponding segment of YData2 (may need resampling if step-interpolated)
            var segY2 = series.YData2.Length > end ? series.YData2[start..(end + 1)] : series.YData2;
            var bot = DrawStyleInterpolation.Apply(segX, segY2, series.StepMode);
            var botPxX = Transform.TransformX(bot.X);
            var botPxY = Transform.TransformY(bot.Y);
            for (int i = bot.X.Length - 1; i >= 0; i--) polygon.Add(new Point(botPxX[i], botPxY[i]));
        }
        else
        {
            double pyZero = Transform.TransformY([0.0])[0];
            for (int i = pxX.Length - 1; i >= 0; i--) polygon.Add(new Point(pxX[i], pyZero));
        }

        Ctx.DrawPolygon(polygon, fillColor, null, 0);
    }

}
