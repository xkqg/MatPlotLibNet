// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// A one-axis numeric range expressed as immutable <c>(Lo, Hi)</c>. Each method returns a new instance, so a range is folded and
/// shaped fluently (<see cref="Merge"/>, <see cref="Normalized"/>). What an axis does with its range (seeded from its limits, padded,
/// held to the sticky edges, put on its scale, widened to nice bounds) is the axis' autoscale policy, <c>AxisAutoscale</c>; the
/// members here that take an <see cref="Axis"/> hand the range to it:
/// <code>
/// var final = Range1D.FromAxis(axes.XAxis)
///     .Merge(seriesMin, seriesMax)
///     .Normalized()
///     .Padded(0.05, axes.XAxis)
///     .ClampSticky(sticky, null, unpadded, axes.XAxis)
///     .ExpandedToNiceBoundsIfAuto(axes.XAxis, unpadded, hasSticky, locator);
/// </code>
/// </summary>
/// <param name="Lo">The low end of the range.</param>
/// <param name="Hi">The high end of the range.</param>
public readonly record struct Range1D(double Lo, double Hi)
{
    /// <summary>The accumulator seed before any contribution has been merged: inverted so the
    /// first <see cref="Merge"/> always overwrites both endpoints.</summary>
    public static Range1D Empty => new(double.MaxValue, double.MinValue);

    /// <summary>Width of the range. Negative when <see cref="IsEmpty"/>.</summary>
    public double Width => Hi - Lo;

    /// <summary>The span <paramref name="length"/> long centred on <paramref name="centre"/>: from half the length before it to half the
    /// length after it. The one rule of a span centred on a point, in any unit: a bar across its axis, a label along its line, a legend
    /// on the middle of its plot, an arrow on its pivot, a box on a text's axis (<c>CategorySlot.Around</c> reads it in data units).</summary>
    /// <param name="centre">Where the middle of the span lies.</param>
    /// <param name="length">How long the span is.</param>
    /// <returns>The span, <see cref="Lo"/> before the centre and <see cref="Hi"/> after it.</returns>
    internal static Range1D Around(double centre, double length) => new(centre - length / 2, centre + length / 2);

    /// <summary>The span <paramref name="length"/> long centred in the room that starts at <paramref name="start"/> and runs
    /// <paramref name="room"/>: what the span leaves of the room is shared equally before and after it. matplotlib's
    /// <c>Bbox.anchored</c> with the anchor <c>'C'</c> along one axis (<c>x0 + (W - w) * 0.5</c>), the way a colour bar shrunk along
    /// its plot and the square of a 3-D axes stand; the same span as <see cref="Around"/> the room's middle, worked out from the room's
    /// start as matplotlib works it, so its numbers are matplotlib's to the last bit.</summary>
    /// <param name="start">Where the room begins.</param>
    /// <param name="room">How long the room is.</param>
    /// <param name="length">How long the span is.</param>
    /// <returns>The span, starting at <see cref="Lo"/>.</returns>
    internal static Range1D CentredIn(double start, double room, double length)
    {
        double lo = start + (room - length) / 2;
        return new(lo, lo + length);
    }

    /// <summary>True when no series has contributed to the range (still at <see cref="Empty"/>).</summary>
    public bool IsEmpty => Lo == double.MaxValue;

    /// <summary>True when only <c>Lo</c> has a real value — a series reported its min without a max.
    /// Guards against the <c>double.MaxValue - double.MinValue = +Inf</c> edge case.</summary>
    public bool IsLopsided => Lo != double.MaxValue && Hi == double.MinValue;

    /// <summary>Whether both ends are finite: false only for an end set at infinity (or NaN), as a data value that is not finite is never
    /// folded in and the seeds <see cref="Empty"/> and <see cref="IsLopsided"/> leave are finite.</summary>
    internal bool HasFiniteEnds => double.IsFinite(Lo) && double.IsFinite(Hi);

    /// <summary>The same interval with <see cref="Lo"/> &lt;= <see cref="Hi"/>. A reversed limit (<c>SetYLim(5.5, 0.5)</c>,
    /// matplotlib's way to put rank 1 at the top) is drawn upside down, but what lies on the axis (its ticks, its decades,
    /// the data it shows) is what lies in this interval; only where it is drawn follows the drawn orientation, which the
    /// transform takes care of.</summary>
    internal Range1D Interval => Lo <= Hi ? this : new(Hi, Lo);

    /// <summary>How far past an end, as a share of the range's width, a value still lies on it (<see cref="HoldsClose"/>):
    /// matplotlib's <c>rtol</c> for the ticks it keeps in view.</summary>
    internal const double CloseTolerance = 1e-10;

    /// <summary>Whether <paramref name="value"/> lies on this range, its ends included, give or take
    /// <see cref="CloseTolerance"/> of its width past either end, whichever way the range runs (matplotlib's
    /// <c>_interval_contains_close</c>). A value a locator computed for an end lands a rounding error either side of it and
    /// still lies on it; NaN lies on no range.</summary>
    /// <param name="value">The value, in the unit of the range.</param>
    /// <returns><see langword="true"/> when the value lies on the range.</returns>
    internal bool HoldsClose(double value)
    {
        var (lo, hi) = Interval;
        double slack = (hi - lo) * CloseTolerance;
        return value >= lo - slack && value <= hi + slack;
    }

    /// <summary>This range turned the way <paramref name="drawn"/> runs: unchanged when <paramref name="drawn"/> runs
    /// low to high, swapped when it is reversed. The way back from <see cref="Interval"/>.</summary>
    /// <param name="drawn">The range whose orientation is kept.</param>
    /// <returns>This range in the orientation of <paramref name="drawn"/>.</returns>
    internal Range1D OrientedLike(Range1D drawn) => drawn.Lo > drawn.Hi ? new(Hi, Lo) : this;

    /// <summary>Seeds a running range from an <see cref="Axis"/>: a side the user has set
    /// (<see cref="Axis.Min"/> / <see cref="Axis.Max"/>) starts the accumulator, an unset side starts empty. The
    /// series fold (<see cref="Merge"/>) can widen the seed; the renderer then puts every set side back, so a set limit is the
    /// range drawn even when data lies beyond it. The axis' autoscale policy says it (<c>AxisAutoscale.Seed</c>).</summary>
    /// <param name="axis">The axis whose limits seed the range.</param>
    /// <returns>The seed.</returns>
    public static Range1D FromAxis(Axis axis) => new AxisAutoscale(axis, isX: false, Styling.Theme.Default).Seed;

    /// <summary>Expands this range to include a single series' contribution on one axis.
    /// Null inputs (no data on that axis) leave the running range untouched.</summary>
    public Range1D Merge(double? seriesMin, double? seriesMax) => new(
        seriesMin.HasValue && seriesMin.Value < Lo ? seriesMin.Value : Lo,
        seriesMax.HasValue && seriesMax.Value > Hi ? seriesMax.Value : Hi);

    /// <summary>
    /// Normalizes degenerate states that would otherwise produce nonsense labels:
    /// <list type="bullet">
    ///   <item><see cref="IsEmpty"/> → <c>(0, 1)</c></item>
    ///   <item><see cref="IsLopsided"/> → <c>(Lo, Lo + 1)</c></item>
    ///   <item>zero-width → centred on <c>Lo</c> with ±0.5 breathing room</item>
    /// </list>
    /// Must be called after aggregation, BEFORE the padding/sticky/nice-bound pipeline. The library's own themes open the range of an
    /// axis this way; the matplotlib themes open it as matplotlib's locators do (<c>AxisAutoscale.Opened</c>).
    /// </summary>
    public Range1D Normalized()
    {
        if (IsEmpty) return new(0, 1);
        double lo = Lo;
        double hi = IsLopsided ? Lo + 1 : Hi;
        return new Range1D(lo, hi).OpenedWhenFlat();
    }

    /// <summary>This range as matplotlib's autoscale reads the data limits it was folded from, low to high: no data
    /// (<see cref="IsEmpty"/>) as <c>(-inf, +inf)</c>, a low end alone (<see cref="IsLopsided"/>) as that one value at both ends.</summary>
    internal Range1D DataLimits =>
        IsEmpty ? new(double.NegativeInfinity, double.PositiveInfinity) : IsLopsided ? new(Lo, Lo) : Interval;

    /// <summary>How close the ends of a range may stand and the range still be flat: a span narrower than this is a rounding error,
    /// not a width.</summary>
    internal const double FlatTolerance = 1e-10;

    /// <summary>This range opened by half a unit on each side when it is flat (its ends less than <see cref="FlatTolerance"/> apart),
    /// so a single value, or values that differ by a rounding error, still have a span to be drawn over; any other range as it is. The
    /// one place a flat span is opened: the axis' range (<see cref="Normalized"/>), a series' own range (a graph whose nodes stand on one
    /// line, a rotation chart of one point) and the bins of a volume profile of one price.</summary>
    /// <returns>The range, opened when flat.</returns>
    internal Range1D OpenedWhenFlat() => Math.Abs(Hi - Lo) < FlatTolerance ? new(Lo - 0.5, Hi + 0.5) : this;

    /// <summary>
    /// Applies symmetric %-margin padding. Edges bound by a user-set <see cref="Axis.Min"/> /
    /// <see cref="Axis.Max"/> are left untouched — users asking for an exact limit should get it. The range is padded in the space
    /// the axis is drawn in (a log axis in decades), by the axis' autoscale policy (<c>AxisAutoscale.Padded</c>).
    /// </summary>
    /// <param name="margin">The share of the range added at each edge.</param>
    /// <param name="axis">The axis whose scale the padding is worked out in and whose given limits stay.</param>
    /// <returns>The padded range.</returns>
    public Range1D Padded(double margin, Axis axis) => new AxisAutoscale(axis, isX: false, Styling.Theme.Default).Padded(this, margin);

    /// <summary>A log axis cannot place a non-positive value; when the floor still is one (an explicit
    /// <c>Min</c> of 0, a series that does not mask) it is lifted to three decades under the ceiling — a
    /// visible, finite axis rather than a NaN transform that blanks every point. A reversed range is lifted as its
    /// interval and keeps its orientation. Any other scale keeps the range.</summary>
    /// <param name="axis">The axis whose scale says whether the range is lifted.</param>
    /// <returns>The lifted range.</returns>
    public Range1D PositiveForLog(Axis axis) => new AxisAutoscale(axis, isX: false, Styling.Theme.Default).Lifted(this);

    /// <summary>
    /// Sticky-edge clamp for one series' contribution — mirrors matplotlib's
    /// <c>sticky_edges</c>. When padding pushed past a registered sticky edge AND the
    /// <paramref name="unpadded"/> range was still inside it, snap back. Guarded by
    /// <paramref name="axis"/>.<c>Min</c>/<c>Max</c> so user-set limits win (<c>AxisAutoscale.ClampSticky</c>).
    /// </summary>
    public Range1D ClampSticky(double? stickyMin, double? stickyMax, Range1D unpadded, Axis axis) =>
        new AxisAutoscale(axis, isX: false, Styling.Theme.Default).ClampSticky(this, stickyMin, stickyMax, unpadded);

    /// <summary>
    /// Nice-number view-limit expansion (matplotlib's <c>MaxNLocator.view_limits</c>):
    /// rounds the bounds OUTWARD to the next nice tick boundary computed from
    /// <paramref name="unpadded"/>. Applied only when it widens past the current range —
    /// never pulls the axis back toward the data. Skipped when the axis has user-set
    /// limits, any sticky edges, a degenerate unpadded range, or a locator: its own
    /// <see cref="Axis.TickLocator"/> or the one its scale brings (log decades, symmetric log)
    /// (<c>AxisAutoscale.ExpandedToNiceBounds</c>).
    /// </summary>
    /// <remarks>This member is not told which axis it reads, so it reads it as a Y axis: a date scale brings its ticks only
    /// on an X axis, and a date Y axis is widened like a linear one.</remarks>
    public Range1D ExpandedToNiceBoundsIfAuto(
        Axis axis, Range1D unpadded, bool hasSticky, AutoLocator locator)
        => new AxisAutoscale(axis, isX: false, Styling.Theme.Default).ExpandedToNiceBounds(this, unpadded, hasSticky, locator);

    /// <summary>matplotlib's <c>transforms._nonsingular(vmin, vmax, expander, tiny)</c> (3.11.2, <c>increasing=True</c>): an end that is
    /// not finite gives <c>(-expander, expander)</c>; a reversed range is put in order; both ends within about 2e-287 of 0 give
    /// <c>(-expander, expander)</c>; a range whose width is at most <paramref name="tiny"/> of its larger end (in absolute value) has its
    /// low end lowered and its high end raised by <paramref name="expander"/> of that end's own size, or is <c>(-expander, expander)</c>
    /// when both ends are 0.</summary>
    /// <param name="expander">The fraction of each end the range is widened by.</param>
    /// <param name="tiny">The width, as a fraction of the larger end, up to which the range counts as singular.</param>
    /// <returns>The widened range, or this one, in order, when it is wide enough.</returns>
    internal Range1D NonsingularBy(double expander, double tiny)
    {
        if (!double.IsFinite(Lo) || !double.IsFinite(Hi)) return new(-expander, expander);
        double lo = Math.Min(Lo, Hi), hi = Math.Max(Lo, Hi);
        double maxAbs = Math.Max(Math.Abs(lo), Math.Abs(hi));
        if (maxAbs < 1e6 / tiny * SmallestNormal) return new(-expander, expander);
        return hi - lo <= maxAbs * tiny ? new(lo - expander * Math.Abs(lo), hi + expander * Math.Abs(hi)) : new(lo, hi);
    }

    /// <summary>The smallest positive normal double (numpy's <c>finfo(float).tiny</c>), which <see cref="NonsingularBy"/> scales its
    /// near-zero arm by.</summary>
    private const double SmallestNormal = 2.2250738585072014E-308;
}
