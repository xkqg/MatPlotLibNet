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
/// a mark (a marker, a dot, a reference line, an annotation, a polar mark) at such a coordinate is skipped before any render
/// context sees it, and a line breaks there, as matplotlib masks an invalid point. No render context (SVG, Skia, a native
/// control) is handed a coordinate it cannot draw. A whole made of parts (a pie, a donut) counts a part that cannot be drawn
/// as 0 in its total.</para>
/// <para>One kind of value is clipped instead of masked: a finite value at or below zero on a log axis, which matplotlib's log
/// scale clips (<c>nonpositive='clip'</c>) and every other scale masks (a logit axis' 0 and 1, <c>nonpositive='mask'</c>). The
/// placement puts it a thousand decades beyond the axis (<see cref="DataTransform.DataToPixelUnclipped"/>, the one rule of
/// <c>PixelDirection.Beyond</c>), and what is drawn of a mark through it is what the plot leaves of it, cut at the edge where
/// the axis begins (<see cref="DataTransform.CutBeyondTheStart"/>): a line dives to that edge and climbs back, a fill runs
/// along it, a bar, a stem or an error bar that runs to it stops on it (the base pinned there,
/// <see cref="DataTransform.DataToPixelClippedY"/>, is that cut in closed form), and a mark with no extent there (a marker)
/// is never seen. A mark wholly beyond the axis is not drawn. An edge of a span runs to the plot edge the same way, as do an
/// infinity and ±<see cref="double.MaxValue"/> as <see cref="Axes.AddThreshold"/> stores its zone, and a span wholly beyond the
/// range draws nothing (<c>CartesianSpansPart</c>). A missing value is still missing everywhere.</para></summary>
internal static class MissingValueExtensions
{
    extension(double value)
    {
        /// <summary>Whether this value is missing: NaN. Infinity is not missing; it is a value beyond every
        /// scale.</summary>
        /// <returns><see langword="true"/> when this value is NaN.</returns>
        internal bool IsMissing() => double.IsNaN(value);

        /// <summary>Whether a chart can draw this value: a finite number.</summary>
        /// <returns><see langword="false"/> for NaN and for either infinity.</returns>
        internal bool IsDrawable() => double.IsFinite(value);

        /// <summary>Whether this value is a number a share, a scale or a width can be taken of: finite and above 0. The total a
        /// chart divides its parts by, a bandwidth, a quantity that is drawn.</summary>
        /// <returns><see langword="false"/> for 0, for a negative value, for NaN and for either infinity.</returns>
        internal bool IsPositiveFinite() => value > 0 && double.IsFinite(value);

        /// <summary>Whether a range on an axis drawn by <paramref name="rule"/> takes this value, as matplotlib's data limits do: a finite
        /// number at or above the lowest value the scale places (<see cref="ScaleRule.LowestPlaced"/>), so a log and a logit axis leave 0
        /// and below out, and a logit axis keeps 1 and above, which its range then ends one smallest positive value short of
        /// (<see cref="ScaleRule.Limited"/>, matplotlib's <c>limit_range_for_scale</c>). The test every range is folded by
        /// (<c>Including</c>).</summary>
        /// <param name="rule">The rule of the scale of the axis it is placed on.</param>
        /// <returns><see langword="true"/> when a range takes it.</returns>
        internal bool IsPlaceable(ScaleRule rule) => value.IsTakenFrom(rule.LowestPlaced);

        /// <summary>Whether a range folded from <paramref name="lowest"/> up takes this value: a finite number at or above it
        /// (<see cref="IsPlaceable"/> with the scale's lowest placed value read once for a whole scan).</summary>
        /// <param name="lowest">The lowest value the scale places (<see cref="ScaleRule.LowestPlaced"/>).</param>
        /// <returns><see langword="true"/> when a range takes it.</returns>
        internal bool IsTakenFrom(double lowest) => value.IsDrawable() && value >= lowest;
    }

    extension(double[] values)
    {
        /// <summary>The smallest and the largest value of these values an axis drawn by <paramref name="rule"/> can
        /// place: missing values and infinities are left out, as matplotlib leaves them out of its data limits, and on a log
        /// axis so are 0 and the negative values, as matplotlib's data limits of a log axis keep only the positive ones (a mark
        /// through such a value is still drawn to the edge, the rule of the class).</summary>
        /// <param name="rule">The rule of the scale of the axis the values are placed on; <see langword="null"/> for a linear one.</param>
        /// <returns>The range, or <see langword="null"/> when no value can be placed: a series contributes nothing there.</returns>
        internal Range1D? DrawableRange(ScaleRule? rule = null)
        {
            double lowest = rule?.LowestPlaced ?? ScaleRule.NoFloor;
            double lo = Range1D.Empty.Lo, hi = Range1D.Empty.Hi;
            foreach (double value in values)
            {
                if (!value.IsDrawable()) continue;   // the fold of IncludingFrom, the floor asked only of a value that beats an end
                if (value < lo && value >= lowest) lo = value;
                if (value > hi && value >= lowest) hi = value;
            }

            if (lo == 0 || hi == 0)
            {
                bool minusZero = false, plusZero = false;   // -0 is the lowest of the zeros and 0 the highest, as Math.Min and Math.Max say
                foreach (double value in values)
                {
                    if (value != 0) continue;
                    if (double.IsNegative(value)) minusZero = true;
                    else plusZero = true;
                }

                if (lo == 0) lo = minusZero ? -0.0 : 0.0;
                if (hi == 0) hi = plusZero ? 0.0 : -0.0;
            }

            return new Range1D(lo, hi).OrNone();
        }

        /// <summary>The unbroken runs of a series' data, in order: the indices at which these values,
        /// <paramref name="y"/> and, when given, <paramref name="y2"/> are all drawable.</summary>
        /// <param name="y">The y values, parallel to this array.</param>
        /// <param name="y2">A second set of y values parallel to this array (an area's lower edge), or
        /// <see langword="null"/>.</param>
        /// <returns>Each run of consecutive drawable indices.</returns>
        internal IEnumerable<IndexRange> DrawableIndexRuns(double[] y, double[]? y2 = null) =>
            Runs(values.Length, i => values[i].IsDrawable() && y[i].IsDrawable() && (y2 is null || y2[i].IsDrawable()));

        /// <summary>The values of this array in <paramref name="run"/>: the very array when the run covers
        /// all of it.</summary>
        /// <param name="run">The indices to take.</param>
        /// <returns>The values in the run.</returns>
        internal double[] Slice(IndexRange run) =>
            run.StartInclusive == 0 && run.EndExclusive == values.Length ? values : values[run.StartInclusive..run.EndExclusive];

        /// <summary>The observations of a sample a chart can draw from: its drawable values, in order. A missing observation
        /// (NaN) is not there and an infinity cannot be placed, so a box or a violin is drawn from the others, as its range is
        /// taken over them. A sample without a gap is handed back as the very array.</summary>
        /// <returns>The drawable observations.</returns>
        internal double[] DrawableValues()
        {
            int count = values.DrawableCount();
            return count == values.Length ? values : values.DrawableCopy(count);
        }

        /// <summary>The observations of a sample a chart can draw from, ascending: what a box, a violin, a density curve, a pair
        /// grid's diagonal, an ECDF, a ridgeline, a Q-Q plot and a stem-and-leaf display are drawn from, and what their reach is
        /// measured over, so the picture and the range cannot read different samples. The sample itself is never sorted: the result is a copy.</summary>
        /// <returns>The drawable observations, smallest first, in an array of their own; empty when none can be drawn.</returns>
        internal double[] SortedDrawableValues()
        {
            double[] sorted = values.DrawableCopy(values.DrawableCount());
            Array.Sort(sorted);
            return sorted;
        }

        /// <summary>How many values of this array a chart can draw.</summary>
        private int DrawableCount()
        {
            int count = 0;
            foreach (double v in values)
                if (v.IsDrawable()) count++;
            return count;
        }

        /// <summary>The drawable values of this array, in order, in a new array of exactly <paramref name="count"/>: one pass,
        /// one allocation.</summary>
        /// <param name="count">The number of drawable values (<see cref="DrawableCount"/>).</param>
        private double[] DrawableCopy(int count)
        {
            double[] drawable = new double[count];
            int k = 0;
            foreach (double v in values)
                if (v.IsDrawable()) drawable[k++] = v;
            return drawable;
        }

        /// <summary>The end of a line: the index of its last point, in data order, whose x and y are both drawable and whose x
        /// lies in [<paramref name="xMin"/>, <paramref name="xMax"/>]. The legend's value and the end-of-line label both stand
        /// at this point, so a trailing gap never puts NaN in either. Only the indices both arrays have are paired.</summary>
        /// <param name="y">The y values of the line.</param>
        /// <param name="xMin">The lowest x the end may lie at; unbounded by default.</param>
        /// <param name="xMax">The highest x the end may lie at; unbounded by default.</param>
        /// <returns>The index, or -1 when no point qualifies.</returns>
        internal int LastDrawableIndex(double[] y,
            double xMin = double.NegativeInfinity, double xMax = double.PositiveInfinity)
        {
            for (int i = Math.Min(values.Length, y.Length) - 1; i >= 0; i--)
            {
                if (values[i].IsDrawable() && y[i].IsDrawable() && values[i] >= xMin && values[i] <= xMax)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The pairs (this array[i], <paramref name="y"/>[i]) a chart can draw: a pair with a missing value
        /// or an infinity on either side is not an observation, so a fit (a regression line, its residuals) is made from the
        /// others, as seaborn drops such a pair. A sample without a gap is handed back as the very arrays, and so are arrays of
        /// different lengths, which are no pairs at all (the fit refuses them as before).</summary>
        /// <param name="y">The y values, parallel to this array.</param>
        /// <returns>The drawable pairs, in order.</returns>
        internal XYData DrawablePairs(double[] y)
        {
            if (values.Length != y.Length)
            {
                return new(values, y);
            }

            var keep = Enumerable.Range(0, values.Length).Where(i => values[i].IsDrawable() && y[i].IsDrawable()).ToArray();
            return keep.Length == values.Length
                ? new(values, y)
                : new(Array.ConvertAll(keep, i => values[i]), Array.ConvertAll(keep, i => y[i]));
        }
    }

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

    extension(IReadOnlyList<IndexRange> runs)
    {
        /// <summary>The line made of these runs of <paramref name="x"/> and <paramref name="y"/> (the drawable runs
        /// <c>DrawableIndexRuns</c> found), each run passed through <paramref name="map"/> on its own and the results joined
        /// by one NaN point, so the line still breaks at every gap and no stage that reshapes a line (a downsampler, a
        /// spline) ever sees a point of two runs at once. The one place a reshaped line is split at its gaps and joined again.
        /// A single run is mapped and handed back as <paramref name="map"/> returns it.</summary>
        /// <param name="x">The x values the runs index.</param>
        /// <param name="y">The y values, parallel to <paramref name="x"/>.</param>
        /// <param name="map">What is done to one run: its x and y values in, the reshaped run out.</param>
        /// <returns>The reshaped runs, in order, one NaN point between each two; empty when there is no run.</returns>
        internal XYData JoinedAcrossGaps(double[] x, double[] y, Func<XYData, XYData> map)
        {
            if (runs.Count == 1)
            {
                return map(new(x.Slice(runs[0]), y.Slice(runs[0])));
            }

            var line = new GappedLine();
            foreach (var run in runs)
            {
                line.Gap();
                var part = map(new(x.Slice(run), y.Slice(run)));
                line.AddRange(part.X, part.Y);
            }

            return line.ToData();
        }
    }

    extension(IEnumerable<double[]> arrays)
    {
        /// <summary>The smallest and the largest value over several arrays (the datasets of a box plot, the two edges of an
        /// area, the layers of a stack) an axis drawn by <paramref name="rule"/> can place: missing values and infinities are left
        /// out, and on a log axis so are 0 and the negative values, the rule of <c>DrawableRange</c>.</summary>
        /// <param name="rule">The rule of the scale of the axis the values are placed on; <see langword="null"/> for a linear one.</param>
        /// <returns>The range, or <see langword="null"/> when no array holds a value the axis can place.</returns>
        internal Range1D? DrawableRange(ScaleRule? rule = null)
        {
            double lowest = rule?.LowestPlaced ?? ScaleRule.NoFloor;
            var range = Range1D.Empty;
            foreach (double[] values in arrays)
            {
                foreach (double value in values)
                {
                    range = range.IncludingFrom(value, lowest);
                }
            }

            return range.OrNone();
        }
    }

    extension(IEnumerable<Range1D> spans)
    {
        /// <summary>The extent of the spans a chart can draw (a task bar, a broken-bar segment, a state segment, an arrow):
        /// a span with an end that is missing or infinite is not drawn, so neither of its ends counts.</summary>
        /// <returns>The range, or <see langword="null"/> when no span can be drawn.</returns>
        internal Range1D? DrawableRange()
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
    }

    extension(double[,] z)
    {
        /// <summary>Whether the grid cell from [<paramref name="row"/>, <paramref name="col"/>] to [row + 1, col + 1] can be
        /// drawn: its four corner values and the grid lines on its four sides all drawable. matplotlib masks a contour cell
        /// with a missing corner; a cell whose corner is missing would otherwise be traced as if the corner lay below every
        /// level.</summary>
        /// <param name="x">The column coordinates.</param>
        /// <param name="y">The row coordinates.</param>
        /// <param name="row">The cell's lower row.</param>
        /// <param name="col">The cell's left column.</param>
        /// <returns><see langword="true"/> when every corner of the cell can be drawn.</returns>
        internal bool IsDrawableCell(double[] x, double[] y, int row, int col) =>
            x[col].IsDrawable() && x[col + 1].IsDrawable() && y[row].IsDrawable() && y[row + 1].IsDrawable()
            && z[row, col].IsDrawable() && z[row, col + 1].IsDrawable()
            && z[row + 1, col].IsDrawable() && z[row + 1, col + 1].IsDrawable();

        /// <summary>The smallest and the largest value of a grid a scale drawn by <paramref name="rule"/> can place: missing values and
        /// infinities are left out, and on a log scale so are 0 and the negative values (a log-normalised contour masks them).</summary>
        /// <param name="rule">The rule of the scale the values are placed on; <see langword="null"/> for a linear one.</param>
        /// <returns>The range, or <see langword="null"/> when no cell holds such a value.</returns>
        internal Range1D? DrawableRange(ScaleRule? rule = null)
        {
            double lowest = rule?.LowestPlaced ?? ScaleRule.NoFloor;
            var range = Range1D.Empty;
            foreach (double value in z)
            {
                range = range.IncludingFrom(value, lowest);
            }

            return range.OrNone();
        }
    }

    extension(int[,] counts)
    {
        /// <summary>The smallest and the largest count of a grid a scale drawn by <paramref name="rule"/> can place: on a log scale the
        /// counts above 0 only (a count is always finite).</summary>
        /// <param name="rule">The rule of the scale the counts are placed on; <see langword="null"/> for a linear one.</param>
        /// <returns>The range, or <see langword="null"/> when no cell holds such a count.</returns>
        internal Range1D? DrawableRange(ScaleRule? rule = null)
        {
            double lowest = rule?.LowestPlaced ?? ScaleRule.NoFloor;
            var range = Range1D.Empty;
            foreach (int count in counts)
            {
                range = range.IncludingFrom(count, lowest);
            }

            return range.OrNone();
        }
    }

    extension(Point point)
    {
        /// <summary>Whether a chart can draw this point: both coordinates finite. A point the transform could
        /// not place (a value inside an axis break, a value a logit axis masks, a non-positive value on a log axis placed by
        /// <see cref="DataTransform.DataToPixel"/>) comes out NaN and is not drawable either.</summary>
        /// <returns><see langword="true"/> when both coordinates are finite.</returns>
        internal bool IsDrawable() => point.X.IsDrawable() && point.Y.IsDrawable();

        /// <summary>Whether a chart can draw the straight line from this point to <paramref name="to"/>: both its
        /// ends drawable. A stem, an error bar or a connector with an end that cannot be placed is not drawn.</summary>
        /// <param name="to">The other end, in pixels.</param>
        /// <returns><see langword="true"/> when both ends are drawable.</returns>
        internal bool CanDrawLineTo(Point to) => point.IsDrawable() && to.IsDrawable();
    }

    extension(IReadOnlyList<Point> points)
    {
        /// <summary>The lines these points are drawn as, in order: each unbroken run of them that can be drawn and is a line
        /// (<see cref="Rendering.PathSinkExtensions"/>' <c>FormsALine</c>, two points or more). A line breaks at every point that cannot be
        /// drawn, as matplotlib breaks a line at a masked or NaN point, and a point alone between two gaps joins nothing. Points without a
        /// gap are one line, the very list given, so a line without a gap is drawn from the same points as before. The one place a
        /// renderer or a surface turns points into the lines it draws; one walk, no enumerator stacked over another (every series' line is
        /// asked this on every draw).</summary>
        /// <returns>Each line, in order; none when no two neighbouring points can be drawn.</returns>
        internal IEnumerable<IReadOnlyList<Point>> LineRuns()
        {
            foreach (var run in Runs(points.Count, i => points[i].IsDrawable()))
            {
                if (run.Count == points.Count)
                {
                    if (points.FormsALine) yield return points;
                    continue;
                }

                var part = new Point[run.Count];
                for (int i = 0; i < part.Length; i++)
                {
                    part[i] = points[run.StartInclusive + i];
                }

                if (part.FormsALine) yield return part;
            }
        }

        /// <summary>Whether a chart can draw the face whose corners are these points (a 3-D surface quad, a
        /// triangle, a bar's side): every corner drawable. A face that needs a vertex which cannot be placed is skipped
        /// whole.</summary>
        /// <returns><see langword="true"/> when every corner is drawable.</returns>
        /// <remarks>The corners are read by index: a 3-D surface asks this of every quad of every draw, and walking the list
        /// through its enumerator allocates one per call.</remarks>
        internal bool IsDrawable()
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (!points[i].IsDrawable()) return false;
            }

            return true;
        }
    }

    extension(Rect rect)
    {
        /// <summary>Whether a chart can draw this rectangle: its corner and its size all finite. A bar, a span or a
        /// waterfall step whose value, foot or edge cannot be placed comes out with a NaN edge and is not drawable.</summary>
        /// <returns><see langword="true"/> when all four numbers are finite.</returns>
        internal bool IsDrawable() =>
            rect.X.IsDrawable() && rect.Y.IsDrawable() && rect.Width.IsDrawable() && rect.Height.IsDrawable();
    }

    extension(Range1D range)
    {
        /// <summary>The range widened to take in <paramref name="value"/> when it is a finite number, unchanged otherwise: the fold of a
        /// linear axis, which places every finite value (<see cref="ScaleRule.NoFloor"/>), and the one step a range walked value by
        /// value (a live window that is never copied) is folded by.</summary>
        /// <param name="value">The next value.</param>
        /// <returns>The widened range.</returns>
        internal Range1D Including(double value) => value.IsDrawable() ? range.Widened(value) : range;

        /// <summary>The range widened to take in <paramref name="value"/> when an axis drawn by <paramref name="rule"/> takes it
        /// (<see cref="IsPlaceable"/>), unchanged otherwise: the rule <c>DrawableRange</c> folds by.</summary>
        /// <param name="value">The next value.</param>
        /// <param name="rule">The rule of the scale of the axis the value is placed on.</param>
        /// <returns>The widened range.</returns>
        internal Range1D Including(double value, ScaleRule rule) => range.IncludingFrom(value, rule.LowestPlaced);

        /// <summary>The range widened to take in <paramref name="value"/> when it is a finite number at or above
        /// <paramref name="lowest"/>, unchanged otherwise: <c>Including(value, rule)</c> with the scale's lowest placed value
        /// read once for a whole scan.</summary>
        /// <param name="value">The next value.</param>
        /// <param name="lowest">The lowest value the scale places (<see cref="ScaleRule.LowestPlaced"/>).</param>
        /// <returns>The widened range.</returns>
        internal Range1D IncludingFrom(double value, double lowest) => value.IsTakenFrom(lowest) ? range.Widened(value) : range;

        /// <summary>The range stretched to hold <paramref name="value"/>.</summary>
        /// <param name="value">A value the range takes.</param>
        /// <returns>The widened range.</returns>
        internal Range1D Widened(double value) => new(Math.Min(range.Lo, value), Math.Max(range.Hi, value));
        /// <summary>The range widened to take in <paramref name="span"/>, the extent of one series' drawable values
        /// (<c>DrawableRange</c>), unchanged when it has none.</summary>
        /// <param name="span">The extent to take in, or <see langword="null"/> when nothing could be placed.</param>
        /// <returns>The widened range.</returns>
        internal Range1D IncludingSpan(Range1D? span) => span is { } extent ? range.Merge(extent.Lo, extent.Hi) : range;

        /// <summary>The range folded by <c>Including</c>, or <see langword="null"/> when no value was taken in: a series
        /// contributes nothing there.</summary>
        /// <returns>The range, or <see langword="null"/>.</returns>
        internal Range1D? OrNone() => range.IsEmpty ? null : range;
    }
}
