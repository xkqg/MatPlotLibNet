// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>A regular ladder of ticks: <paramref name="First"/>, then <paramref name="First"/> plus one, two, three ... times
/// <paramref name="Step"/>, as long as the tick is at or below <paramref name="Limit"/>, each rounded to ten decimals. The one walk the
/// ticks of the library's own locator, of a 3-D axis, of a <see cref="MultipleLocator"/> and the minor values between the major ticks
/// stand on.
/// <para>Each tick is worked out from its index, never added to the one before it: far from zero a step smaller than the distance
/// between two doubles leaves a sum where it is, and a walk that adds never reaches the end of its range; a walk that rounds as it goes
/// drifts by a rounding at every step. A tick that rounds onto the one before it is left out, a tick that is no number ends the walk
/// (it is not at or below the limit), and a step that is not a positive number walks nothing. Walked with <c>foreach</c> it allocates
/// nothing; <see cref="ToArray"/> walks twice, once to count and once to fill, so the array is made once at its size.</para></summary>
/// <param name="First">The first tick.</param>
/// <param name="Step">The distance between two ticks.</param>
/// <param name="Limit">The value the walk stops past.</param>
internal readonly record struct ArithmeticTicks(double First, double Step, double Limit)
{
    /// <summary>How far past the end of its range a ladder reaches, in steps: a hundredth of one, so a tick that a rounding put a
    /// hair past the end still stands there.</summary>
    internal const double Reach = 0.01;

    /// <summary>The ladder from <paramref name="first"/> in steps of <paramref name="step"/> that reaches a hundredth of a step
    /// (<see cref="Reach"/>) past <paramref name="end"/>.</summary>
    /// <param name="first">The first tick.</param>
    /// <param name="step">The distance between two ticks.</param>
    /// <param name="end">The end of the range the ticks are laid over.</param>
    /// <returns>The ladder.</returns>
    internal static ArithmeticTicks Through(double first, double step, double end) => new(first, step, end + step * Reach);

    /// <summary>The ladder of <paramref name="step"/> over <paramref name="lo"/> .. <paramref name="hi"/> the library lays: from the first
    /// multiple of the step at or above the low end (<see cref="TickEdge.AtOrAbove"/>, matplotlib's <c>_Edge_integer</c>, so a tick a
    /// rounding put a hair inside the low end stands there) to a hundredth of a step past the high end (<see cref="Through"/>). The values
    /// are kept whole, so the edge's tolerance is widened by the offset matplotlib would take off the range (<see cref="SteppedTicks.ScaleOffset"/>):
    /// far from zero a value's own rounding is many ten-billionths of a step.</summary>
    /// <param name="lo">The low end of the range.</param>
    /// <param name="hi">The high end of the range.</param>
    /// <param name="step">The distance between two ticks.</param>
    /// <returns>The ladder.</returns>
    internal static ArithmeticTicks Over(double lo, double hi, double step) =>
        Through(TickEdge.Of(step, SteppedTicks.ScaleOffset(lo, hi)).MultipleAtOrAbove(lo), step, hi);

    /// <summary>The multiples of <paramref name="step"/>, shifted by <paramref name="offset"/>, from the first at or above
    /// <paramref name="lo"/> to the last at or below <paramref name="hi"/>, both ends by matplotlib's edge (<c>MultipleLocator.tick_values</c>'
    /// <c>_Edge_integer(base, 0)</c>, <see cref="TickEdge.AtOrAbove"/> and <see cref="TickEdge.AtOrBelow"/>), its tolerance widened as in
    /// <see cref="Over"/>.</summary>
    /// <param name="lo">The low end of the range.</param>
    /// <param name="hi">The high end of the range.</param>
    /// <param name="step">The distance between two ticks.</param>
    /// <param name="offset">The shift of every multiple.</param>
    /// <returns>The ladder.</returns>
    internal static ArithmeticTicks Multiples(double lo, double hi, double step, double offset)
    {
        double from = lo - offset, to = hi - offset;
        var edge = TickEdge.Of(step, SteppedTicks.ScaleOffset(from, to));
        return Through(offset + edge.MultipleAtOrAbove(from), step, offset + edge.MultipleAtOrBelow(to));
    }

    /// <summary>The multiples of <paramref name="step"/> at or below <paramref name="lo"/> and at or above <paramref name="hi"/>
    /// (<see cref="TickEdge.AtOrBelow"/>, <see cref="TickEdge.AtOrAbove"/>), each written as a tick is
    /// (<see cref="Rounded"/>): the range a ladder of that step holds, the edges' tolerance widened as in <see cref="Over"/>.</summary>
    /// <param name="lo">The low end of the range.</param>
    /// <param name="hi">The high end of the range.</param>
    /// <param name="step">The distance between two ticks.</param>
    /// <returns>The range from the multiple at or below the low end to the one at or above the high end.</returns>
    internal static MinMaxRange Holding(double lo, double hi, double step)
    {
        var edge = TickEdge.Of(step, SteppedTicks.ScaleOffset(lo, hi));
        return new(Rounded(edge.MultipleAtOrBelow(lo)), Rounded(edge.MultipleAtOrAbove(hi)));
    }

    /// <summary>A tick value as a ladder writes it: rounded to ten decimals, so the bits of a product (0.30000000000000004) do not
    /// stand in a label or a range end.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The rounded value.</returns>
    internal static double Rounded(double value) => Math.Round(value, 10);

    /// <summary>The walk's enumerator, a struct, so <c>foreach</c> allocates nothing.</summary>
    /// <returns>The enumerator, before the first tick.</returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>The ticks, ascending, in one array of exactly their number.</summary>
    /// <returns>The ticks; empty when the walk has none.</returns>
    internal double[] ToArray()
    {
        int count = 0;
        foreach (double _ in this) count++;
        if (count == 0) return [];

        var ticks = new double[count];
        int at = 0;
        foreach (double tick in this) ticks[at++] = tick;
        return ticks;
    }

    /// <summary>Walks the ladder one tick at a time (<see cref="ArithmeticTicks"/>).</summary>
    internal struct Enumerator
    {
        private readonly ArithmeticTicks _ladder;
        private long _index;
        private bool _started;

        /// <summary>An enumerator before the first tick of <paramref name="ladder"/>.</summary>
        /// <param name="ladder">The ladder.</param>
        internal Enumerator(ArithmeticTicks ladder) => _ladder = ladder;

        /// <summary>The tick the walk stands on.</summary>
        public double Current { readonly get; private set; }

        /// <summary>Moves to the next tick.</summary>
        /// <returns><see langword="true"/> when there is one.</returns>
        public bool MoveNext()
        {
            if (!(_ladder.Step > 0)) return false;
            while (true)
            {
                double tick = _ladder.First + _index++ * _ladder.Step;
                if (!(tick <= _ladder.Limit)) return false;
                double rounded = Rounded(tick);
                if (_started && !(rounded > Current)) continue;
                Current = rounded;
                _started = true;
                return true;
            }
        }
    }
}
