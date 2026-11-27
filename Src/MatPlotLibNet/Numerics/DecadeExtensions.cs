// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The powers of ten around a value, and their exponents: at or below it, at or above it and nearest it, and matplotlib's
/// signed decades (<c>_decade_less_equal</c>, <c>_decade_greater_equal</c>). The one place the decade rule is written: the step magnitude
/// of a ladder of ticks, the offset a range far from zero is taken off by and the power of ten its labels are written in, a log axis
/// rounded out to its decades, a log tick told from a labelled minor, the decades a logit axis spans and names, a symmetric-log axis
/// rounded out and the library's cube ticks all read it.</summary>
internal static class DecadeExtensions
{
    extension(double value)
    {
        /// <summary>The exponent of the power of ten at or below this value, which is above 0: <c>floor(log10(value))</c>.</summary>
        /// <returns>The exponent, a whole number.</returns>
        internal double DecadeExponentAtOrBelow() => Math.Floor(Math.Log10(value));

        /// <summary>The exponent of the power of ten at or above this value, which is above 0: <c>ceil(log10(value))</c>.</summary>
        /// <returns>The exponent, a whole number.</returns>
        internal double DecadeExponentAtOrAbove() => Math.Ceiling(Math.Log10(value));

        /// <summary>The exponent of the power of ten nearest this value, which is above 0, on a log scale:
        /// <c>round(log10(value))</c>.</summary>
        /// <returns>The exponent, a whole number.</returns>
        internal double NearestDecadeExponent() => Math.Round(Math.Log10(value));

        /// <summary>The power of ten at or below this value, which is above 0: <c>10 ** floor(log10(value))</c>.</summary>
        /// <returns>The decade.</returns>
        internal double DecadeAtOrBelow() => Math.Pow(10, value.DecadeExponentAtOrBelow());

        /// <summary>The power of ten at or above this value, which is above 0: <c>10 ** ceil(log10(value))</c>.</summary>
        /// <returns>The decade.</returns>
        internal double DecadeAtOrAbove() => Math.Pow(10, value.DecadeExponentAtOrAbove());

        /// <summary>The power of ten nearest this value, which is above 0, on a log scale: <c>10 ** round(log10(value))</c>.</summary>
        /// <returns>The decade.</returns>
        internal double NearestDecade() => Math.Pow(10, value.NearestDecadeExponent());

        /// <summary>matplotlib's <c>_decade_less_equal(value, 10)</c>: the decade at or below a positive value, the negated decade at or
        /// above the size of a negative one, and 0 for 0.</summary>
        /// <returns>The decade.</returns>
        internal double SignedDecadeAtOrBelow() =>
            value == 0 ? 0 : value < 0 ? -(-value).DecadeAtOrAbove() : value.DecadeAtOrBelow();

        /// <summary>matplotlib's <c>_decade_greater_equal(value, 10)</c>: the decade at or above a positive value, the negated decade at
        /// or below the size of a negative one, and 0 for 0.</summary>
        /// <returns>The decade.</returns>
        internal double SignedDecadeAtOrAbove() =>
            value == 0 ? 0 : value < 0 ? -(-value).DecadeAtOrBelow() : value.DecadeAtOrAbove();

        /// <summary>matplotlib's <c>_decade_less(value, 10)</c>: the decade strictly below this value (a decade itself steps one down),
        /// the negated decade strictly above the size of a negative one. A log axis opens a range of one value by it
        /// (<c>LogLocator.nonsingular</c>).</summary>
        /// <returns>The decade.</returns>
        internal double DecadeBelow()
        {
            if (value < 0) return -(-value).DecadeAbove();
            double less = value.SignedDecadeAtOrBelow();
            return less == value ? less / 10 : less;
        }

        /// <summary>matplotlib's <c>_decade_greater(value, 10)</c>: the decade strictly above this value (a decade itself steps one up),
        /// the negated decade strictly below the size of a negative one.</summary>
        /// <returns>The decade.</returns>
        internal double DecadeAbove()
        {
            if (value < 0) return -(-value).DecadeBelow();
            double greater = value.SignedDecadeAtOrAbove();
            return greater == value ? greater * 10 : greater;
        }
    }
}
