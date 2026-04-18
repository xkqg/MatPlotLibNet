// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// A one-axis numeric range expressed as immutable <c>(Lo, Hi)</c>. Each method returns a
/// new instance, so range pipelines chain fluently:
/// <code>
/// var final = Range1D.FromAxis(axes.XAxis)
///     .MergeAll(contributions, c => (c.XMin, c.XMax))
///     .Normalized()
///     .Padded(0.05, axes.XAxis)
///     .ClampSticky(sticky, unpadded, axes.XAxis)
///     .ExpandedToNiceBoundsIfAuto(axes.XAxis, unpadded, hasSticky, locator);
/// </code>
/// Encapsulates the seven small passes <see cref="CartesianAxesRenderer.ComputeDataRanges"/>
/// previously inlined for both X and Y, eliminating the X-vs-Y duplication.
/// </summary>
public readonly record struct Range1D(double Lo, double Hi)
{
    /// <summary>The accumulator seed before any contribution has been merged: inverted so the
    /// first <see cref="Merge"/> always overwrites both endpoints.</summary>
    public static Range1D Empty => new(double.MaxValue, double.MinValue);

    /// <summary>Width of the range. Negative when <see cref="IsEmpty"/>.</summary>
    public double Width => Hi - Lo;

    /// <summary>True when no series has contributed to the range (still at <see cref="Empty"/>).</summary>
    public bool IsEmpty => Lo == double.MaxValue;

    /// <summary>True when only <c>Lo</c> has a real value — a series reported its min without a max.
    /// Guards against the <c>double.MaxValue - double.MinValue = +Inf</c> edge case.</summary>
    public bool IsLopsided => Lo != double.MaxValue && Hi == double.MinValue;

    /// <summary>Seeds a running range from an <see cref="Axis"/>. When the user has set
    /// <see cref="Axis.Min"/> / <see cref="Axis.Max"/>, those become the accumulator starting
    /// values; a series point only widens the range when it extends beyond them.</summary>
    public static Range1D FromAxis(Axis axis) => new(
        axis.Min ?? double.MaxValue,
        axis.Max ?? double.MinValue);

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
    /// Must be called after aggregation, BEFORE the padding/sticky/nice-bound pipeline.
    /// </summary>
    public Range1D Normalized()
    {
        if (IsEmpty) return new(0, 1);
        double lo = Lo;
        double hi = IsLopsided ? Lo + 1 : Hi;
        if (Math.Abs(hi - lo) < 1e-10) { lo -= 0.5; hi += 0.5; }
        return new(lo, hi);
    }

    /// <summary>
    /// Applies symmetric %-margin padding. Edges bound by a user-set <see cref="Axis.Min"/> /
    /// <see cref="Axis.Max"/> are left untouched — users asking for an exact limit should get it.
    /// </summary>
    public Range1D Padded(double margin, Axis axis)
    {
        double pad = Width * margin;
        return new(
            axis.Min.HasValue ? Lo : Lo - pad,
            axis.Max.HasValue ? Hi : Hi + pad);
    }

    /// <summary>
    /// Sticky-edge clamp for one series' contribution — mirrors matplotlib's
    /// <c>sticky_edges</c>. When padding pushed past a registered sticky edge AND the
    /// <paramref name="unpadded"/> range was still inside it, snap back. Guarded by
    /// <paramref name="axis"/>.<c>Min</c>/<c>Max</c> so user-set limits win.
    /// </summary>
    public Range1D ClampSticky(double? stickyMin, double? stickyMax, Range1D unpadded, Axis axis)
    {
        double lo = Lo;
        double hi = Hi;
        if (stickyMin.HasValue && lo < stickyMin.Value
            && unpadded.Lo >= stickyMin.Value && !axis.Min.HasValue)
            lo = stickyMin.Value;
        if (stickyMax.HasValue && hi > stickyMax.Value
            && unpadded.Hi <= stickyMax.Value && !axis.Max.HasValue)
            hi = stickyMax.Value;
        return new(lo, hi);
    }

    /// <summary>
    /// Nice-number view-limit expansion (matplotlib's <c>MaxNLocator.view_limits</c>):
    /// rounds the bounds OUTWARD to the next nice tick boundary computed from
    /// <paramref name="unpadded"/>. Applied only when it widens past the current range —
    /// never pulls the axis back toward the data. Skipped when the axis has user-set
    /// limits, a custom <see cref="Axis.TickLocator"/>, any sticky edges, or a degenerate
    /// unpadded range. Using the <em>unpadded</em> range as the input is critical:
    /// feeding the padded range can bloat <c>[0,10]</c>→<c>[-0.5,10.5]</c> all the way to
    /// <c>[-2,12]</c> because the rounding step changes with the range width.
    /// </summary>
    public Range1D ExpandedToNiceBoundsIfAuto(
        Axis axis, Range1D unpadded, bool hasSticky, AutoLocator locator)
    {
        if (hasSticky || axis.Min.HasValue || axis.Max.HasValue
            || axis.TickLocator is not null || unpadded.Hi <= unpadded.Lo
            || axis.Margin == 0)
            return this;
        var (nLo, nHi) = locator.ExpandToNiceBounds(unpadded.Lo, unpadded.Hi);
        return new(Math.Min(Lo, nLo), Math.Max(Hi, nHi));
    }
}
