// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What an axis does with the range its data reach (matplotlib's <c>autoscale_view</c> for one axis): seeded from the limits it
/// sets, every set side exact, put on its scale (<see cref="ScaleRule.Limited"/>), padded in the space it is drawn in, held to the
/// sticky edges its series register, ended where the theme's autolimit mode says, turned the way it is drawn. The one policy the range
/// of a 2-D axes (<see cref="AxesRangeExtensions"/>), the cube of a 3-D axes, a polar radius (<see cref="RadialScale"/>) and an axis past
/// the primary are worked out by, X and Y alike; <see cref="Range1D"/> is the value it works on and reads no axis itself (its public
/// members that take an axis forward here). The axis' scale rule is picked once, when the policy is made.</summary>
internal readonly struct AxisAutoscale
{
    /// <summary>The five per cent each free side of an axis past the primary is padded by, the margin the primary axes follow on a
    /// linear scale.</summary>
    private const double PastPrimaryMargin = 0.05;

    /// <summary>How far in from 0 and 1 the data limits a 3-D axes seeds X and Y with lie (<c>Axes3D.clear</c>: <c>xymargin = 0.05 *
    /// 10/11</c>, "match mpl3.8 appearance"), so that padded by the default margin they reach 0 and 1.</summary>
    private const double CubeSeedInset = 0.05 * 10 / 11;

    /// <summary>The data limits a 3-D axes seeds X and Y with (<c>mplot3d/axes3d.py</c> 1588-1590).</summary>
    private static readonly Range1D CubeSeedXY = new(CubeSeedInset, 1 - CubeSeedInset);

    /// <summary>The data limits a 3-D axes seeds Z with (<c>Bbox.unit()</c>, <c>mplot3d/axes3d.py</c> 1592).</summary>
    private static readonly Range1D CubeSeedZ = new(0, 1);

    private readonly Axis _axis;
    private readonly ScaleRule _rule;
    private readonly bool _isX;

    /// <summary>Whether the theme follows matplotlib's autoscale (it names an autolimit mode,
    /// <see cref="AutolimitRule.NamedByTheme"/>): a range with no width is then opened as matplotlib's locators open it
    /// (<see cref="Opened"/>).</summary>
    private readonly bool _matplotlibs;

    /// <summary>The policy of <paramref name="axis"/>.</summary>
    /// <param name="axis">The axis: its limits, scale, margin, locator, spacing and orientation.</param>
    /// <param name="isX">Whether the axis is an X axis: only an X axis brings date ticks with its scale.</param>
    /// <param name="theme">The theme the axis is drawn under, whose symlog threshold an axis given none is drawn with.</param>
    internal AxisAutoscale(Axis axis, bool isX, Theme theme)
    {
        _axis = axis;
        _rule = axis.ScaleRuleUnder(theme);
        _isX = isX;
        _matplotlibs = theme.Looks.Range.AutolimitRule.NamedByTheme;
    }

    /// <summary>The seed a running range starts from: a side the axis sets (<see cref="Axis.Min"/> / <see cref="Axis.Max"/>) starts
    /// it, an unset side starts empty. The series fold (<see cref="Range1D.Merge"/>) can widen the seed;
    /// <see cref="WithExplicitLimits"/> then puts every set side back, so a set limit is the range drawn even when data lies beyond
    /// it.</summary>
    internal Range1D Seed => new(_axis.Min ?? double.MaxValue, _axis.Max ?? double.MinValue);

    /// <summary>The range drawn when the axis has explicit limits: every side it sets (by <c>SetXLim</c>/<c>SetYLim</c>, or a zoom or a
    /// pan applied to it) is exactly that value, whatever the data reach (matplotlib's <c>set_xlim</c>). An unset side keeps the value
    /// the data folded into it. When only one side is set and every data point lies beyond it, the free side is put one unit to the
    /// other side of it, so the axis is never inverted by data it does not show.</summary>
    /// <param name="folded">The range the data folded into the seed.</param>
    /// <returns>The range with the axis' set sides in place.</returns>
    internal Range1D WithExplicitLimits(Range1D folded)
    {
        double lo = _axis.Min ?? folded.Lo;
        double hi = _axis.Max ?? folded.Hi;
        if (_axis.Min.HasValue != _axis.Max.HasValue && lo >= hi)
        {
            if (_axis.Min.HasValue) hi = lo + 1;
            else lo = hi - 1;
        }

        return new(lo, hi);
    }

    /// <summary>The range, its set sides in place (<see cref="WithExplicitLimits"/>), opened where it has no width and put on the axis'
    /// scale (<see cref="OnScale"/>): the one place the range of an axis that scales itself is opened. Under the library's own themes by
    /// the library's rule (<see cref="Range1D.Normalized"/>: no data 0 to 1, one value half a unit each way); under the matplotlib themes
    /// as the locator of the axis' scale opens it (<see cref="ScaleRule.Nonsingular"/>, matplotlib's <c>locator.nonsingular</c>, which
    /// <c>autoscale_view</c> asks of the data limits and <c>set_xlim</c> of a limit set at one value), a range set high to low keeping its
    /// turn. A range with an end set at infinity is kept as it is under every theme: the locator reads an end that is not finite as no data
    /// at all, and no matplotlib axes holds such a limit (<c>set_xlim</c> refuses it, "Axis limits cannot be NaN or Inf",
    /// <c>axes/_base.py</c> 3913), so it stays one no rule can measure.</summary>
    /// <param name="range">The range with the set sides in place.</param>
    /// <param name="minPositive">The smallest value above 0 the axis' data reach, which a logit axis puts an end at or past 0 or 1 at;
    /// <see langword="null"/> when none does.</param>
    /// <returns>The opened range on the axis' scale.</returns>
    internal Range1D Opened(Range1D range, double? minPositive)
    {
        if (!_matplotlibs || !range.HasFiniteEnds) return OnScale(range.Normalized(), minPositive);
        var opened = _rule.Nonsingular(range.DataLimits, _isX, minPositive);
        return OnScale(range.IsEmpty || range.IsLopsided ? opened : opened.OrientedLike(range), minPositive);
    }

    /// <summary>The view of an axis whose range holds nothing (no data, no set limit) and that nothing asks to be scaled, under the
    /// matplotlib themes: the scale's first view (<see cref="ScaleRule.FirstView"/>, 0 to 1 on a linear axis), with no margin and no
    /// rounding, as a matplotlib axes keeps the view it was made with until something asks for the autoscale. <see langword="null"/>
    /// under the library's own themes, for a range that holds something, and on a scale that is scaled at once (log, logit).</summary>
    /// <param name="range">The range with the set sides in place.</param>
    /// <returns>The first view, or <see langword="null"/> when the axis is scaled.</returns>
    internal Range1D? FirstViewWhenEmpty(Range1D range) => _matplotlibs && range.IsEmpty ? _rule.FirstView(_isX) : null;

    /// <summary>The range a 3-D axis is padded from: the data it holds (<paramref name="raw"/>) with its set sides in place, opened
    /// where it has no width. Under the matplotlib themes an axis that holds no data holds the data limits <c>Axes3D.clear</c> seeds it
    /// with (X and Y <see cref="CubeSeedInset"/> in from 0 and 1, Z 0 to 1), opened and put on its scale as any axis
    /// (<see cref="Opened"/>); under the library's own themes the range is normalised first (<see cref="Range1D.Normalized"/>).</summary>
    /// <param name="raw">What the series reach along the axis.</param>
    /// <param name="isZ">Whether the axis is the Z axis.</param>
    /// <returns>The unpadded range.</returns>
    internal Range1D UnpaddedInCube(Range1D raw, bool isZ) => _matplotlibs
        ? Opened(WithExplicitLimits(raw.IsEmpty ? isZ ? CubeSeedZ : CubeSeedXY : raw), null)
        : WithExplicitLimits(raw.Normalized());

    /// <summary>The range put on the axis' scale (<see cref="ScaleRule.Limited"/>): a log axis lifted above 0, a logit axis inside 0
    /// and 1, any other as it is.</summary>
    /// <param name="range">The normalised range.</param>
    /// <param name="minPositive">The smallest value above 0 the axis' data reach, which a logit axis puts an end at or past 0 or 1 at;
    /// <see langword="null"/> when none does.</param>
    /// <returns>The range the scale can draw.</returns>
    internal Range1D OnScale(Range1D range, double? minPositive) => _rule.Limited(range, minPositive);

    /// <summary>The range with a floor a log axis has no place for lifted (<see cref="ScaleRule.Lifted"/>); every other scale keeps it.</summary>
    /// <param name="range">The range.</param>
    /// <returns>The lifted range.</returns>
    internal Range1D Lifted(Range1D range) => _rule.Lifted(range);

    /// <summary>The range padded by <paramref name="margin"/> of its width at each free side, in the space the axis is drawn in, as
    /// matplotlib's <c>autoscale_view</c> pads every scale: a margin under a floor of 2 on a log axis is laid in decades, not at -10.4,
    /// one on a logit axis never reaches past 0 or 1, and one on a symmetric-log axis is laid through its threshold, so data from 0.5
    /// is not padded to -24.5. On a linear and a date axis the space is the value itself, so the pad is the width times the margin. A
    /// side the axis sets is left as it is.</summary>
    /// <param name="range">The range.</param>
    /// <param name="margin">The share of the range added at each side.</param>
    /// <returns>The padded range.</returns>
    internal Range1D Padded(Range1D range, double margin)
    {
        double lo = _rule.Forward(range.Lo);
        double hi = _rule.Forward(range.Hi);
        double pad = (hi - lo) * margin;
        return new(
            _axis.Min.HasValue ? range.Lo : _rule.Inverse(lo - pad),
            _axis.Max.HasValue ? range.Hi : _rule.Inverse(hi + pad));
    }

    /// <summary>The sticky-edge clamp of one series' contribution (matplotlib's <c>sticky_edges</c>): when the padding pushed the range
    /// past a sticky edge and the <paramref name="unpadded"/> range was still inside it, the range snaps back to it. A side the axis sets
    /// is left as it is.</summary>
    /// <param name="range">The padded range.</param>
    /// <param name="stickyMin">The low sticky edge, or <see langword="null"/>.</param>
    /// <param name="stickyMax">The high sticky edge, or <see langword="null"/>.</param>
    /// <param name="unpadded">The range before the padding.</param>
    /// <returns>The clamped range.</returns>
    internal Range1D ClampSticky(Range1D range, double? stickyMin, double? stickyMax, Range1D unpadded)
    {
        double lo = range.Lo;
        double hi = range.Hi;
        if (stickyMin.HasValue && lo < stickyMin.Value && unpadded.Lo >= stickyMin.Value && !_axis.Min.HasValue)
            lo = stickyMin.Value;
        if (stickyMax.HasValue && hi > stickyMax.Value && unpadded.Hi <= stickyMax.Value && !_axis.Max.HasValue)
            hi = stickyMax.Value;
        return new(lo, hi);
    }

    /// <summary>Where the free ends of the axis stop under <paramref name="rule"/>, the theme's <c>axes.autolimit_mode</c> picked once from its
    /// look (<see cref="AutolimitRule"/>): the range as it is, rounded out to its outermost ticks (<see cref="RoundedOutToTicks"/>), or, under
    /// a theme that names no mode, the library's own nice bounds (<see cref="ExpandedToNiceBounds"/>). The one rule the range of a 2-D axes
    /// and the cube of a 3-D axes end by.</summary>
    /// <param name="range">The padded, clamped range.</param>
    /// <param name="rule">The theme's rule.</param>
    /// <param name="ladder">The locator the theme places ticks by (<see cref="AxisTickLook.Ladder"/>), whose ends a rounding mode rounds
    /// out to; a theme that rounds without naming one takes the classic locator.</param>
    /// <param name="unpadded">The range before the margin, which the library's nice bounds are taken from.</param>
    /// <param name="hasSticky">Whether a series sets a sticky edge on this axis, which the library's nice bounds stand aside for.</param>
    /// <param name="namesCategories">Whether the axis' ticks name categories: matplotlib places those with a fixed locator, which rounds
    /// nothing, so such an axis keeps its range when its rule rounds.</param>
    /// <returns>The range as the axis ends it.</returns>
    internal Range1D Autolimited(Range1D range, AutolimitRule rule, TickLadder? ladder, Range1D unpadded, bool hasSticky,
        bool namesCategories = false) => rule.Autolimited(this, range, ladder, unpadded, hasSticky, namesCategories);

    /// <summary>The range rounded out to the outermost ticks that hold it, as matplotlib's classic style rounds the limits of a 2-D and a
    /// 3-D axis (<c>axes.autolimit_mode = 'round_numbers'</c>: the major locator's <c>view_limits</c>). An axis with a locator of its own
    /// ends where that locator rounds it out to (<see cref="TickLocatorExtensions"/>' <c>RangeRoundedOut</c>), otherwise where its scale
    /// does (<see cref="ScaleRule.RoundedOut"/>: a linear axis to its spacing or the ladder's ticks, a log or symmetric-log axis to
    /// decades, a logit or a date axis not at all). An axis with a limit of its own and an empty or degenerate range keep their range.</summary>
    /// <param name="range">The range.</param>
    /// <param name="ladder">The locator the theme places the axis' ticks by.</param>
    /// <returns>The rounded range.</returns>
    internal Range1D RoundedOutToTicks(Range1D range, TickLadder ladder)
    {
        if (_axis.Min.HasValue || _axis.Max.HasValue || !(range.Hi > range.Lo) || !double.IsFinite(range.Hi - range.Lo))
            return range;
        return _axis.TickLocator is { } own ? own.RangeRoundedOut(range) : _rule.RoundedOut(range, _axis.MajorTicks.Spacing, ladder);
    }

    /// <summary>Nice-number view-limit expansion (matplotlib's <c>MaxNLocator.view_limits</c>): the bounds rounded OUTWARD to the next nice
    /// tick boundary of <paramref name="unpadded"/>, applied only where that widens the range. Skipped when the axis sets a limit or a
    /// margin of 0, a series sets a sticky edge, the unpadded range is degenerate, or a locator places the ticks: the axis' own or the
    /// one its scale brings (<see cref="ScaleRule.BringsLocator"/>, asked without making it), so the first draw of an axis on a log,
    /// symmetric-log or date X scale is the draw every later one is. The unpadded range is the input: the padded one can bloat
    /// <c>[0,10]</c> to <c>[-2,12]</c>, because the rounding step changes with the width.</summary>
    /// <param name="range">The padded range.</param>
    /// <param name="unpadded">The range before padding.</param>
    /// <param name="hasSticky">Whether a series sets a sticky edge on this axis.</param>
    /// <param name="locator">The locator whose nice bounds widen the range.</param>
    /// <returns>The range, widened to nice bounds or as it was.</returns>
    internal Range1D ExpandedToNiceBounds(Range1D range, Range1D unpadded, bool hasSticky, AutoLocator locator)
    {
        if (hasSticky || _axis.Min.HasValue || _axis.Max.HasValue
            || unpadded.Hi <= unpadded.Lo || _axis.Margin == 0
            || _axis.TickLocator is not null || _rule.BringsLocator(_isX))
            return range;
        var (nLo, nHi) = locator.ExpandToNiceBounds(unpadded.Lo, unpadded.Hi);
        return new(Math.Min(range.Lo, nLo), Math.Max(range.Hi, nHi));
    }

    /// <summary>The range the way the axis is drawn: high to low when the axis is <see cref="Axis.Inverted"/>, otherwise as it is. A
    /// range that already runs high to low (limits set high to low) is left as it is: inverted is a state of the axis, as matplotlib's
    /// <c>set_inverted(True)</c> is, never a second turn, so the two ways of asking for a reversed axis draw it reversed once.</summary>
    /// <param name="range">The range.</param>
    /// <returns>The range in the orientation it is drawn in.</returns>
    internal Range1D Oriented(Range1D range) => _axis.Inverted && range.Lo < range.Hi ? new(range.Hi, range.Lo) : range;

    /// <summary>The range an axis past the primary (a Y axis slot, or the top X axis) draws from what its own series reach: a set limit
    /// exact, a degenerate range opened and put on its scale (<see cref="Opened"/>; an empty one nothing asks for keeps its first view,
    /// <see cref="FirstViewWhenEmpty"/>, as a matplotlib <c>twinx()</c> with nothing on it does), its free sides
    /// padded by five per cent in the space it is drawn in and held to the sticky edges its series set (<see cref="ClampSticky"/>, as the
    /// primary is: matplotlib's twin axes are axes of their own, a bar's base stays at 0), turned the way it is drawn. The one rule both
    /// kinds of axis follow, so an X and a Y cannot disagree.</summary>
    /// <param name="folded">The range the axis' own series folded into its seed.</param>
    /// <param name="minPositive">The smallest value above 0 for a logit axis (<see cref="OnScale"/>).</param>
    /// <param name="contributions">What each of the axis' own series adds, whose sticky edges along the axis hold the padded range.</param>
    /// <param name="asked">Whether a series of the axis asks for it to be scaled (<see cref="AutoscaleAskExtensions"/>).</param>
    /// <returns>The range as drawn.</returns>
    internal Range1D DrawnPastPrimary(Range1D folded, double? minPositive, IReadOnlyList<DataRangeContribution> contributions, bool asked)
    {
        var limited = WithExplicitLimits(folded);
        if (!asked && FirstViewWhenEmpty(limited) is { } first) return Oriented(first);
        var unpadded = Opened(limited, minPositive);
        var drawn = Padded(unpadded, PastPrimaryMargin);
        foreach (var c in contributions)
            drawn = ClampSticky(drawn, c.StickyMinAlong(_isX), c.StickyMaxAlong(_isX), unpadded);
        return Oriented(drawn);
    }
}
