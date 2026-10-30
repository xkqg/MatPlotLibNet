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
    /// <param name="Polygon">The fill: the top edge left to right, then the lower edge right to left.</param>
    /// <param name="Outline">The line of the top edge, or <see langword="null"/> for a fill.</param>
    /// <param name="TopCount">How many of the polygon's first points are the top edge.</param>
    private readonly record struct AreaShape(List<Point>? Polygon, IReadOnlyList<Point>? Outline, int TopCount = 0);

    /// <summary>Everything the area draws, in drawing order: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<AreaShape> Shapes(AreaSeries series)
    {
        var plane = PlaneOf(series);
        var curves = series.Curves;
        var data = ApplyDownsampling(curves.Along, curves.Across, series.MaxDisplayPoints);
        int n = data.X.Length;
        if (n == 0) yield break;

        // A gap splits the area, as matplotlib's fill_between masks an invalid point: each unbroken run is filled and
        // outlined on its own. The top is data, so a top the axes cannot place (a missing value, NaN, an infinity, 0 or
        // below on a log axis) is a gap, as it is in a line. The lower edge is what the area stands on: a missing one is a
        // gap too, but one the axis cannot place (0 on a log axis) is clipped to the edge where the axis begins, as
        // matplotlib clips it. Without a gap the one run is the whole area, drawn from the very arrays given. A lower
        // edge of another length than the data does not pair with it point by point and is handed on whole, as before.
        double[]? bottom = curves.Second;
        double[]? pairedBottom = bottom is { } b && b.Length == n ? b : null;
        double[] px = plane.Along(data.X);
        double[] py = plane.Across(data.Y);
        double[]? pyBottom = pairedBottom is null ? null : plane.AcrossClipped(pairedBottom);
        foreach (var run in px.DrawableIndexRuns(py, pyBottom))
        {
            foreach (var shape in RunShapes(series, plane, data.X.Slice(run), data.Y.Slice(run), pairedBottom?.Slice(run) ?? bottom))
                yield return shape;
        }
    }

    /// <inheritdoc />
    public override void Render(AreaSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? ApplyAlpha(color, series.Alpha);
        // A gradient runs across each fill from its baseline to its tip; the hatch is laid over it. A fill colour of the series
        // is taken as given, so is the colour the gradient ends in then.
        Color? tipColor = series.GradientColor is { } tip ? (series.FillColor is null ? ScaleAlpha(tip, series.Alpha) : tip) : null;
        var plane = PlaneOf(series);
        foreach (var shape in Shapes(series))
        {
            if (shape.Polygon is not null)
            {
                var style = new ShapeStyle(fillColor, null, 0) { Hatch = series.Hatch, HatchColor = series.HatchColor };
                if (tipColor is { } end) style = style with { Gradient = plane.BaselineToTip(shape.Polygon, shape.TopCount, fillColor, end) };
                Ctx.DrawPolygon(shape.Polygon, style);
            }
            else
                DrawPolyline(shape.Outline!, Outline(series, color));
        }
    }

    /// <summary>The shapes of one unbroken run of the area: its fill down to <paramref name="bottom"/> (or the zero baseline)
    /// and its top edge.</summary>
    private IEnumerable<AreaShape> RunShapes(AreaSeries series, AreaPlane plane, double[] x, double[] y, double[]? bottom)
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
            foreach (var shape in WhereShapes(series, plane, top, bottom)) yield return shape;
            yield break;
        }

        // Batch-transform the top edge; reuse pixel X for the reverse bottom edge
        var topPts = plane.Points(top.X, top.Y);
        var pxX = plane.Along(top.X);
        var polygon = new List<Point>(topPts.Length * 2);
        polygon.AddRange(topPts);

        AddLowerEdge(polygon, series, plane, x, bottom, pxX);

        yield return new AreaShape(polygon, null, topPts.Length);
        yield return new AreaShape(null, topPts);
    }

    private AreaPlane PlaneOf(AreaSeries series) => new(Transform, series.Orientation == BarOrientation.Vertical);

    /// <summary>The stroke of the area's top edge: the <see cref="AreaSeries.EdgeColor"/> when set, otherwise the area's
    /// colour; the dash pattern and width the series names, otherwise its slot's.</summary>
    private StrokeStyle Outline(AreaSeries series, Color color) =>
        Slot.StrokeOf(series.EdgeColor ?? color, series.LineWidth, series.LineStyle);

    /// <summary>The shapes of the area with a Where predicate: a fill for each contiguous run the predicate keeps, then the
    /// full top edge.</summary>
    private IEnumerable<AreaShape> WhereShapes(AreaSeries series, AreaPlane plane, XYData top, double[]? bottom)
    {
        var where = series.Where!;
        int len = top.X.Length;

        // Build a mask of which points pass the predicate
        var mask = new bool[len];
        for (int i = 0; i < len; i++)
            mask[i] = plane.Holds(where, top.X[i], top.Y[i]);

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
                yield return SegmentShape(series, plane, top, runStart, i - 1, bottom);
                runStart = -1;
            }
        }

        // Always draw the full top-edge line (unmasked by the predicate) so the boundary is continuous
        yield return new AreaShape(null, plane.Points(top.X, top.Y));
    }

    /// <summary>The fill of a single contiguous segment from index <paramref name="start"/> to <paramref name="end"/> (inclusive).</summary>
    private AreaShape SegmentShape(AreaSeries series, AreaPlane plane, XYData top, int start, int end, double[]? bottom)
    {
        int count = end - start + 1;
        var segX = top.X[start..(end + 1)];
        var segY = top.Y[start..(end + 1)];

        var topPts = plane.Points(segX, segY);
        var pxX = plane.Along(segX);
        var polygon = new List<Point>(count * 2);
        polygon.AddRange(topPts);

        // Use corresponding segment of the lower edge (may need resampling if step-interpolated)
        AddLowerEdge(polygon, series, plane, segX, bottom is not null && bottom.Length > end ? bottom[start..(end + 1)] : bottom, pxX);

        return new AreaShape(polygon, null, topPts.Length);
    }

    /// <summary>Closes <paramref name="polygon"/> along the lower edge the area stands on, right to left:
    /// <paramref name="bottom"/> (stepped as the top is), or the zero baseline under the top's own pixel X
    /// (<paramref name="topPxX"/>) when there is none. A lower edge the axis cannot place is clipped to the edge where the
    /// axis begins (<see cref="DataTransform.TransformYClipped"/>).</summary>
    private void AddLowerEdge(List<Point> polygon, AreaSeries series, AreaPlane plane, double[] x, double[]? bottom, double[] topPxX)
    {
        if (bottom is not null)
        {
            var bot = DrawStyleInterpolation.Apply(x, bottom, series.StepMode);
            var botPxX = plane.Along(bot.X);
            var botPxY = plane.AcrossClipped(bot.Y);
            for (int i = bot.X.Length - 1; i >= 0; i--) polygon.Add(plane.At(botPxX[i], botPxY[i]));
        }
        else
        {
            double pyZero = plane.AcrossClipped([0.0])[0];
            for (int i = topPxX.Length - 1; i >= 0; i--) polygon.Add(plane.At(topPxX[i], pyZero));
        }
    }

    /// <summary>The one place where an area's two axes are told apart. The drawing is written in terms of the axis the fill
    /// runs <i>along</i> and the axis its curves are read <i>across</i>: for an upright fill that is X and Y, for one that lies
    /// down (<c>fill_betweenx</c>) Y and X. This turns a value on either into the pixel it stands at and a pixel pair back
    /// into a point on the page, so the polygon of a fill that lies down is the upright one on its side.</summary>
    private readonly record struct AreaPlane(DataTransform Transform, bool Upright)
    {
        /// <summary>The pixels along the axis the fill runs along, for values on it.</summary>
        internal double[] Along(double[] values) => Upright ? Transform.TransformX(values) : Transform.TransformY(values);

        /// <summary>The pixels across, for values on the axis the curves are read on.</summary>
        internal double[] Across(double[] values) => Upright ? Transform.TransformY(values) : Transform.TransformX(values);

        /// <summary>The pixels across for a base the fill stands on: one the axis cannot place is clipped to the edge where
        /// the axis begins.</summary>
        internal double[] AcrossClipped(double[] values) => Upright ? Transform.TransformYClipped(values) : Transform.TransformXClipped(values);

        /// <summary>The point on the page for a pixel along and a pixel across.</summary>
        internal Point At(double along, double across) => Upright ? new Point(along, across) : new Point(across, along);

        private double AlongOf(Point point) => Upright ? point.X : point.Y;

        private double AcrossOf(Point point) => Upright ? point.Y : point.X;

        /// <summary>The blend that runs across the fill, through the middle of it along its axis, from the baseline it stands on
        /// to the point of its top edge furthest from that baseline: <paramref name="baseColour"/> at the baseline,
        /// <paramref name="tipColour"/> at the tip. The baseline is the mean height of the lower edge (the zero line is the same
        /// pixel everywhere), so the blend is the same whichever way the axis runs.</summary>
        /// <param name="polygon">The fill: <paramref name="topCount"/> points of the top edge, then those of the lower edge.</param>
        /// <param name="topCount">How many of the first points are the top edge.</param>
        /// <param name="baseColour">The colour at the baseline.</param>
        /// <param name="tipColour">The colour at the tip.</param>
        /// <returns>The gradient, in the pixels of the figure.</returns>
        internal LinearGradient BaselineToTip(IReadOnlyList<Point> polygon, int topCount, Color baseColour, Color tipColour)
        {
            var top = polygon.Take(topCount).Select(AcrossOf).ToArray();
            var lower = polygon.Skip(topCount).Select(AcrossOf).ToArray();
            double baseline = lower.Average();
            double tip = top.MaxBy(across => Math.Abs(across - baseline));
            double along = (polygon.Min(AlongOf) + polygon.Max(AlongOf)) / 2;
            return new LinearGradient(At(along, baseline), At(along, tip), baseColour, tipColour);
        }

        /// <summary>The points on the page for values along and across.</summary>
        internal Point[] Points(double[] along, double[] across)
        {
            if (Upright) return Transform.TransformBatch(along, across);
            var alongPx = Along(along);
            var acrossPx = Across(across);
            var points = new Point[alongPx.Length];
            for (int i = 0; i < points.Length; i++) points[i] = At(alongPx[i], acrossPx[i]);
            return points;
        }

        /// <summary>Asks a <see cref="AreaSeries.Where"/> predicate about a point, which it takes as (x, y) on the page axes.</summary>
        internal bool Holds(Func<double, double, bool> where, double along, double across) =>
            Upright ? where(along, across) : where(across, along);
    }
}
