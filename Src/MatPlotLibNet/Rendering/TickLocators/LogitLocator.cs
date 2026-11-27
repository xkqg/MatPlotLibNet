// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Places the ticks of a logit axis as matplotlib's logit scale does: its major <c>LogitLocator</c> (…, 0.01, 0.1, ½,
/// 0.9, 0.99, …, every other one or fewer when more of them are in view than labels fit), the minor ticks its labels name folded in
/// (its <c>LogitFormatter(minor=True)</c> names up to six of them when there are fewer than 25), and, in a view too narrow for two
/// of those ticks, <c>MaxNLocator</c>'s ticks with steps of 1, 2, 5 and 10. The minor ticks a logit axis draws without labels
/// (<see cref="MinorsOver"/>) come from its minor <c>LogitLocator</c>. The locator a logit axis is drawn with when it names none;
/// it is never written onto the axis.</summary>
internal sealed class LogitLocator : ITickSpaceLocator, IMinorTickLocator
{
    /// <summary>How many labels <c>LogitLocator</c> takes to fit when it is not told: <c>nbins = 9</c> with no axis.</summary>
    private const double TickSpaceWithoutAnAxis = 9;

    /// <summary>The smallest value a logit axis is taken to reach when nothing says how close to 0 it comes
    /// (<c>LogitLocator.nonsingular</c>' <c>standard_minpos</c>).</summary>
    internal const double StandardMinPos = 1e-7;

    /// <summary>The most minor ticks among which some are named (<c>LogitFormatter</c>'s <c>minor_threshold</c>).</summary>
    private const int MinorThreshold = 25;

    /// <summary>How many minor ticks are named when some are (<c>LogitFormatter</c>'s <c>minor_number</c>).</summary>
    private const int MinorNumber = 6;

    /// <summary>The ladder of a view too narrow for two ideal ticks, laid as the room allows (<see cref="TickLadder.ForTickSpace"/>):
    /// <c>LogitLocator.tick_values</c>' <c>MaxNLocator(nbins='auto', steps=[1, 2, 5, 10])</c>, <c>min_n_ticks</c> 2.</summary>
    private static readonly TickLadder Zoomed = new([1, 2, 5, 10], TickBudget.Standard.Most, 2, BinsByLength: true);

    /// <inheritdoc />
    public double[] Locate(double min, double max) => LocateWithSpace(min, max, TickSpaceWithoutAnAxis, Theme.Default);

    /// <inheritdoc />
    public double[] LocateWithSpace(double min, double max, double tickSpace, Theme theme)
    {
        var (lo, hi) = NonsingularOver(new Range1D(min, max), StandardMinPos);
        int nbins = Bins(tickSpace);
        var ideals = new IdealRun(lo, hi);
        if (ideals.Count - 2 < 2)
        {
            // "the scale is zoomed so same ticks as linear scale can be used": MaxNLocator(nbins='auto', steps=[1, 2, 5, 10]).
            return Zoomed.ForTickSpace(tickSpace).TicksOver(new Range1D(lo, hi), onThreeDAxis: false, theme.Looks.TickRule.RoundNumbers);
        }

        var majors = ideals.Majors(nbins);
        var minors = ideals.Minors(nbins);
        var named = NamedMinors(minors);
        if (named.Count == 0) return majors;
        var all = new double[majors.Length + named.Count];
        majors.CopyTo(all, 0);
        named.CopyTo(all, majors.Length);
        Array.Sort(all);
        return all;
    }

    /// <inheritdoc />
    /// <remarks>The minor ticks of <c>LogitLocator(minor=True)</c> over the view, for the same count of labels the major locator was
    /// told: the ideal ticks the major locator skips when it takes every other one or fewer, or else the multiples between two ideal
    /// ticks (2 to 9 times a power of ten below 0.1, 0.2 to 0.4 and 0.6 to 0.8 about ½, and their mirrors above 0.9); none in a view too
    /// narrow for two ideal ticks. The ones the labels name stand among <paramref name="majors"/> and are left out here.</remarks>
    public double[] MinorsOver(Range1D view, double[] majors, double tickSpace, Theme theme)
    {
        var (lo, hi) = NonsingularOver(view, StandardMinPos);
        var ideals = new IdealRun(lo, hi);
        if (ideals.Count - 2 < 2) return [];
        return [.. ideals.Minors(Bins(tickSpace)).Where(m => Array.IndexOf(majors, m) < 0)];
    }

    /// <inheritdoc />
    /// <remarks>A tick off the ideal ones (a power of ten, ½, one less a power of ten) in a view wide enough for four ideal ticks: the
    /// minor ticks <c>LogitFormatter(minor=True)</c> names (printed for ylim (0.05, 0.95): majors 0.1, ½, 0.9, labelled minors 0.2, 0.3,
    /// 0.4, 0.6, 0.7, 0.8). In a narrower view <c>MaxNLocator</c> lays every tick as a major (ylim (0.3, 0.7): no labelled minor).</remarks>
    public bool NamesAsMinor(double tick, Range1D view)
    {
        var (lo, hi) = NonsingularOver(view, StandardMinPos);
        return new IdealRun(lo, hi).Count - 2 >= 2 && !LogitFormatter.IsIdeal(tick);
    }

    /// <summary>How many labels the locator lays at most for <paramref name="tickSpace"/> labels that fit: at least two, every one
    /// when nothing bounds them (<see cref="TickBudget.Logit"/>).</summary>
    private static int Bins(double tickSpace) => TickBudget.Logit.Clamp(tickSpace);

    /// <summary><c>LogitLocator.nonsingular</c> (matplotlib 3.11.2, <c>ticker.py</c> 2991-3024): the ends in order; no data at all (an end
    /// that is not finite), or data wholly at or below 0 or at or above 1, the standard range <c>(1e-7, 1 - 1e-7)</c>; an end at or past 0
    /// taken to <paramref name="minPositive"/> and one at or past 1 to one less it; a range of no width opened (<see cref="OpenedAround"/>).
    /// The one rule the ticks of a logit axis and the range it scales itself to (<c>ScaleRule</c>) open a range by.</summary>
    /// <param name="view">The range.</param>
    /// <param name="minPositive">The smallest value above 0 the axis reaches (matplotlib's <c>axis.get_minpos()</c>); the locator, which
    /// is handed no axis, takes <see cref="StandardMinPos"/>.</param>
    /// <returns>The range opened.</returns>
    internal static Range1D NonsingularOver(Range1D view, double minPositive)
    {
        var (lo, hi) = view.Interval;
        if (!double.IsFinite(lo) || !double.IsFinite(hi) || hi <= 0 || lo >= 1) return new(StandardMinPos, 1 - StandardMinPos);
        if (lo <= 0) lo = minPositive;
        if (hi >= 1) hi = 1 - minPositive;
        return lo == hi ? OpenedAround(lo) : new(lo, hi);
    }

    /// <summary>A logit range of no width at <paramref name="lo"/> opened to <c>0.1 * lo</c> .. <c>1 - 0.1 * lo</c>
    /// (<c>LogitLocator.nonsingular</c>' last arm): the one place it is written, which <see cref="NonsingularOver"/> and the range a logit
    /// scale can draw (<c>ScaleRule</c>' <c>Limited</c>) read.</summary>
    /// <param name="lo">The value the range stands at.</param>
    /// <returns>The opened range.</returns>
    internal static Range1D OpenedAround(double lo) => new(0.1 * lo, 1 - 0.1 * lo);

    /// <summary>The minor ticks <c>LogitFormatter(minor=True).set_locs</c> names among <paramref name="minors"/>: none when every one
    /// is an ideal tick or there are 25 or more; otherwise the six with the most room on their tighter side in the logit space (and,
    /// on a tie, on both sides together), as its stable sort picks them. Its rule for fewer than six (name them all) has no case
    /// here: four ideal ticks or more, which it takes to name minor ticks, always have fourteen multiples or more between them.</summary>
    private static List<double> NamedMinors(double[] minors)
    {
        var named = new List<double>();
        if (minors.Length >= MinorThreshold || minors.All(LogitFormatter.IsIdeal)) return named;

        int n = minors.Length;
        var diff = new double[n - 1];
        for (int i = 0; i < n - 1; i++) diff[i] = -Math.Log(1 / minors[i + 1] - 1) - -Math.Log(1 / minors[i] - 1);
        var order = new int[n];
        for (int i = 0; i < n; i++) order[i] = i;
        double Pessimistic(int i) => Math.Min(i == 0 ? double.PositiveInfinity : diff[i - 1], i == n - 1 ? double.PositiveInfinity : diff[i]);
        double Sum(int i) => (i == 0 ? 0 : diff[i - 1]) + (i == n - 1 ? 0 : diff[i]);
        // Python's sorted is stable: ties keep the order of the ticks, and the last six are taken.
        var sorted = order.OrderBy(Pessimistic).ThenBy(Sum).ToArray();
        for (int i = n - MinorNumber; i < n; i++) named.Add(minors[sorted[i]]);
        return named;
    }

    /// <summary>The ideal ticks of a logit view and their indices (<c>tick_values</c>' <c>binf</c> .. <c>bsup</c>):
    /// … 10⁻² 10⁻¹ ½ 1−10⁻¹ 1−10⁻² … at … −2, −1, 0, 1, 2 ….</summary>
    private readonly struct IdealRun
    {
        internal IdealRun(double vmin, double vmax)
        {
            Low = (int)(vmin < 0.5 ? vmin.DecadeExponentAtOrBelow() : vmin < 0.9 ? 0 : -(1 - vmin).DecadeExponentAtOrAbove());
            High = (int)(vmax <= 0.5 ? vmax.DecadeExponentAtOrAbove() : vmax <= 0.9 ? 1 : -(1 - vmax).DecadeExponentAtOrBelow());
        }

        /// <summary><c>binf</c>.</summary>
        internal int Low { get; }

        /// <summary><c>bsup</c>.</summary>
        internal int High { get; }

        /// <summary>How many ideal ticks there are from <see cref="Low"/> to <see cref="High"/>; <c>numideal</c> is two fewer.</summary>
        internal int Count => High - Low + 1;

        /// <summary><c>ideal_ticks(b)</c>: <c>10 ** b</c> below 0, ½ at 0, <c>1 - 10 ** -b</c> above.</summary>
        internal static double Tick(int b) => b < 0 ? Math.Pow(10, b) : b > 0 ? 1 - Math.Pow(10, -b) : 0.5;

        /// <summary>The major ticks: every ideal tick, or every <c>ceil(numideal / nbins)</c>-th when there are more than
        /// <paramref name="nbins"/>.</summary>
        internal double[] Majors(int nbins)
        {
            int numideal = Count - 2;
            if (numideal <= nbins) return [.. Enumerable.Range(Low, Count).Select(Tick)];
            int factor = (int)Math.Ceiling((double)numideal / nbins);
            return [.. Enumerable.Range(Low, Count).Where(b => b % factor == 0).Select(Tick)];
        }

        /// <summary>The ideal ticks a subsampling by <paramref name="factor"/> skips.</summary>
        internal double[] Skipped(int factor) => [.. Enumerable.Range(Low, Count).Where(b => b % factor != 0).Select(Tick)];

        /// <summary>The minor ticks of <c>LogitLocator(minor=True)</c> for <paramref name="nbins"/>.</summary>
        internal double[] Minors(int nbins)
        {
            int numideal = Count - 2;
            if (numideal > nbins) return Skipped((int)Math.Ceiling((double)numideal / nbins));
            var ticks = new List<double>();
            for (int b = Low; b < High; b++)
            {
                if (b < -1)
                {
                    double power = Math.Pow(10, b);
                    for (int k = 2; k < 10; k++) ticks.Add(k * power);
                }
                else if (b == -1)
                {
                    for (int k = 2; k < 5; k++) ticks.Add(k / 10.0);
                }
                else if (b == 0)
                {
                    for (int k = 6; k < 9; k++) ticks.Add(k / 10.0);
                }
                else
                {
                    double power = Math.Pow(10, -b - 1);
                    for (int k = 9; k >= 2; k--) ticks.Add(1 - k * power);
                }
            }

            return [.. ticks];
        }
    }
}
