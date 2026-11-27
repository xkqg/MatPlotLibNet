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
            if (shape.Polygon is not null) marks.AddPath(Fill(shape.Polygon));
            else AddPolyline(marks, shape.Outline!);
        }
    }

    /// <summary>One thing an area draws: a filled polygon, or the line of its top edge.</summary>
    /// <param name="Polygon">The fill: the top edge left to right, then the lower edge right to left, a value a log axis cannot
    /// place a thousand decades beyond it (<see cref="Fill"/> cuts it there).</param>
    /// <param name="Outline">The line of the top edge, cut where it leaves the plot that way, or <see langword="null"/> for a
    /// fill.</param>
    /// <param name="TopCount">How many of the polygon's first points are the top edge.</param>
    private readonly record struct AreaShape(List<Point>? Polygon, IReadOnlyList<Point>? Outline, int TopCount = 0);

    /// <summary>The fill drawn of <paramref name="polygon"/>: cut where it leaves the plot towards a value a log axis cannot
    /// place, running along the edge where the axis begins between the two cuts, as matplotlib's clip leaves its fill_between.</summary>
    private IReadOnlyList<Point> Fill(List<Point> polygon) => Transform.CutBeyondTheStart(polygon, closed: true);

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
        // outlined on its own. A gap is a missing value (NaN) or an infinity in the data or in the lower edge. A value a log
        // axis cannot place (0 or below) is no gap: matplotlib's log scale puts it a thousand decades below the axis, so the
        // edge through it dives to where the axis begins and the fill is cut there (Fill). Without a gap the one run is the
        // whole area, drawn from the very arrays given. A lower edge of another length than the data does not pair with it
        // point by point and is handed on whole, as before.
        double[]? bottom = curves.Second;
        double[]? pairedBottom = bottom is { } b && b.Length == n ? b : null;
        double[] px = plane.Along(data.X);
        double[] py = plane.Across(data.Y);
        double[]? pyBottom = pairedBottom is null ? null : plane.Across(pairedBottom);
        foreach (var run in px.DrawableIndexRuns(py, pyBottom))
        {
            foreach (var shape in RunShapes(series, plane, data.X.Slice(run), data.Y.Slice(run), pairedBottom?.Slice(run) ?? bottom))
                yield return shape;
        }
    }

    /// <inheritdoc />
    public override void Render(AreaSeries series)
    {
        var color = ColorOf(series);
        // Under the matplotlib themes a fill that names no alpha, line or edge is drawn as matplotlib's fill_between draws its
        // polygon: opaque, no line, edged as every patch is (AreaSeries.IsFillBetweenUnder, Theme.PatchEdgeOf: black 1 pt under
        // classic, none under v2).
        var theme = Context.Theme;
        double alpha = series.OpacityUnder(theme);
        bool drawsLine = !series.IsFillBetweenUnder(theme);
        var edges = theme.PatchEdgesOf(series);
        var edge = drawsLine ? PatchEdge.None : edges.Of(color);
        var fillColor = series.FillColor ?? color.WithOpacityGiven(alpha);
        // A gradient runs across each fill from its baseline to its tip; the hatch is laid over it. A fill colour of the series
        // is taken as given, so is the colour the gradient ends in then.
        Color? tipColor = series.GradientColor is { } tip ? (series.FillColor is null ? tip.ScaleAlpha(alpha) : tip) : null;
        var plane = PlaneOf(series);
        var style = edges.StyleOf(fillColor, edge, series.Hatching);
        foreach (var shape in Shapes(series))
        {
            if (shape.Polygon is not null)
            {
                var shapeStyle = style;
                if (tipColor is { } end)
                {
                    // The blend runs to the edge where a log axis begins, not to a corner a thousand decades below it.
                    var outline = shape.Polygon.Exists(Transform.LiesBeyond) ? shape.Polygon.ConvertAll(Transform.AtTheStart) : shape.Polygon;
                    shapeStyle = style with { Gradient = plane.BaselineToTip(outline, shape.TopCount, fillColor, end) };
                }
                Ctx.DrawPolygon(Fill(shape.Polygon), shapeStyle);
            }
            else if (drawsLine)
                DrawPolyline(shape.Outline!, Outline(series, color));
        }
    }

    /// <summary>The shapes of one unbroken run of the area: its fill down to <paramref name="bottom"/> (or the zero baseline)
    /// and its top edge. With a <see cref="AreaSeries.Where"/> predicate the fill is one polygon for each region of the run's data
    /// the predicate keeps (<see cref="RegionsKept"/>), cut from the run's top, and the top edge is still the whole run's.</summary>
    private IEnumerable<AreaShape> RunShapes(AreaSeries series, AreaPlane plane, double[] x, double[] y, double[]? bottom)
    {
        var top = TopOf(series, x, y);
        var topPts = plane.Points(top.X, top.Y);
        var topPxX = plane.Along(top.X);
        if (series.Where is not { } where)
        {
            if (FillUnder(series, plane, topPts, topPxX, x, bottom) is { } fill) yield return fill;
        }
        else
        {
            // A lower edge of another length than the data does not pair with it point by point and is handed on whole.
            bool paired = bottom is not null && bottom.Length == x.Length;
            foreach (var region in RegionsKept(plane, where, x, y))
            {
                // The region's top is the run's top from the region's first data point to its last, so the fill stands under the
                // line drawn through the whole run; its lower edge is the region's own, stepped as the top is.
                var drawn = DrawnIndexOf(series, region.Start.Value, x.Length)..(DrawnIndexOf(series, region.End.Value - 1, x.Length) + 1);
                if (FillUnder(series, plane, topPts[drawn], topPxX[drawn], x[region], paired ? bottom![region] : bottom) is { } fill)
                    yield return fill;
            }
        }

        // The whole top edge, a predicate not asked, so the line runs on through what is not filled.
        yield return new AreaShape(null, Transform.CutBeyondTheStart(topPts, closed: false));
    }

    /// <summary>The top edge drawn through the points <paramref name="x"/>, <paramref name="y"/>: stepped as the series steps, then
    /// smoothed by a monotone cubic when it asks for that and has three points or more.</summary>
    private static XYData TopOf(AreaSeries series, double[] x, double[] y)
    {
        var top = new XYData(x, y).SteppedBy(series.StepMode);
        if (!series.Smooth || top.X.Length < 3) return top;
        var (sx, sy) = MonotoneCubicSpline.Interpolate(top.X, top.Y, series.SmoothResolution);
        return new XYData(sx, sy);
    }

    /// <summary>Where data point <paramref name="point"/> of <paramref name="count"/> stands in the top edge <see cref="TopOf"/> draws
    /// through them: its place among the stepped points (<see cref="DrawStyleInterpolation"/>), then among the samples of the curve
    /// through those when the top is smoothed (<see cref="MonotoneCubicSpline.SampleOf"/>).</summary>
    private static int DrawnIndexOf(AreaSeries series, int point, int count)
    {
        int stepped = series.StepMode.IndexOfPoint(point, count);
        int steppedCount = series.StepMode.IndexOfPoint(count - 1, count) + 1;
        return series.Smooth && steppedCount >= 3 ? MonotoneCubicSpline.SampleOf(stepped, steppedCount, series.SmoothResolution) : stepped;
    }

    /// <summary>The fill under the top edge <paramref name="top"/> (its pixels along, <paramref name="topPxX"/>) down to
    /// <paramref name="bottom"/> (or the zero baseline), the points <paramref name="x"/> of the data under it: the top edge left to
    /// right, then the lower edge stepped as the top is, right to left. <see langword="null"/> when a corner of it cannot be placed
    /// (its zero baseline on a logit axis, which masks 0).</summary>
    private AreaShape? FillUnder(AreaSeries series, AreaPlane plane, Point[] top, double[] topPxX, double[] x, double[]? bottom)
    {
        var polygon = new List<Point>(top.Length * 2);
        polygon.AddRange(top);
        AddLowerEdge(polygon, series, plane, x, bottom, topPxX);
        return polygon.IsDrawable() ? new AreaShape(polygon, null, top.Length) : null;
    }

    /// <summary>The regions of the data <paramref name="where"/> keeps, as matplotlib's <c>fill_between</c> finds them
    /// (<c>cbook.contiguous_regions(where)</c>, lib/matplotlib/collections.py:1538-1541): each unbroken range of points the
    /// predicate holds for, asked of the data points themselves, never of a stepped corner or a smoothed sample. matplotlib then
    /// slices the region's x and both curves and steps them together (<c>_make_verts_for_region</c>, collections.py:1573-1584): the
    /// same points the run's stepped top holds from the region's first data point to its last (<see cref="DrawnIndexOf"/>).</summary>
    private static IEnumerable<Range> RegionsKept(AreaPlane plane, Func<double, double, bool> where, double[] x, double[] y)
    {
        int start = -1;
        for (int i = 0; i <= x.Length; i++)
        {
            bool kept = i < x.Length && plane.Holds(where, x[i], y[i]);
            if (kept && start < 0) start = i;
            else if (!kept && start >= 0)
            {
                yield return start..i;
                start = -1;
            }
        }
    }

    private AreaPlane PlaneOf(AreaSeries series) => new(Transform, !series.Orientation.ValuesAlongX);

    /// <summary>The stroke of the area's top edge: the <see cref="AreaSeries.EdgeColor"/> when set, otherwise the area's
    /// colour; the dash pattern and width the series names, otherwise its slot's.</summary>
    private StrokeStyle Outline(AreaSeries series, Color color) =>
        Slot.StrokeOf(series.EdgeColor ?? color, series);

    /// <summary>Closes <paramref name="polygon"/> along the lower edge the area stands on, right to left:
    /// <paramref name="bottom"/> (stepped as the top is), a curve whose values a log axis cannot place lie a thousand decades
    /// below it as the top's do; or the zero baseline under the top's own pixel X (<paramref name="topPxX"/>) when there is none,
    /// a straight line along the axis that a log axis clips to the edge where it begins
    /// (<see cref="DataTransform.TransformYClipped"/>): where that edge would cut it.</summary>
    private void AddLowerEdge(List<Point> polygon, AreaSeries series, AreaPlane plane, double[] x, double[]? bottom, double[] topPxX)
    {
        if (bottom is not null)
        {
            var bot = new XYData(x, bottom).SteppedBy(series.StepMode);
            var botPxX = plane.Along(bot.X);
            var botPxY = plane.Across(bot.Y);
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
        /// <summary>The pixels along the axis the fill runs along, for values on it; one a log axis cannot place a thousand
        /// decades beyond it.</summary>
        internal double[] Along(double[] values) => Transform.TransformUnclippedAlong(Upright, values);

        /// <summary>The pixels across, for values on the axis the curves are read on; one a log axis cannot place a thousand
        /// decades beyond it.</summary>
        internal double[] Across(double[] values) => Transform.TransformUnclippedAlong(!Upright, values);

        /// <summary>The pixels across for a base the fill stands on: one the axis cannot place is clipped to the edge where
        /// the axis begins.</summary>
        internal double[] AcrossClipped(double[] values) => Transform.TransformClippedAlong(!Upright, values);

        /// <summary>The point on the page for a pixel along and a pixel across.</summary>
        internal Point At(double along, double across) => Point.Along(Upright, along, across);

        private double AlongOf(Point point) => point.CoordinateAlong(Upright);

        private double AcrossOf(Point point) => point.CoordinateAcross(Upright);

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
            if (Upright) return Transform.TransformBatchUnclipped(along, across);
            var alongPx = Along(along);
            var acrossPx = Across(across);
            var points = new Point[alongPx.Length];
            for (int i = 0; i < points.Length; i++) points[i] = At(alongPx[i], acrossPx[i]);
            return points;
        }

        /// <summary>Asks a <see cref="AreaSeries.Where"/> predicate about a point, which it takes as (x, y) on the page axes.</summary>
        internal bool Holds(Func<double, double, bool> where, double along, double across)
        {
            var page = At(along, across);
            return where(page.X, page.Y);
        }
    }
}
