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
    internal override void AddMarks(AreaSeries series, MarkObstacles.Builder marks)
    {
        foreach (var shape in Shapes(series))
        {
            if (shape.Polygon is not null) marks.AddPath(shape.Polygon);
            else AddPolyline(marks, shape.Outline!);
        }
    }

    /// <summary>One thing an area draws: a filled polygon, or the line of its top edge.</summary>
    private readonly record struct AreaShape(List<Point>? Polygon, IReadOnlyList<Point>? Outline);

    /// <summary>Everything the area draws, in drawing order: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<AreaShape> Shapes(AreaSeries series)
    {
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = data.X.Length;
        if (n == 0) yield break;

        // A gap splits the area, as matplotlib's fill_between masks an invalid point: each unbroken run is filled and
        // outlined on its own. The top is data, so a top the axes cannot place (a missing value, NaN, an infinity, 0 or
        // below on a log axis) is a gap, as it is in a line. The lower edge is what the area stands on: a missing one is a
        // gap too, but one the axis cannot place (0 on a log axis) is clipped to the edge where the axis begins, as
        // matplotlib clips it. Without a gap the one run is the whole area, drawn from the very arrays given. A lower
        // edge of another length than the data does not pair with it point by point and is handed on whole, as before.
        double[]? bottom = series.YData2;
        double[]? pairedBottom = bottom is { } b && b.Length == n ? b : null;
        double[] px = Transform.TransformX(data.X);
        double[] py = Transform.TransformY(data.Y);
        double[]? pyBottom = pairedBottom is null ? null : Transform.TransformYClipped(pairedBottom);
        foreach (var run in px.DrawableIndexRuns(py, pyBottom))
        {
            foreach (var shape in RunShapes(series, data.X.Slice(run), data.Y.Slice(run), pairedBottom?.Slice(run) ?? bottom))
                yield return shape;
        }
    }

    /// <inheritdoc />
    public override void Render(AreaSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? ApplyAlpha(color, series.Alpha);
        foreach (var shape in Shapes(series))
        {
            if (shape.Polygon is not null)
                Ctx.DrawPolygon(shape.Polygon, new ShapeStyle(fillColor, null, 0) { Hatch = series.Hatch, HatchColor = series.HatchColor });
            else
                DrawPolyline(shape.Outline!, Outline(series, color));
        }
    }

    /// <summary>The shapes of one unbroken run of the area: its fill down to <paramref name="bottom"/> (or the zero baseline)
    /// and its top edge.</summary>
    private IEnumerable<AreaShape> RunShapes(AreaSeries series, double[] x, double[] y, double[]? bottom)
    {
        // Apply step interpolation to the top edge if requested
        var top = DrawStyleInterpolation.Apply(x, y, series.StepMode);

        // Apply monotone-cubic smoothing to the top edge if requested
        if (series.Smooth && top.X.Length >= 3)
        {
            var (sx, sy) = MonotoneCubicSpline.Interpolate(top.X, top.Y, series.SmoothResolution);
            top = new XYData(sx, sy);
        }

        // When a Where predicate is set, split into contiguous runs of included points
        if (series.Where is not null)
        {
            foreach (var shape in WhereShapes(series, top, bottom)) yield return shape;
            yield break;
        }

        // Batch-transform the top edge; reuse pixel X for the reverse bottom edge
        var topPts = Transform.TransformBatch(top.X, top.Y);
        var pxX = Transform.TransformX(top.X);
        var polygon = new List<Point>(topPts.Length * 2);
        polygon.AddRange(topPts);

        AddLowerEdge(polygon, series, x, bottom, pxX);

        yield return new AreaShape(polygon, null);
        yield return new AreaShape(null, topPts);
    }

    /// <summary>The stroke of the area's top edge: the <see cref="AreaSeries.EdgeColor"/> when set, otherwise the area's
    /// colour; the dash pattern and width the series names, otherwise its slot's.</summary>
    private StrokeStyle Outline(AreaSeries series, Color color) =>
        Slot.StrokeOf(series.EdgeColor ?? color, series.LineWidth, series.LineStyle);

    /// <summary>The shapes of the area with a Where predicate: a fill for each contiguous run the predicate keeps, then the
    /// full top edge.</summary>
    private IEnumerable<AreaShape> WhereShapes(AreaSeries series, XYData top, double[]? bottom)
    {
        var where = series.Where!;
        int len = top.X.Length;

        // Build a mask of which points pass the predicate
        var mask = new bool[len];
        for (int i = 0; i < len; i++)
            mask[i] = where(top.X[i], top.Y[i]);

        // Walk through contiguous runs of true values and fill each as a separate polygon
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
                yield return SegmentShape(series, top, runStart, i - 1, bottom);
                runStart = -1;
            }
        }

        // Always draw the full top-edge line (unmasked by the predicate) so the boundary is continuous
        yield return new AreaShape(null, Transform.TransformBatch(top.X, top.Y));
    }

    /// <summary>The fill of a single contiguous segment from index <paramref name="start"/> to <paramref name="end"/> (inclusive).</summary>
    private AreaShape SegmentShape(AreaSeries series, XYData top, int start, int end, double[]? bottom)
    {
        int count = end - start + 1;
        var segX = top.X[start..(end + 1)];
        var segY = top.Y[start..(end + 1)];

        var topPts = Transform.TransformBatch(segX, segY);
        var pxX = Transform.TransformX(segX);
        var polygon = new List<Point>(count * 2);
        polygon.AddRange(topPts);

        // Use corresponding segment of the lower edge (may need resampling if step-interpolated)
        AddLowerEdge(polygon, series, segX, bottom is not null && bottom.Length > end ? bottom[start..(end + 1)] : bottom, pxX);

        return new AreaShape(polygon, null);
    }

    /// <summary>Closes <paramref name="polygon"/> along the lower edge the area stands on, right to left:
    /// <paramref name="bottom"/> (stepped as the top is), or the zero baseline under the top's own pixel X
    /// (<paramref name="topPxX"/>) when there is none. A lower edge the axis cannot place is clipped to the edge where the
    /// axis begins (<see cref="DataTransform.TransformYClipped"/>).</summary>
    private void AddLowerEdge(List<Point> polygon, AreaSeries series, double[] x, double[]? bottom, double[] topPxX)
    {
        if (bottom is not null)
        {
            var bot = DrawStyleInterpolation.Apply(x, bottom, series.StepMode);
            var botPxX = Transform.TransformX(bot.X);
            var botPxY = Transform.TransformYClipped(bot.Y);
            for (int i = bot.X.Length - 1; i >= 0; i--) polygon.Add(new Point(botPxX[i], botPxY[i]));
        }
        else
        {
            double pyZero = Transform.TransformYClipped([0.0])[0];
            for (int i = topPxX.Length - 1; i >= 0; i--) polygon.Add(new Point(topPxX[i], pyZero));
        }
    }
}
