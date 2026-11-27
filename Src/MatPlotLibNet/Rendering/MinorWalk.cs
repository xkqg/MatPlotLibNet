// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Rendering;

/// <summary>The minor tick values between the major ticks of an axis, low to high: from a major step below the first major
/// to a major step past the last, every <c>minorStep</c>, each value worked out from its index and rounded to ten decimals (the one
/// walk of a ladder, <see cref="TickLocators.ArithmeticTicks"/>), and a value within a hundredth of a step of a major tick left out,
/// for the major stands there.
/// The one walk the minor tick marks and the minor grid lines stand on; what each draws at a value, and which values it
/// keeps, is the caller's. A walk made <see cref="Over"/> values a scale's locator placed walks those instead. Walked with
/// <c>foreach</c>, it allocates nothing.</summary>
/// <param name="majors">The major tick values, in the axis' own unit, low to high; fewer than two walk nothing.</param>
/// <param name="minorStep">The distance between two minor values; a step that is not a positive number walks nothing.</param>
/// <param name="given">The minor values to walk as they are, or <see langword="null"/> to split the major step.</param>
internal readonly struct MinorWalk(double[] majors, double minorStep, double[]? given = null)
{
    /// <summary>The walk over <paramref name="values"/> as they are: the minor ticks a scale's own locator placed.</summary>
    /// <param name="values">The minor values, low to high.</param>
    /// <returns>The walk.</returns>
    internal static MinorWalk Over(double[] values) => new([], 0, values);

    /// <summary>The walk's enumerator, a struct, so <c>foreach</c> allocates nothing.</summary>
    /// <returns>The enumerator, before the first value.</returns>
    public Enumerator GetEnumerator() => given is null ? new(majors, minorStep) : new(given);

    /// <summary>Walks the minor values one at a time (<see cref="MinorWalk"/>).</summary>
    internal struct Enumerator
    {
        private readonly double[] _majors;
        private readonly double _tolerance;
        private readonly double[]? _given;
        private ArithmeticTicks.Enumerator _ladder;
        private int _index;

        /// <summary>An enumerator before the first of <paramref name="given"/>.</summary>
        /// <param name="given">The values to walk as they are.</param>
        internal Enumerator(double[] given)
        {
            _majors = [];
            _given = given;
            _index = -1;
        }

        /// <summary>An enumerator before the first value of the walk over <paramref name="majors"/>: the ladder of
        /// <paramref name="step"/> from a major step below the first major to a major step past the last (<see cref="ArithmeticTicks"/>,
        /// each value worked out from its index).</summary>
        /// <param name="majors">The major tick values.</param>
        /// <param name="step">The distance between two minor values.</param>
        internal Enumerator(double[] majors, double step)
        {
            _majors = majors;
            _tolerance = step * ArithmeticTicks.Reach;
            // Fewer than two majors, or a step that is not a positive number, walk nothing: the ladder of a step of NaN is empty.
            double majorStep = majors.Length < 2 ? double.NaN : majors[1] - majors[0];
            var ladder = majors.Length < 2 ? new ArithmeticTicks(double.NaN, double.NaN, double.NaN)
                : ArithmeticTicks.Through(majors[0] - majorStep, step, majors[^1] + majorStep);
            _ladder = ladder.GetEnumerator();
        }

        /// <summary>The value the walk stands on.</summary>
        public double Current { readonly get; private set; }

        /// <summary>Moves to the next minor value.</summary>
        /// <returns><see langword="true"/> when there is one.</returns>
        public bool MoveNext()
        {
            if (_given is not null)
            {
                if (++_index >= _given.Length) return false;
                Current = _given[_index];
                return true;
            }

            while (_ladder.MoveNext())
            {
                double value = _ladder.Current;
                if (OnAMajor(value)) continue;
                Current = value;
                return true;
            }
            return false;
        }

        /// <summary>Whether a major tick stands within a hundredth of a step of <paramref name="value"/>.</summary>
        private readonly bool OnAMajor(double value)
        {
            foreach (double major in _majors)
                if (Math.Abs(major - value) < _tolerance) return true;
            return false;
        }
    }
}
