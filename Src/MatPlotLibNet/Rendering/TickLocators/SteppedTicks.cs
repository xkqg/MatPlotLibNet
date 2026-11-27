// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>The steps a run of ticks may take and how many of them a range is split into: matplotlib's <c>MaxNLocator</c> with
/// its <c>steps</c>, <c>nbins</c> and <c>min_n_ticks</c>. Three are used: the axis locator of matplotlib's default style
/// (<see cref="DefaultStyle"/>) and of its classic style (<see cref="Classic"/>), which the matplotlib themes place their ticks by
/// (<see cref="Styling.AxisTickLook.Ladder"/>; the classic one rounds an axis out to its ticks too), and the locator a contour asks
/// for the levels it draws when it names none (<see cref="ForLevels"/>).</summary>
/// <param name="Steps">The steps, from 1 to 10 (<c>MaxNLocator</c>'s <c>steps</c>), in units of the range's scale.</param>
/// <param name="Bins">How many steps the range is split into at most (<c>nbins</c>); when <paramref name="BinsByLength"/>, the
/// most it is ever split into.</param>
/// <param name="MinTicks">How many ticks must stand inside the range for a step to be taken (<c>min_n_ticks</c>).</param>
/// <param name="BinsByLength">Whether the bins follow the length of the axis (<c>nbins = 'auto'</c>, <see cref="ForTickSpace"/>).</param>
internal readonly record struct TickLadder(double[] Steps, int Bins, int MinTicks, bool BinsByLength = false)
{
    /// <summary>The staircase of steps, a decade below and one step above the steps given (<c>MaxNLocator._staircase</c>:
    /// <c>0.1 * steps[:-1]</c>, the steps, <c>10 * steps[1]</c>), made once.</summary>
    internal double[] ExtendedSteps { get; } = Staircase(Steps);

    /// <summary>The axis locator of matplotlib's classic style (<c>AutoLocator</c> in classic mode: <c>nbins = 9</c>,
    /// <c>steps = [1, 2, 5, 10]</c>, and <c>MaxNLocator</c>'s <c>min_n_ticks</c> of 2).</summary>
    internal static readonly TickLadder Classic = new([1, 2, 5, 10], 9, 2);

    /// <summary>The axis locator of matplotlib's default style (<c>AutoLocator</c>: <c>nbins = 'auto'</c>,
    /// <c>steps = [1, 2, 2.5, 5, 10]</c>, <c>min_n_ticks</c> 2): as many bins as the axis has room for, at most nine
    /// (<see cref="ForTickSpace"/>).</summary>
    internal static readonly TickLadder DefaultStyle = new([1, 2, 2.5, 5, 10], 9, 2, BinsByLength: true);

    /// <summary>This ladder laid along an axis with room for <paramref name="tickSpace"/> labels, as <c>MaxNLocator._raw_ticks</c> reads
    /// <c>nbins = 'auto'</c>: <c>np.clip(axis.get_tick_space(), max(1, min_n_ticks - 1), 9)</c>, the tick space being how many label spans
    /// fit along the axis (<see cref="MaxNLocator.LabelsAlong"/>, <see cref="TickRequest.TickSpace"/>). Only the bin count changes, so the
    /// staircase is the one made with the ladder; a ladder whose bins do not follow the length is returned as it is.</summary>
    /// <param name="tickSpace">How many labels fit along the axis.</param>
    /// <returns>The ladder with the bins of that room.</returns>
    internal TickLadder ForTickSpace(double tickSpace) => BinsByLength ? this with { Bins = Budget.Clamp(tickSpace) } : this;

    /// <summary>The bins this ladder lays along an axis whose bins follow the length: <c>max(1, min_n_ticks - 1)</c> to its
    /// <see cref="Bins"/>, the most for a room that is no number (<see cref="TickBudget"/>).</summary>
    internal TickBudget Budget => new(Math.Max(1, MinTicks - 1), Bins);

    /// <summary>The steps of <c>MaxNLocator</c> when it is given none.</summary>
    private static readonly double[] DefaultSteps = [1, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10];

    /// <summary>The locator a contour that names no levels asks for its levels: <c>ContourSet._ensure_locator_exists</c>,
    /// <c>ticker.MaxNLocator(N + 1, min_n_ticks=1)</c>, with the default steps.</summary>
    /// <param name="count">The number of levels asked for (matplotlib's <c>levels=N</c>).</param>
    /// <returns>The ladder.</returns>
    internal static TickLadder ForLevels(int count) => new(DefaultSteps, count + 1, 1);

    /// <summary>The range rounded out to the ends of the run of ticks this ladder lays over it, as matplotlib's
    /// <c>MaxNLocator.view_limits</c> does under <c>axes.autolimit_mode = 'round_numbers'</c>: a range too narrow for its magnitude
    /// is widened first (<c>_nonsingular</c>, expander 1e-12, tiny 1e-13), and the step is one from which the bins reach the end.
    /// A range is worked out before the plot has a length, so a ladder whose bins follow the length rounds out at its most bins.</summary>
    /// <param name="range">The range to round out; its low end below its high end.</param>
    /// <returns>The range from the run's first tick to its last.</returns>
    internal Range1D RoundedOut(Range1D range)
    {
        var widened = range.NonsingularBy(expander: 1e-12, tiny: 1e-13);
        var run = SteppedTicks.Over(widened.Lo, widened.Hi, this, roundNumbers: true);
        return new(run.First, run.Last);
    }

    /// <summary>The ticks of the run this ladder lays over <paramref name="view"/>, as <c>MaxNLocator.tick_values</c> lays it
    /// (<c>_nonsingular</c>, expander 1e-13, tiny 1e-14): the run holds the view, so it reaches a tick or two past either end. On a
    /// 3-D axis the raw step is taken as <c>raw_step * 23 / 24</c> (<c>_raw_ticks</c>: "the zoom factor of 2/48").</summary>
    /// <param name="view">The ends of the axis, whichever way it runs.</param>
    /// <param name="onThreeDAxis">Whether the axis belongs to a 3-D axes.</param>
    /// <param name="roundNumbers">Whether the theme rounds an axis out to its ticks (<c>axes.autolimit_mode = 'round_numbers'</c>),
    /// which asks for a step from which the bins reach the end of the view.</param>
    /// <returns>Every tick of the run, ascending.</returns>
    internal double[] TicksOver(Range1D view, bool onThreeDAxis, bool roundNumbers)
    {
        var (lo, hi) = view.Interval;
        var widened = new Range1D(lo, hi).NonsingularBy(expander: 1e-13, tiny: 1e-14);
        var run = SteppedTicks.Over(widened.Lo, widened.Hi, this, roundNumbers, onThreeDAxis);
        var ticks = new double[run.Count];
        for (int i = 0; i < ticks.Length; i++) ticks[i] = run[i];
        return ticks;
    }

    /// <summary>The ticks of <see cref="TicksOver"/> that stand in <paramref name="view"/>, the ones an axis draws
    /// (<c>_interval_contains_close</c>).</summary>
    /// <param name="view">The ends of the axis, whichever way it runs.</param>
    /// <param name="roundNumbers">Whether the theme rounds an axis out to its ticks.</param>
    /// <returns>The ticks in view, ascending.</returns>
    internal double[] TicksIn(Range1D view, bool roundNumbers) =>
        [.. TicksOver(view, onThreeDAxis: false, roundNumbers).Where(view.HoldsClose)];

    /// <summary>matplotlib's <c>MaxNLocator._staircase</c>, computed as it computes it, so every step has its bits.</summary>
    private static double[] Staircase(double[] steps)
    {
        var extended = new double[2 * steps.Length];
        for (int i = 0; i < steps.Length - 1; i++) extended[i] = 0.1 * steps[i];
        steps.CopyTo(extended, steps.Length - 1);
        extended[^1] = 10 * steps[1];
        return extended;
    }
}

/// <summary>A run of ticks at equal steps, as matplotlib's <c>MaxNLocator._raw_ticks</c> lays it over a range: the ticks
/// <c>(k * Step + Origin) + Offset</c> for every whole <c>k</c> from <see cref="Low"/> to <see cref="High"/>, which hold the
/// range, the last at or below its start and the first at or above its end. The one place that lays such a run: the round
/// numbers a classic axis ends at (<c>AxisAutoscale.RoundedOutToTicks</c>) and the levels of a filled contour
/// (<c>ContourLevels.Auto</c>).</summary>
/// <param name="Origin">The multiple of the step at or below the range's start, less the offset (<c>best_vmin</c>).</param>
/// <param name="Step">The step between two ticks.</param>
/// <param name="Low">The <c>k</c> of the first tick.</param>
/// <param name="High">The <c>k</c> of the last tick.</param>
/// <param name="Offset">The round number taken off a range far from zero before it is stepped (<c>scale_range</c>).</param>
internal readonly record struct SteppedTicks(double Origin, double Step, double Low, double High, double Offset)
{
    /// <summary>How many ticks the run has.</summary>
    internal int Count => (int)(High - Low) + 1;

    /// <summary>The tick at <paramref name="index"/>, from the first: <c>np.arange(low, high + 1) * step + best_vmin</c>, then the
    /// offset added back, in that order.</summary>
    /// <param name="index">0 for the first tick.</param>
    internal double this[int index] => (Low + index) * Step + Origin + Offset;

    /// <summary>The first tick.</summary>
    internal double First => this[0];

    /// <summary>The last tick.</summary>
    internal double Last => this[Count - 1];

    /// <summary>The run <paramref name="ladder"/> lays over <paramref name="vmin"/> .. <paramref name="vmax"/>
    /// (<c>MaxNLocator._raw_ticks</c>, read from matplotlib 3.11.2): the range is scaled and offset (<c>scale_range</c>); the
    /// smallest step of the staircase that is at least the range over the bins is taken, and under
    /// <paramref name="roundNumbers"/> (<c>axes.autolimit_mode = 'round_numbers'</c>) only one from which the bins reach the end
    /// of the range; when it leaves fewer ticks inside the range than the ladder asks for, the next smaller step is tried.</summary>
    /// <param name="vmin">The start of the range; less than <paramref name="vmax"/> and finite.</param>
    /// <param name="vmax">The end of the range.</param>
    /// <param name="ladder">The steps and bins.</param>
    /// <param name="roundNumbers">Whether the theme rounds an axis out to its ticks, which asks for a step that reaches.</param>
    /// <param name="onThreeDAxis">Whether the raw step is taken as <c>raw_step * 23 / 24</c>, as on a 3-D axis.</param>
    /// <returns>The run.</returns>
    internal static SteppedTicks Over(double vmin, double vmax, TickLadder ladder, bool roundNumbers, bool onThreeDAxis = false)
    {
        double width = Math.Abs(vmax - vmin), offset = ScaleOffset(vmin, vmax);
        double scale = (width / ladder.Bins).DecadeAtOrBelow();
        double from = vmin - offset, to = vmax - offset;
        double rawStep = (to - from) / ladder.Bins;
        if (onThreeDAxis) rawStep = rawStep * 23 / 24;

        var steps = ladder.ExtendedSteps;
        int chosen = steps.Length - 1;
        for (int i = 0; i < steps.Length; i++)
        {
            double step = steps[i] * scale;
            if (step >= rawStep && (!roundNumbers || from.DivMod(step).Quotient * step + step * ladder.Bins >= to))
            {
                chosen = i;
                break;
            }
        }

        var run = default(SteppedTicks);
        for (int i = chosen; i >= 0; i--)
        {
            double step = steps[i] * scale;
            double origin = from.DivMod(step).Quotient * step;
            var edge = TickEdge.Of(step, offset);
            run = new SteppedTicks(origin, step, edge.AtOrBelow(from - origin), edge.AtOrAbove(to - origin), offset);
            int inside = 0;
            for (double k = run.Low; k <= run.High; k++)
            {
                double tick = k * step + origin;
                if (tick <= to && tick >= from) inside++;
            }

            if (inside >= ladder.MinTicks) break;
        }

        return run;
    }

    /// <summary>The round number matplotlib's <c>scale_range</c> takes off a range far from zero before it steps it: the power of ten at or
    /// below the middle of <paramref name="vmin"/> .. <paramref name="vmax"/>, signed as the middle, when the middle is 100 widths or more
    /// from zero; 0 otherwise. The ladder of the matplotlib themes takes it off (<see cref="Over"/>); a ladder that keeps its values whole
    /// (<see cref="ArithmeticTicks"/>) widens its edge tolerance by it (<see cref="TickEdge.Of"/>), as the rounding of a value that far
    /// from zero is that large.</summary>
    /// <param name="vmin">The start of the range.</param>
    /// <param name="vmax">The end of the range.</param>
    /// <returns>The offset, 0 for none.</returns>
    internal static double ScaleOffset(double vmin, double vmax)
    {
        double width = Math.Abs(vmax - vmin), middle = (vmax + vmin) / 2;
        return Math.Abs(middle) / width < 100 ? 0 : Math.CopySign(Math.Abs(middle).DecadeAtOrBelow(), middle);
    }
}
