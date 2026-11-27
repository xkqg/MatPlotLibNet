// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>The kind of axis a <see cref="TickRequest"/> asks for, and what that kind does where neither the axis nor the theme places
/// its ticks: the library's own placement (<see cref="LibraryMajors"/>), every list of it at any length
/// (<see cref="LibraryMajorsAtAnyLength"/>), and how much of a theme's ladder it keeps (<see cref="Laid"/>). One sealed leaf per kind
/// (<see cref="Plot"/>, <see cref="Cube"/>, <see cref="Ruler"/>), so a request asks its kind and never compares it. The kinds differ on
/// purpose, each as the library has always drawn it.</summary>
internal abstract class TickAxisKind
{
    /// <summary>A 2-D axis: the library's nearest step over eight intervals from a plot of 240 pixels, fewer along a shorter one
    /// (<see cref="AutoLocator.ForLength"/>), so normal-sized plots keep the ticks they have always had; of a ladder's run, the ticks in
    /// view.</summary>
    internal static TickAxisKind Plot { get; } = new PlotAxis();

    /// <summary>An axis of a 3-D cube: the steps of matplotlib's default style (<see cref="TickLadder.DefaultStyle"/>: 1, 2, 2.5, 5 and 10)
    /// over at most ten intervals whatever the width, the target that matches matplotlib's bar3d reference scene best; of a ladder's run,
    /// every tick, laid with the 3-D raw step (<c>raw_step * 23 / 24</c>), as matplotlib's 3-D axis formats every value its locator gives
    /// before it keeps the ones in view.</summary>
    internal static TickAxisKind Cube { get; } = new CubeAxis();

    /// <summary>A scale drawn along a length, a colour bar or the rings of a polar axes: as many ticks as fit, two to nine
    /// (<see cref="MaxNLocator"/>), a short ruler beside a chart and on purpose less dense than an axis, naming only the ticks inside its
    /// range; of a ladder's run, the ticks in view.</summary>
    internal static TickAxisKind Ruler { get; } = new RulerAxis();

    private TickAxisKind() { }

    /// <summary>The library's own placement of the major ticks of <paramref name="request"/>, where neither the axis nor the theme names
    /// one.</summary>
    /// <param name="request">The request, its span ascending or descending.</param>
    /// <returns>The ticks, ascending.</returns>
    internal abstract double[] LibraryMajors(in TickRequest request);

    /// <summary>Every list of major ticks the library's own placement names at any length, for a layout that keeps room before it knows
    /// the length: the one list of <see cref="LibraryMajors"/>, which no length changes, unless the kind counts its ticks by the
    /// room.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The lists, one at a time.</returns>
    internal virtual IEnumerable<double[]> LibraryMajorsAtAnyLength(TickRequest request)
    {
        yield return LibraryMajors(request);
    }

    /// <summary>The ticks <paramref name="ladder"/> lays over <paramref name="span"/> that this kind keeps: the ones in view.</summary>
    /// <param name="ladder">The theme's ladder, its bins already laid for the room.</param>
    /// <param name="span">The ends of the axis, whichever way it runs.</param>
    /// <param name="roundNumbers">Whether the theme rounds an axis out to its ticks.</param>
    /// <returns>The ticks, ascending.</returns>
    internal virtual double[] Laid(TickLadder ladder, Range1D span, bool roundNumbers) => ladder.TicksIn(span, roundNumbers);

    /// <summary>A 2-D axis (<see cref="Plot"/>).</summary>
    private sealed class PlotAxis : TickAxisKind
    {
        /// <inheritdoc />
        internal override double[] LibraryMajors(in TickRequest request)
        {
            var (lo, hi) = request.Span.Interval;
            return AutoLocator.ForLength(request.LibraryLengthPx).Locate(lo, hi);
        }
    }

    /// <summary>An axis of a 3-D cube (<see cref="Cube"/>).</summary>
    private sealed class CubeAxis : TickAxisKind
    {
        /// <summary>The most intervals the library's cube ticks lay (eleven accepted, as matplotlib's <c>MaxNLocator</c> accepts
        /// <c>nbins + 1</c>).</summary>
        private const int MostIntervals = 10;

        /// <inheritdoc />
        internal override double[] Laid(TickLadder ladder, Range1D span, bool roundNumbers) =>
            ladder.TicksOver(span, onThreeDAxis: true, roundNumbers);

        /// <inheritdoc />
        /// <remarks>The first step of the default style's (<see cref="TickLadder.DefaultStyle"/>), from finest to coarsest, that leaves at
        /// least 2 and at most <see cref="MostIntervals"/> + 1 intervals between the multiples at or beyond the two ends (a width of 1.83
        /// takes a step of 0.2 over eleven); the values run from the first multiple at or above the low end to a hundredth of a step past the
        /// high end (<see cref="ArithmeticTicks.Over"/>), and which of them stand on the axis is the cube row's to decide.</remarks>
        internal override double[] LibraryMajors(in TickRequest request)
        {
            var (lo, hi) = request.Span.Interval;
            double magnitude = ((hi - lo) / MostIntervals).DecadeAtOrBelow();
            double chosenStep = 10 * magnitude, offset = SteppedTicks.ScaleOffset(lo, hi);
            foreach (double s in TickLadder.DefaultStyle.Steps)
            {
                // How many steps the multiples at or beyond the two ends span (matplotlib's _Edge_integer, its tolerance widened as
                // ArithmeticTicks widens it).
                double step = s * magnitude;
                var edge = TickEdge.Of(step, offset);
                double count = edge.AtOrAbove(hi) - edge.AtOrBelow(lo);
                if (count <= MostIntervals + 1 && count >= 2)
                {
                    chosenStep = step;
                    break;
                }
            }

            return ArithmeticTicks.Over(lo, hi, chosenStep).ToArray();
        }
    }

    /// <summary>A scale drawn along a length (<see cref="Ruler"/>).</summary>
    private sealed class RulerAxis : TickAxisKind
    {
        /// <inheritdoc />
        internal override double[] LibraryMajors(in TickRequest request)
        {
            var (lo, hi) = request.Span.Interval;
            double room = MaxNLocator.LabelsAlong(request.LibraryLengthPx, request.LabelPx, request.AlongX);
            return Inside(MaxNLocator.ForTickSpace(room).Locate(lo, hi), lo, hi);
        }

        /// <inheritdoc />
        /// <remarks>A ruler asks for as many ticks as fit, so at any length it names every count of its budget
        /// (<see cref="TickBudget.Standard"/>).</remarks>
        internal override IEnumerable<double[]> LibraryMajorsAtAnyLength(TickRequest request)
        {
            var (lo, hi) = request.Span.Interval;
            foreach (int most in TickBudget.Standard)
                yield return Inside(new MaxNLocator(most).Locate(lo, hi), lo, hi);
        }

        /// <summary><paramref name="ticks"/> from <paramref name="lo"/> to <paramref name="hi"/>, ends included; the array itself when
        /// every tick is inside.</summary>
        private static double[] Inside(double[] ticks, double lo, double hi)
        {
            int inside = 0;
            foreach (double tick in ticks)
                if (tick >= lo && tick <= hi) inside++;
            if (inside == ticks.Length) return ticks;

            var kept = new double[inside];
            int k = 0;
            foreach (double tick in ticks)
                if (tick >= lo && tick <= hi) kept[k++] = tick;
            return kept;
        }
    }
}
