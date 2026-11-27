// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Places the ticks of a base-10 log axis where matplotlib's log scale labels them: its major <c>LogLocator</c> (the powers of ten,
/// every one or, with more decades in view than labels fit, every second, third or more of them) and its minor
/// <c>LogLocator(subs='auto')</c> (2 to 9 times them), as many of them as its <c>LogFormatterSciNotation</c> names.
/// With more than one power of ten in view, the powers; with one or none, the multiples whose
/// coefficient is 1, 2, 3, 4 or 6 as well, and every multiple when the view is narrower than 0.4 of a decade. When the view holds one
/// power or multiple or none, matplotlib's minor locator lays <c>AutoLocator</c>'s evenly spaced ticks instead, and so does this one.
/// Requires min &gt; 0.
/// </summary>
public sealed class LogLocator : ITickLocator, IJsonDescribed<TickLocatorDto>, IRoundingLocator, ITickSpaceLocator, IMinorTickLocator
{
    /// <inheritdoc />
    double[] IMinorTickLocator.MinorsOver(Range1D view, double[] majors, double tickSpace, Theme theme) =>
        MinorsOver(view, majors, tickSpace, theme);

    /// <summary>The coefficients of the multiples of a power of ten a log scale lays (<c>subs</c> 1 for the powers, 2 to 9).</summary>
    private const int Multiples = 9;


    /// <summary>The ticks the locator asks for in matplotlib's classic mode, whatever the length (<c>numticks = 15</c>).</summary>
    private const int ClassicTicksAsked = 15;

    /// <inheritdoc />
    TickLocatorDto IJsonDescribed<TickLocatorDto>.Describe() => new LogTickLocatorDto();

    /// <inheritdoc />
    /// <remarks>Named as matplotlib's default style names them with no axis to ask how many labels fit (nine are asked for); an axis
    /// drawn under a theme names them as that theme does, for as many labels as fit along it.</remarks>
    public double[] Locate(double min, double max)
    {
        if (min <= 0) min = 1e-10;
        if (max <= min) return [min];
        return NamedOver(min, max, LogLabelThresholds.Default, AxisTickRule.Library, TickBudget.Standard.Most);
    }

    /// <inheritdoc />
    double[] ITickSpaceLocator.LocateWithSpace(double min, double max, double tickSpace, Theme theme)
    {
        if (min <= 0) min = 1e-10;
        if (max <= min) return [min];
        return TicksNamedOver(min, max, tickSpace, theme);
    }

    /// <summary>The ticks a log scale names over <paramref name="lo"/> .. <paramref name="hi"/> along an axis with room for
    /// <paramref name="tickSpace"/> labels, under <paramref name="theme"/>: the one rule a log axis and a colour bar of matplotlib's log
    /// norm name their ticks by. The theme gives the label thresholds (<see cref="TickLabelLook.LogThresholds"/>) and its tick rule
    /// (<see cref="AxisTickRule"/>): the <c>AutoLocator</c> that lays the ticks of a view with one power or multiple or none (its steps,
    /// nine bins, and whether it rounds numbers), and the classic mode, which asks for fifteen ticks whatever the length.</summary>
    /// <param name="lo">The low end of the view, above 0.</param>
    /// <param name="hi">The high end of the view.</param>
    /// <param name="tickSpace">How many labels fit along the axis (matplotlib's <c>get_tick_space</c>).</param>
    /// <param name="theme">The theme the scale is drawn under.</param>
    /// <returns>The named ticks in view, ascending.</returns>
    internal static double[] TicksNamedOver(double lo, double hi, double tickSpace, Theme theme)
    {
        var rule = theme.Looks.TickRule;
        return NamedOver(lo, hi, theme.Looks.TickLabels.DrawnLogThresholds, rule,
            rule.RoundNumbers ? ClassicTicksAsked : TicksAsked(tickSpace));
    }

    /// <summary>The ticks a log scale names over <paramref name="lo"/> .. <paramref name="hi"/> under <paramref name="theme"/> along an
    /// axis of any length: those of <see cref="TicksNamedOver"/> for every count of labels it can be told (2 to 9), each once. The room a
    /// colour bar of matplotlib's log norm reserves before it knows its length holds these.</summary>
    /// <param name="lo">The low end of the view, above 0.</param>
    /// <param name="hi">The high end of the view.</param>
    /// <param name="theme">The theme the scale is drawn under.</param>
    /// <returns>The ticks, ascending.</returns>
    internal static double[] TicksNamedAtAnyLength(double lo, double hi, Theme theme)
    {
        var named = new SortedSet<double>();
        foreach (int asked in TickBudget.Standard) named.UnionWith(TicksNamedOver(lo, hi, asked, theme));
        return [.. named];
    }

    /// <summary>How many ticks the default style's locator asks for along an axis with room for <paramref name="tickSpace"/> labels:
    /// <c>np.clip(get_tick_space(), 2, 9)</c>, nine with no axis to ask (<see cref="TickBudget.Standard"/>).</summary>
    private static int TicksAsked(double tickSpace) => TickBudget.Standard.Clamp(tickSpace);

    /// <summary>matplotlib 3.11.2's <c>LogLocator.tick_values</c> (major and <c>subs='auto'</c> minor) with
    /// <c>LogFormatter.set_locs</c> and <c>__call__</c>: with more powers of ten in view than the thresholds name multiples for, the
    /// powers the major locator lays in view (<see cref="DecadesLaid"/>, which strides over decades when more stand in view than
    /// <paramref name="ticksAsked"/>); otherwise the powers and their multiples 2 to 9 in view (a hair's tolerance at either end), or,
    /// when they are one or none, <c>AutoLocator().tick_values</c> in view; of those, each whose coefficient rounded half to even
    /// (<c>round(b ** (fx - exponent))</c>, the exponent the power at or below) is one the thresholds name.</summary>
    private static double[] NamedOver(double lo, double hi, LogLabelThresholds thresholds, AxisTickRule rule, int ticksAsked)
    {
        double lmin = Math.Log10(lo), lmax = Math.Log10(hi);
        int powers = LogLabelThresholds.PowersIn(lmin, lmax);
        int named = thresholds.NamedOver(lo, hi);
        double low = lo * (1 - 1e-12), high = hi * (1 + 1e-12);

        // Two powers or more in view and the powers alone named: the major locator's, every one or strided.
        if (named == 1 && powers >= 2) return DecadesLaid(lo, hi, ticksAsked, rule.RoundNumbers).PowersBetween(low, high);

        var ticks = new List<double>();
        int inView = 0;
        for (int exponent = (int)Math.Floor(lmin) - 1; exponent <= (int)Math.Ceiling(lmax); exponent++)
        {
            double power = Math.Pow(10, exponent);
            for (int coefficient = 1; coefficient <= Multiples; coefficient++)
            {
                double value = coefficient * power;
                if (value < low || value > high) continue;
                inView++;
                if (LogLabelThresholds.Names(coefficient, named)) ticks.Add(value);
            }
        }

        if (inView > 1) return [.. ticks];

        // matplotlib's minor locator: "If we're a minor locator *that expects at least two ticks per decade* ... and there's no more
        // than one major or minor tick, switch to AutoLocator." Its formatter names what it lays by the rounded coefficient, worked out
        // as LogFormatter.__call__ works it out (LogLabelThresholds.CoefficientOf).
        ticks.Clear();
        foreach (double value in rule.FallbackLadder.TicksIn(new Range1D(lo, hi), rule.RoundNumbers))
            if (LogLabelThresholds.Names(LogLabelThresholds.CoefficientOf(value), named)) ticks.Add(value);

        return [.. ticks];
    }

    /// <summary>The minor ticks a log scale marks over <paramref name="view"/> under <paramref name="theme"/>, as matplotlib 3.11.2's
    /// minor <c>LogLocator(subs='auto').tick_values</c> lays them, the ones in view, less those a major tick stands on
    /// (<c>remove_overlapping_locs</c>): 2 to 9 times each power from the one below the view on; none when ten or more powers stand in
    /// view (<c>n_avail &gt;= 10</c>) or when the major ticks stride over decades (under the default style
    /// <c>n_avail // (n_request + 1) + 1</c> above 1 with more than one power in view, <c>n_request</c> the tick space clipped to 2 .. 9;
    /// the classic mode asks for 15 ticks, so below ten powers it never strides); and, when the view holds no more than one major and
    /// minor tick together, the ticks <c>AutoLocator</c> lays, the theme's fallback a log axis names by too. The minor tick marks and
    /// the minor grid of a log axis and the minor marks of a colour bar of matplotlib's log norm all stand on these.</summary>
    /// <param name="view">The ends of the view, whichever way it runs; a view that reaches 0 or below, or past a finite number, has
    /// none.</param>
    /// <param name="majors">The major ticks, left out.</param>
    /// <param name="tickSpace">How many labels fit along the axis (matplotlib's <c>get_tick_space</c>).</param>
    /// <param name="theme">The theme the scale is drawn under.</param>
    /// <returns>The minor ticks in view, ascending.</returns>
    internal static double[] MinorsOver(Range1D view, double[] majors, double tickSpace, Theme theme)
    {
        var interval = view.Interval;
        double lo = interval.Lo, hi = interval.Hi;
        if (!(lo > 0) || !double.IsFinite(hi)) return [];

        double efmin = Math.Log10(lo), efmax = Math.Log10(hi);
        int emin = (int)Math.Ceiling(efmin), emax = (int)Math.Floor(efmax), available = emax - emin + 1;
        if (available >= 10) return [];

        // "if stride == 1 or n_avail <= 1", the stride of the major locator's decades (the classic mode asks for 15 ticks, whose
        // stride is 1 below ten powers).
        var rule = theme.Looks.TickRule;
        bool classic = rule.RoundNumbers;
        if (available > 1 && DecadesLaid(lo, hi, classic ? ClassicTicksAsked : TicksAsked(tickSpace), classic).Stride != 1) return [];

        double low = lo * (1 - 1e-12), high = hi * (1 + 1e-12);
        var minors = new List<double>();
        int inView = 0;
        for (int exponent = emin - 1; exponent <= emax; exponent++)
        {
            double power = Math.Pow(10, exponent);
            for (int coefficient = 2; coefficient <= Multiples; coefficient++)
            {
                double value = coefficient * power;
                if (value < low || value > high) continue;
                inView++;
                if (!IsAMajor(value, majors)) minors.Add(value);
            }
        }

        if (available + inView > 1) return [.. minors];

        // "If we're a minor locator *that expects at least two ticks per decade* ... and there's no more than one major or minor
        // tick, switch to AutoLocator."
        return [.. rule.FallbackLadder.TicksIn(new Range1D(lo, hi), classic).Where(tick => !IsAMajor(tick, majors))];
    }

    /// <summary>The decades matplotlib 3.11.2's major <c>LogLocator(numticks=ticksAsked).tick_values</c> lays over
    /// <paramref name="lo"/> .. <paramref name="hi"/> (base 10, <c>subs=(1.0,)</c>), with the decade outside each end that a contour's
    /// <c>_autolev</c> relies on: the one rule a log axis, a colour bar of matplotlib's log norm and the levels of a log-normalised contour
    /// lay their powers of ten by. Under the default style "<c>stride = n_avail // (n_request + 1) + 1</c>", the largest number of ticks
    /// no more than asked for that can be drawn, then "the largest stride that yields this actual n_request" and an offset that puts the
    /// ticks on multiples of the stride if it can, else the smallest one allowed; a single power in view stands alone with one past each
    /// side, and none in view leaves the two just outside. In classic mode (<paramref name="classic"/>) the older stride
    /// <c>max(ceil((n_avail - 1) / (numticks - 1)), 1)</c> from <c>emin - stride</c>, where matplotlib raises for a single tick and two
    /// are taken here.</summary>
    /// <param name="lo">The low end, above 0.</param>
    /// <param name="hi">The high end, at or above <paramref name="lo"/>.</param>
    /// <param name="ticksAsked">How many ticks are asked for (matplotlib's <c>n_request</c>); at least 1.</param>
    /// <param name="classic">Whether matplotlib's classic mode lays them.</param>
    /// <returns>The run of exponents.</returns>
    internal static DecadeRun DecadesLaid(double lo, double hi, int ticksAsked, bool classic)
    {
        double efmin = Math.Log10(lo), efmax = Math.Log10(hi);
        int emin = (int)Math.Ceiling(efmin), emax = (int)Math.Floor(efmax), available = emax - emin + 1;

        if (classic)
        {
            int stride = Math.Max((int)Math.Ceiling((available - 1.0) / (Math.Max(2, ticksAsked) - 1)), 1);
            return DecadeRun.From(emin - stride, emax + stride + 1, stride);
        }

        // The largest number of ticks, no more than asked for, that can be drawn (no stride yields 7 ticks of 9 decades).
        int requested = ticksAsked, first = available / (requested + 1) + 1;
        int actual = (available + first - 1) / first;
        if (actual <= requested) requested = actual;

        // No tick in bounds: two just outside.
        if (requested == 0) return new(emin - 1, emax + 1 - (emin - 1), 2);

        if (requested == 1)
        {
            // A single tick close to the centre.
            int middle = (int)Math.Round((efmin + efmax) / 2);
            int apart = Math.Max(middle - (emin - 1), emax + 1 - middle);
            return new(middle - apart, apart, 3);
        }

        // The largest stride that yields this many ticks, then an offset that puts them on multiples of the stride if it can.
        int strideOf = (available - 1) / (requested - 1);
        if (strideOf < (double)available / requested) strideOf = available / requested;
        int offsetLow = Math.Max(available - strideOf * requested, 0), offsetHigh = Math.Min(available - strideOf * (requested - 1), strideOf);
        int offset = ((-emin % strideOf) + strideOf) % strideOf;
        if (!(offsetLow <= offset && offset < offsetHigh)) offset = offsetLow;
        return DecadeRun.From(emin + offset - strideOf, emax + strideOf + 1, strideOf);
    }

    /// <inheritdoc />
    bool ITickSpaceLocator.NamesAsMinor(double tick, Range1D view) => NamesAsMinor(tick);

    /// <summary>Whether a tick a log scale names (<see cref="TicksNamedOver"/>) is one of its minor ticks with a label: every tick off a
    /// power of ten. matplotlib's major <c>LogLocator</c> (<c>subs=(1.0,)</c>) lays powers alone; a multiple, and a tick its minor locator
    /// lays when it switches to <c>AutoLocator</c>, is its minor <c>LogLocator(subs='auto')</c>'s, labelled by the formatter (printed for
    /// ylim (2, 80): majors [10.0], the minors 2, 3, 4, 6, 20, 30, 40, 60 labelled). The one rule a log axis and a colour bar of
    /// matplotlib's log norm tell a labelled minor from a major by.</summary>
    /// <param name="tick">A named tick, above 0.</param>
    /// <returns><see langword="true"/> when it is off a power of ten.</returns>
    internal static bool NamesAsMinor(double tick) => Math.Abs(tick / tick.NearestDecade() - 1) > 1e-9;

    /// <summary>Whether a major tick stands on <paramref name="value"/>, to a billionth of it.</summary>
    private static bool IsAMajor(double value, double[] majors)
    {
        foreach (double major in majors)
            if (Math.Abs(value - major) <= 1e-9 * Math.Abs(major)) return true;
        return false;
    }

    /// <inheritdoc />
    Range1D IRoundingLocator.RoundedOut(Range1D range) => DecadesHolding(range);

    /// <summary>The powers of ten at or beyond the ends of <paramref name="range"/>, as matplotlib's <c>LogLocator.view_limits</c>
    /// rounds under <c>round_numbers</c> (<c>_decade_less_equal</c>, <c>_decade_greater_equal</c>): the one rule a log axis is rounded
    /// out by, whether it names this locator or its scale brings it. A range that reaches 0 or below has no decade and is kept.</summary>
    /// <param name="range">The range, its low end below its high end.</param>
    /// <returns>The rounded range.</returns>
    internal static Range1D DecadesHolding(Range1D range) => range.Lo > 0
        ? new(range.Lo.DecadeAtOrBelow(), range.Hi.DecadeAtOrAbove())
        : range;
}
