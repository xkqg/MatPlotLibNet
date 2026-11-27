// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The radial scale of one polar axes: the radii it runs between, the radius at its centre and which rings it draws. The
/// grid and every series and patch on the axes read it, so a ring labelled 10 stands exactly where a radius of 10 is drawn, whichever
/// series draws it, and two series share their scale. The ends are the limits the axes was given, exactly, or the ones worked out
/// from what the series (and the patches that range it) reach (<see cref="RangeOf"/>); a radius below zero is drawn when the scale
/// reaches it, as matplotlib's polar axes draws it.</summary>
/// <param name="Extent">The radius at the outermost ring (matplotlib's <c>rmax</c>).</param>
/// <param name="Rings">The radii of the rings, in ascending order; none at the centre.</param>
/// <param name="Labels">The text of each ring, in one number format, beside <paramref name="Rings"/>.</param>
/// <param name="Feet">For a stack of polar bars (<see cref="BarMode.Stacked"/>), where each bar series of the axes starts, wedge
/// by wedge: the sum of the bars before it, aligned by index (the wedges of one angle are the same index of every series);
/// <see langword="null"/> when the bars are not stacked. Worked out here, read by the bar renderer, and never written into the
/// series, so a stacked render leaves nothing in the figure or its JSON.</param>
internal readonly record struct RadialScale(double Extent, double[] Rings, string[] Labels, IReadOnlyDictionary<PolarBarSeries, double[]>? Feet = null)
{
    /// <summary>The radius at the low end of the scale (matplotlib's <c>rmin</c>): 0 for a scale that does not reach below zero.</summary>
    internal double Min { get; init; }

    /// <summary>The radius drawn at the centre (matplotlib's <c>rorigin</c>): <see cref="Min"/> unless the axes was given an origin of
    /// its own (<see cref="Axes.ROrigin"/>); an origin below the low end leaves a hole at the centre.</summary>
    internal double Origin { get; init; }

    /// <summary>Where <paramref name="bar"/> starts, wedge by wedge, when the bars are stacked; <see langword="null"/>
    /// when they are not.</summary>
    /// <param name="bar">A bar series of the axes.</param>
    internal double[]? FootOf(PolarBarSeries bar) => Feet is not null && Feet.TryGetValue(bar, out var feet) ? feet : null;

    /// <summary>The angle, from the right-hand spoke and counter-clockwise, at which the rings are labelled (matplotlib's
    /// <c>rlabel_position</c>), clear of the spokes the bars of a wind rose stand on.</summary>
    internal const double LabelAngleDegrees = 22.5;

    /// <summary>The margin the farthest mark leaves before the outer ring under the library's own themes.</summary>
    private const double Margin = 1.05;

    /// <summary>The radii the scale of <paramref name="series"/> runs between, low end first: a limit the radial axis was given is
    /// that end exactly (a low end above the high end runs the scale from the outside in, matplotlib's <c>set_ylim(10, 0)</c>);
    /// the other end is worked out from what every series reaches, hidden ones too (hiding a series does not move the rings).
    /// <para>Under the library's own themes the span from the lower of 0 and the nearest radius to the farthest one is padded 5 %
    /// both ways, the low end staying at 0 when no radius falls below it and the high end at 0 when every radius does; a series
    /// whose cells end at a radius of their own (a polar heatmap) reaches at least that far without the margin; 0 to 1 when
    /// nothing reaches anywhere.</para>
    /// <para>Under the matplotlib themes the scale is ranged as matplotlib's polar axes ranges its radial axis: the data's span
    /// widened when it has none (<c>nonsingular</c>), padded by the axis' or the theme's margin (a classic scatter's included),
    /// held at 0 when the padding would cross it (the radial axis' sticky edge) and at a heatmap's own ends, rounded out to the
    /// ticks under the classic theme, and its low end taken at 0 at most (<c>RadialLocator.view_limits</c>).</para></summary>
    /// <param name="series">The series of the axes.</param>
    /// <param name="axis">The axes' radial axis: its limits, its margin and its locator.</param>
    /// <param name="theme">The theme the axes is drawn under (<see cref="Theme.Default"/>: the library's own rule).</param>
    /// <param name="scatterMargin">The least margin the theme pads by because the axes holds a scatter
    /// (<see cref="AxesRangeExtensions"/>' <c>ScatterMarginIn</c>).</param>
    /// <param name="stackTop">How high the stack of polar bars reaches, <see langword="null"/> when they are not stacked: the bars
    /// of a stack reach from 0 to its top, and their own radii (a radius below zero adds nothing to a stack) do not count.</param>
    /// <param name="patches">The radii the patches of the axes reach where they range the radius; <see langword="null"/> when none
    /// does.</param>
    /// <returns>The low end (<c>Lo</c>) and the extent (<c>Hi</c>).</returns>
    internal static Range1D RangeOf(IReadOnlyList<ISeries> series, Axis axis, Theme theme, double scatterMargin = 0,
        double? stackTop = null, RadialExtent? patches = null)
    {
        double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
        double stickyLo = double.PositiveInfinity, stickyHi = double.NegativeInfinity;
        foreach (var one in series)
        {
            if (one is not ChartSeries { RadialExtent: { } reach } || (stackTop is not null && one is PolarBarSeries)) continue;
            if (reach.Sticky) { stickyLo = Math.Min(stickyLo, reach.Min); stickyHi = Math.Max(stickyHi, reach.Max); }
            else { lo = Math.Min(lo, reach.Min); hi = Math.Max(hi, reach.Max); }
        }
        if (stackTop is { } top) { lo = Math.Min(lo, 0); hi = Math.Max(hi, top); }
        if (patches is { } reached) { lo = Math.Min(lo, reached.Min); hi = Math.Max(hi, reached.Max); }

        var rule = theme.Looks.Range.AutolimitRule;
        var worked = rule.NamedByTheme
            ? MatplotlibRange(new Range1D(Math.Min(lo, stickyLo), Math.Max(hi, stickyHi)), stickyLo, stickyHi, axis, theme, rule, scatterMargin)
            : LibraryRange(lo, hi, stickyHi);
        double min = axis.Min is { } low && double.IsFinite(low) ? low : worked.Lo;
        double max = axis.Max is { } high && double.IsFinite(high) ? high : worked.Hi;
        return min != max ? new(min, max) : worked;
    }

    /// <summary>The library's own rule (see <see cref="RangeOf"/>). A scale that does not reach below zero keeps the bits it always
    /// had: the farthest radius times 1.05.</summary>
    private static Range1D LibraryRange(double lo, double hi, double stickyHi)
    {
        double low = 0, high = 0;
        if (lo <= hi)
        {
            low = Math.Min(lo, 0);
            double span = hi - low;
            if (span == 0) span = Math.Abs(hi);
            high = low == 0 ? hi * Margin : hi + span * (Margin - 1);
            if (low < 0) low -= span * (Margin - 1);
            if (hi <= 0 && high > 0) high = 0;
        }

        high = Math.Max(high, stickyHi);
        return high > low ? new(low, high) : new(0, 1);
    }

    /// <summary>matplotlib's rule (see <see cref="RangeOf"/>), over the data's span <paramref name="data"/>.</summary>
    private static Range1D MatplotlibRange(Range1D data, double stickyLo, double stickyHi, Axis axis, Theme theme, AutolimitRule rule,
        double scatterMargin)
    {
        // A polar axes with nothing on it starts at (0, 1), as matplotlib's RadialLocator.nonsingular does.
        if (!(data.Lo <= data.Hi)) return new(0, 1);

        // matplotlib's nonsingular(vmin, vmax, expander=0.05): a span of no width is widened by 5 % of its ends, and one at 0 to
        // (-0.05, 0.05).
        var unpadded = data.NonsingularBy(0.05, 1e-15);
        var auto = new AxisAutoscale(axis, isX: false, theme);
        var range = auto.ClampSticky(auto.Padded(unpadded, axis.Margin ?? Math.Max(theme.AxisYMargin, scatterMargin)), 0, 0, unpadded);
        if (stickyLo <= stickyHi) range = auto.ClampSticky(range, stickyLo, stickyHi, unpadded);
        range = auto.Autolimited(range, rule, theme.Looks.Ticks.Ladder, unpadded, hasSticky: false);
        return new(Math.Min(0, range.Lo), range.Hi);
    }

    /// <summary>The scale a series makes alone, with no axes around it, for a renderer that is handed none: its range and no
    /// rings.</summary>
    /// <param name="series">The series.</param>
    /// <param name="theme">The theme it is drawn under.</param>
    /// <returns>The scale of the series.</returns>
    internal static RadialScale Alone(ISeries series, Theme theme)
    {
        var range = RangeOf([series], new Axis(), theme);
        return new(range.Hi, [], []) { Min = range.Lo, Origin = range.Lo };
    }

    /// <summary>The scale of an axes.</summary>
    /// <param name="series">The series of the axes.</param>
    /// <param name="axis">The axes' radial axis: its limits, its tick locator, its major tick spacing and its tick formatter. A locator
    /// it was given places the rings, as matplotlib's polar axes rings itself at the ticks of its radial axis' locator, and so does a
    /// spacing, as it places the ticks of a 2-D axis.</param>
    /// <param name="maxRadiusPx">The radius of the outermost ring in pixels: with no locator of the axis' own, the rings ask for as
    /// many ticks as fit along it (along the circle's height under the matplotlib themes, whose locator lays them).</param>
    /// <param name="tickFontPx">The size of the tick font.</param>
    /// <param name="barMode">How the bars of the axes are placed: stacked bars stand on one another and the scale reaches the
    /// top of the stack.</param>
    /// <param name="patches">The radii the patches of the axes reach where they range the radius; <see langword="null"/> when none
    /// does.</param>
    /// <param name="theme">The theme the axes is drawn under (see <see cref="RangeOf"/>); its minus sign writes a ring below zero.</param>
    /// <param name="scatterMargin">The least margin the theme pads by because the axes holds a scatter.</param>
    /// <param name="origin">The radius the axes was told to draw at its centre (<see cref="Axes.ROrigin"/>), or
    /// <see langword="null"/> for the low end.</param>
    /// <returns>The scale of the axes.</returns>
    internal static RadialScale For(IReadOnlyList<ISeries> series, Axis axis, double maxRadiusPx, double tickFontPx, Theme theme,
        BarMode barMode = BarMode.Grouped, RadialExtent? patches = null, double scatterMargin = 0, double? origin = null)
    {
        var stack = barMode == BarMode.Stacked ? StackOf(series) : null;
        var range = RangeOf(series, axis, theme, scatterMargin, stack?.Top, patches);
        return Over(range, axis, maxRadiusPx, tickFontPx, theme, origin, stack?.Feet);
    }

    /// <summary>The scale that runs over <paramref name="range"/>: the rings a polar axes draws on it, as matplotlib's polar axes places
    /// them. A radar chart, which has no axes of its own to range, takes its rings from here too.</summary>
    /// <param name="range">The radii the scale runs between, low end first.</param>
    /// <param name="axis">The radial axis whose locator or major tick spacing places the rings and whose formatter writes them;
    /// <see langword="null"/> for none.</param>
    /// <param name="maxRadiusPx">The radius of the outermost ring in pixels.</param>
    /// <param name="tickFontPx">The size of the tick font.</param>
    /// <param name="theme">The theme the scale is drawn under; its minus sign writes a ring below zero.</param>
    /// <param name="origin">The radius drawn at the centre, or <see langword="null"/> for the low end.</param>
    /// <param name="feet">Where each bar series of a stack starts (<see cref="Feet"/>); <see langword="null"/> when the bars are not
    /// stacked.</param>
    /// <returns>The scale over the range.</returns>
    internal static RadialScale Over(Range1D range, Axis? axis, double maxRadiusPx, double tickFontPx, Theme theme, double? origin = null,
        IReadOnlyDictionary<PolarBarSeries, double[]>? feet = null)
    {
        double extent = range.Hi, centre = origin ?? range.Lo;
        var interval = range.Interval;
        // The rings are the ticks the radial axis' request names (a ruler): a locator of its own told how many labels fit along the
        // circle's height (the radial axis is matplotlib's YAxis, and its polar axes rings itself with that axis' AutoLocator under the
        // matplotlib themes, the one rule the classic scale was rounded out by), else its spacing, else the theme's ladder, else the
        // library's ruler sized to the radius.
        double[] ticks = new TickRequest(range, 2 * maxRadiusPx, tickFontPx, AlongX: false, theme, TickAxisKind.Ruler)
        {
            Given = axis?.TickLocator,
            Spacing = axis?.MajorTicks.Spacing,
            LibraryLengthPx = maxRadiusPx,
        }.Majors();
        // A ring stands inside the scale and off the centre: matplotlib's RadialLocator keeps the ticks past the origin.
        double[] rings = [.. ticks.Where(ring => ring >= interval.Lo && ring <= interval.Hi && (ring - centre) / (extent - centre) > 0)];
        string[] labels = axis?.TickFormatter is { } formatter ? [.. rings.Select(formatter.Format)] : theme.Looks.TickLabels.Labelled(rings);
        return new(extent, rings, labels, feet) { Min = range.Lo, Origin = centre };
    }

    /// <summary>The stack of the bar series of the axes: where each stands and how high the stack reaches. A radius that is
    /// not drawn (missing, infinite or below zero) adds nothing. One bar series is not a stack.</summary>
    private static PolarStack? StackOf(IReadOnlyList<ISeries> series)
    {
        var bars = series.OfType<PolarBarSeries>().ToList();
        if (bars.Count < 2) return null;

        double[][] layers = [.. bars.Select(bar => bar.R.Select(radius => radius.IsPositiveFinite() ? radius : 0).ToArray())];
        var stack = layers.Stack();
        return new PolarStack(bars.Zip(stack.Feet).ToDictionary(pair => pair.First, pair => pair.Second),
            stack.Ends.SelectMany(end => end).DefaultIfEmpty(0).Max());
    }

    /// <summary>The stack of the polar bars of an axes: where each bar series starts, and how high the stack reaches.</summary>
    private readonly record struct PolarStack(IReadOnlyDictionary<PolarBarSeries, double[]> Feet, double Top);
}
