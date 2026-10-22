// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Downsampling;

namespace MatPlotLibNet;

/// <summary>NaN is the library's one missing value: a value that is not there. Only a finite value can be drawn: a
/// missing value cannot, and neither can infinity, which lies beyond every axis. This class is the one place that says
/// both; a colormap, a renderer and a series' data range ask it, and none of them tests <c>double.IsNaN</c> on its own.
/// <para>A renderer asks it about the pixels it is about to draw: <see cref="DataTransform"/> (and
/// <see cref="PolarTransform"/> for a radius or an angle) maps every coordinate it cannot place to a non-finite pixel, so
/// a mark (a marker, a bar, a stem, an error bar, a reference line, a span, a signal, an annotation, a polar mark) at such
/// a coordinate is skipped before any render context sees it, as matplotlib masks an invalid point. No render context
/// (SVG, Skia, a native control) is handed a coordinate it cannot draw. A whole made of parts (a pie, a donut) counts a
/// part that cannot be drawn as 0 in its total.</para>
/// <para>Two kinds of coordinate are clipped instead of masked. The first is the base a mark stands on (a bar's foot, an
/// area's lower edge, the lower end of a box). A base the axis' scale cannot place (0 on a log axis) is drawn at the edge
/// where the axis begins, as matplotlib's log scale clips it (<see cref="DataTransform.DataToPixelClippedY"/>); a missing
/// base is still missing. The second is an edge of a span: an edge beyond the range the axis draws (an infinity,
/// ±<see cref="double.MaxValue"/> as <see cref="Axes.AddThreshold"/> stores its zone, a value a log axis cannot place) runs
/// to the plot edge, as matplotlib's axes clip a span, and a span wholly beyond the range draws nothing
/// (<c>CartesianSpansPart</c>); a missing edge is still missing.</para></summary>
internal static class MissingValueExtensions
{
    /// <summary>Whether <paramref name="value"/> is missing: NaN. Infinity is not missing; it is a value beyond every
    /// scale.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is NaN.</returns>
    internal static bool IsMissing(this double value) => double.IsNaN(value);

    /// <summary>Whether a chart can draw <paramref name="value"/>: a finite number.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="false"/> for NaN and for either infinity.</returns>
    internal static bool IsDrawable(this double value) => double.IsFinite(value);

    /// <summary>The smallest and the largest value of <paramref name="values"/> an axis of <paramref name="scale"/> can
    /// place: missing values and infinities are left out, as matplotlib leaves them out of its data limits, and on a log
    /// axis so are 0 and the negative values (masked, matplotlib's <c>nonpositive='mask'</c>).</summary>
    /// <param name="values">The values to range over.</param>
    /// <param name="scale">The scale of the axis the values are placed on.</param>
    /// <returns>The range, or <see langword="null"/> when no value can be placed: a series contributes nothing there.</returns>
    internal static Range1D? DrawableRange(this double[] values, AxisScale scale = AxisScale.Linear)
    {
        var range = Range1D.Empty;
        foreach (double value in values)
        {
            range = range.Including(value, scale);
        }

        return range.OrNone();
    }

    /// <summary>The smallest and the largest value over several arrays (the datasets of a box plot, the two edges of an
    /// area, the layers of a stack) an axis of <paramref name="scale"/> can place: missing values and infinities are left
    /// out, and on a log axis so are 0 and the negative values, the rule of <see cref="DrawableRange(double[], AxisScale)"/>.</summary>
    /// <param name="arrays">The arrays to range over together.</param>
    /// <param name="scale">The scale of the axis the values are placed on.</param>
    /// <returns>The range, or <see langword="null"/> when no array holds a value the axis can place.</returns>
    internal static Range1D? DrawableRange(this IEnumerable<double[]> arrays, AxisScale scale = AxisScale.Linear)
    {
        var range = Range1D.Empty;
        foreach (double[] values in arrays)
        {
            foreach (double value in values)
            {
                range = range.Including(value, scale);
            }
        }

        return range.OrNone();
    }

    /// <summary>The extent of the spans a chart can draw (a task bar, a broken-bar segment, a state segment, an arrow):
    /// a span with an end that is missing or infinite is not drawn, so neither of its ends counts.</summary>
    /// <param name="spans">The spans, each from one end to the other (in either order).</param>
    /// <returns>The range, or <see langword="null"/> when no span can be drawn.</returns>
    internal static Range1D? DrawableRange(this IEnumerable<Range1D> spans)
    {
        var range = Range1D.Empty;
        foreach (var span in spans)
        {
            if (span.Lo.IsDrawable() && span.Hi.IsDrawable())
            {
                range = range.Including(span.Lo).Including(span.Hi);
            }
        }

        return range.OrNone();
    }

    /// <summary>Whether the grid cell from [<paramref name="row"/>, <paramref name="col"/>] to [row + 1, col + 1] can be
    /// drawn: its four corner values and the grid lines on its four sides all drawable. matplotlib masks a contour cell
    /// with a missing corner; a cell whose corner is missing would otherwise be traced as if the corner lay below every
    /// level.</summary>
    /// <param name="z">The grid values, indexed [row, col].</param>
    /// <param name="x">The column coordinates.</param>
    /// <param name="y">The row coordinates.</param>
    /// <param name="row">The cell's lower row.</param>
    /// <param name="col">The cell's left column.</param>
    /// <returns><see langword="true"/> when every corner of the cell can be drawn.</returns>
    internal static bool IsDrawableCell(this double[,] z, double[] x, double[] y, int row, int col) =>
        x[col].IsDrawable() && x[col + 1].IsDrawable() && y[row].IsDrawable() && y[row + 1].IsDrawable()
        && z[row, col].IsDrawable() && z[row, col + 1].IsDrawable()
        && z[row + 1, col].IsDrawable() && z[row + 1, col + 1].IsDrawable();

    /// <summary>The smallest and the largest drawable value of a grid: missing values and infinities are left out.</summary>
    /// <param name="values">The cells to range over.</param>
    /// <returns>The range, or <see langword="null"/> when no cell holds a drawable value.</returns>
    internal static Range1D? DrawableRange(this double[,] values)
    {
        var range = Range1D.Empty;
        foreach (double value in values)
        {
            range = range.Including(value, AxisScale.Linear);
        }

        return range.OrNone();
    }

    /// <summary>Whether a chart can draw <paramref name="point"/>: both coordinates finite. A point the transform could
    /// not place (a non-positive value on a log axis, a value inside an axis break) comes out NaN and is not drawable
    /// either.</summary>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> when both coordinates are finite.</returns>
    internal static bool IsDrawable(this Point point) => point.X.IsDrawable() && point.Y.IsDrawable();

    /// <summary>The unbroken runs of <paramref name="points"/>, in order: a line breaks at every point that cannot be
    /// drawn, as matplotlib breaks a line at a masked or NaN point. Points without a gap are one run, the very list given,
    /// so a line without a gap is drawn from the same points as before.</summary>
    /// <param name="points">The points of a line, in drawing order.</param>
    /// <returns>Each run of consecutive drawable points; none when no point can be drawn.</returns>
    internal static IEnumerable<IReadOnlyList<Point>> DrawableRuns(this IReadOnlyList<Point> points)
    {
        foreach (var run in Runs(points.Count, i => points[i].IsDrawable()))
        {
            if (run.Count == points.Count)
            {
                yield return points;
                continue;
            }

            var part = new Point[run.Count];
            for (int i = 0; i < part.Length; i++)
            {
                part[i] = points[run.StartInclusive + i];
            }

            yield return part;
        }
    }

    /// <summary>The unbroken runs of a series' data, in order: the indices at which <paramref name="x"/>,
    /// <paramref name="y"/> and, when given, <paramref name="y2"/> are all drawable.</summary>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values, parallel to <paramref name="x"/>.</param>
    /// <param name="y2">A second set of y values parallel to <paramref name="x"/> (an area's lower edge), or
    /// <see langword="null"/>.</param>
    /// <returns>Each run of consecutive drawable indices.</returns>
    internal static IEnumerable<IndexRange> DrawableIndexRuns(this double[] x, double[] y, double[]? y2 = null) =>
        Runs(x.Length, i => x[i].IsDrawable() && y[i].IsDrawable() && (y2 is null || y2[i].IsDrawable()));

    /// <summary>The values of <paramref name="values"/> in <paramref name="run"/>: the very array when the run covers
    /// all of it.</summary>
    /// <param name="values">The array to take the run from.</param>
    /// <param name="run">The indices to take.</param>
    /// <returns>The values in the run.</returns>
    internal static double[] Slice(this double[] values, IndexRange run) =>
        run.StartInclusive == 0 && run.EndExclusive == values.Length ? values : values[run.StartInclusive..run.EndExclusive];

    /// <summary>The maximal runs of consecutive indices in [0, <paramref name="count"/>) at which
    /// <paramref name="drawable"/> holds.</summary>
    private static IEnumerable<IndexRange> Runs(int count, Func<int, bool> drawable)
    {
        int start = -1;
        for (int i = 0; i <= count; i++)
        {
            bool inRun = i < count && drawable(i);
            if (inRun && start < 0)
            {
                start = i;
            }
            else if (!inRun && start >= 0)
            {
                yield return new IndexRange(start, i);
                start = -1;
            }
        }
    }

    /// <summary>Whether a chart can draw <paramref name="rect"/>: its corner and its size all finite. A bar, a span or a
    /// waterfall step whose value, foot or edge cannot be placed comes out with a NaN edge and is not drawable.</summary>
    /// <param name="rect">The rectangle, in pixels.</param>
    /// <returns><see langword="true"/> when all four numbers are finite.</returns>
    internal static bool IsDrawable(this Rect rect) =>
        rect.X.IsDrawable() && rect.Y.IsDrawable() && rect.Width.IsDrawable() && rect.Height.IsDrawable();

    /// <summary>Whether a chart can draw the face whose corners are <paramref name="corners"/> (a 3-D surface quad, a
    /// triangle, a bar's side): every corner drawable. A face that needs a vertex which cannot be placed is skipped
    /// whole.</summary>
    /// <param name="corners">The corners, in pixels.</param>
    /// <returns><see langword="true"/> when every corner is drawable.</returns>
    internal static bool IsDrawable(this IReadOnlyList<Point> corners)
    {
        foreach (var corner in corners)
        {
            if (!corner.IsDrawable()) return false;
        }

        return true;
    }

    /// <summary>Whether a chart can draw the straight line from <paramref name="from"/> to <paramref name="to"/>: both its
    /// ends drawable. A stem, an error bar or a connector with an end that cannot be placed is not drawn.</summary>
    /// <param name="from">One end, in pixels.</param>
    /// <param name="to">The other end, in pixels.</param>
    /// <returns><see langword="true"/> when both ends are drawable.</returns>
    internal static bool CanDrawLineTo(this Point from, Point to) => from.IsDrawable() && to.IsDrawable();

    /// <summary>The observations of a sample a chart can draw from: its drawable values, in order. A missing observation
    /// (NaN) is not there and an infinity cannot be placed, so a box or a violin is drawn from the others, as its range is
    /// taken over them. A sample without a gap is handed back as the very array.</summary>
    /// <param name="values">The sample.</param>
    /// <returns>The drawable observations.</returns>
    internal static double[] DrawableValues(this double[] values)
    {
        double[] drawable = Array.FindAll(values, IsDrawable);
        return drawable.Length == values.Length ? values : drawable;
    }

    /// <summary>The observations of a sample a chart can draw from, ascending: what a box and a violin are drawn from, and
    /// what their reach is measured over, so the picture and the range cannot read different samples.</summary>
    /// <param name="values">The sample.</param>
    /// <returns>The drawable observations, smallest first; empty when none can be drawn.</returns>
    internal static double[] SortedDrawableValues(this double[] values) => [.. values.DrawableValues().OrderBy(v => v)];

    /// <summary>The end of a line: the index of its last point, in data order, whose x and y are both drawable and whose x
    /// lies in [<paramref name="xMin"/>, <paramref name="xMax"/>]. The legend's value and the end-of-line label both stand
    /// at this point, so a trailing gap never puts NaN in either. Only the indices both arrays have are paired.</summary>
    /// <param name="x">The x values of the line.</param>
    /// <param name="y">The y values of the line.</param>
    /// <param name="xMin">The lowest x the end may lie at; unbounded by default.</param>
    /// <param name="xMax">The highest x the end may lie at; unbounded by default.</param>
    /// <returns>The index, or -1 when no point qualifies.</returns>
    internal static int LastDrawableIndex(this double[] x, double[] y,
        double xMin = double.NegativeInfinity, double xMax = double.PositiveInfinity)
    {
        for (int i = Math.Min(x.Length, y.Length) - 1; i >= 0; i--)
        {
            if (x[i].IsDrawable() && y[i].IsDrawable() && x[i] >= xMin && x[i] <= xMax)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>The pairs (<paramref name="x"/>[i], <paramref name="y"/>[i]) a chart can draw: a pair with a missing value
    /// or an infinity on either side is not an observation, so a fit (a regression line, its residuals) is made from the
    /// others, as seaborn drops such a pair. A sample without a gap is handed back as the very arrays, and so are arrays of
    /// different lengths, which are no pairs at all (the fit refuses them as before).</summary>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values, parallel to <paramref name="x"/>.</param>
    /// <returns>The drawable pairs, in order.</returns>
    internal static XYData DrawablePairs(this double[] x, double[] y)
    {
        if (x.Length != y.Length)
        {
            return new(x, y);
        }

        var keep = Enumerable.Range(0, x.Length).Where(i => x[i].IsDrawable() && y[i].IsDrawable()).ToArray();
        return keep.Length == x.Length
            ? new(x, y)
            : new(Array.ConvertAll(keep, i => x[i]), Array.ConvertAll(keep, i => y[i]));
    }

    /// <summary>The range widened to take in <paramref name="value"/> when an axis of <paramref name="scale"/> can place
    /// it, unchanged otherwise: the one step every drawable range is folded from, so a range walked value by value (a
    /// live window that is never copied) follows the same rule as <see cref="DrawableRange(double[], AxisScale)"/>.</summary>
    /// <param name="range">The range folded so far; <see cref="Range1D.Empty"/> to start.</param>
    /// <param name="value">The next value.</param>
    /// <param name="scale">The scale of the axis the value is placed on.</param>
    /// <returns>The widened range.</returns>
    internal static Range1D Including(this Range1D range, double value, AxisScale scale = AxisScale.Linear) =>
        value.IsPlaceable(scale)
            ? new(Math.Min(range.Lo, value), Math.Max(range.Hi, value))
            : range;

    /// <summary>Whether an axis of <paramref name="scale"/> can place <paramref name="value"/>: a finite number, and on a
    /// log axis a positive one. The test every range is folded by (<see cref="Including"/>).</summary>
    /// <param name="value">The value.</param>
    /// <param name="scale">The scale of the axis it is placed on.</param>
    /// <returns><see langword="true"/> when the axis has a place for it.</returns>
    internal static bool IsPlaceable(this double value, AxisScale scale) => value.IsDrawable() && scale.HasPlaceFor(value);

    /// <summary>The range folded by <see cref="Including"/>, or <see langword="null"/> when no value was taken in: a series
    /// contributes nothing there.</summary>
    /// <param name="range">The folded range.</param>
    /// <returns>The range, or <see langword="null"/>.</returns>
    internal static Range1D? OrNone(this Range1D range) => range.IsEmpty ? null : range;
}
