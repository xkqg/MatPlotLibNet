// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Numerics;

/// <summary>The checks a value given to the library is held to, written once: finite (not NaN, not an infinity), other than one
/// value (a scale of 0, a logarithm of base 1), at least a floor (a period of at least 2), above a floor (a width above 0), in a closed
/// range (a centre line between its limits), strictly between two others (a smoothing factor in (0, 1)), a power of 2, an amount (a
/// missing value, or finite and at least a floor), the index of an item of a list, a condition the site tells (a range open at one end,
/// a power of ten). Each returns the value it accepts, so a check reads where the value is taken and a
/// site that asks more than one thing chains them (<c>width.ThrowIfNotFinite(name).ThrowIfNotAbove(0, name)</c>); each refuses the rest
/// with one <see cref="ArgumentOutOfRangeException"/> whose message names the value (by what it is, else by the parameter) and the
/// number, written the same in every culture, and whose <see cref="ArgumentException.ParamName"/> is the parameter. This class and
/// <c>EnumMemberExtensions</c> are the only places that make that exception (<c>RefusalRules</c> holds it).
/// <para>A value that is one of many (the count of a category, the flow of a cell) is named by a function of a state
/// (<c>(categories, i), static at =&gt; $"The count of '{at.categories[at.i]}'"</c>) that runs only when the value is refused, so a
/// check of every value of an array writes no text and allocates nothing for the values it accepts.</para></summary>
internal static class ArgumentRangeExtensions
{
    extension<T>(T value) where T : INumber<T>
    {
        /// <summary>This value, refused when it is below <paramref name="least"/> or not a number.</summary>
        /// <param name="least">The smallest value accepted.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="what">What the value is, when it is one of a few (<c>The upper limit</c>); the parameter's name by default.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is below <paramref name="least"/> or not a number.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ThrowIfBelow(T least, string name, string? what = null) => value.ThrowIfBelow(least, name, what ?? name, Itself);

        /// <summary>As the check above, the value named by <paramref name="what"/> of
        /// <paramref name="state"/> only when it is refused.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ThrowIfBelow<TState>(T least, string name, TState state, Func<TState, string> what) =>
            value >= least ? value : throw Refused(name, what(state), $"must be >= {least.InvariantText()}", value);

        /// <summary>This value, refused when it is outside the closed range from <paramref name="least"/> to <paramref name="most"/>.</summary>
        /// <param name="least">The smallest value accepted.</param>
        /// <param name="most">The largest value accepted.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="what">What the value is, when it is one of a few; the parameter's name by default.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is outside the range or not a number.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ThrowIfNotIn(T least, T most, string name, string? what = null) => value.ThrowIfNotIn(least, most, name, what ?? name, Itself);

        /// <summary>As the check above, the value named by <paramref name="what"/> of
        /// <paramref name="state"/> only when it is refused.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ThrowIfNotIn<TState>(T least, T most, string name, TState state, Func<TState, string> what) =>
            value >= least && value <= most
                ? value
                : throw Refused(name, what(state), $"must be in [{least.InvariantText()}, {most.InvariantText()}]", value);
    }

    extension(int value)
    {
        /// <summary>This value, refused when it is not a power of 2 at least <paramref name="least"/> (the width of a Haar transform).</summary>
        /// <param name="least">The smallest value accepted.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is below <paramref name="least"/> or not a power of 2.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ThrowIfNotPowerOfTwo(int least, string name) =>
            value >= least && int.IsPow2(value) ? value : throw Refused(name, name, string.Invariant($"must be a power of 2 and >= {least}"), value);

        /// <summary>This value, refused when it is no index of a list of <paramref name="count"/> items (from 0 to one below the
        /// count; a list with no item has none).</summary>
        /// <param name="count">How many items the list holds.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="what">What the index is, when it is one of a few; the parameter's name by default.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ThrowIfNotIndexOf(int count, string name, string? what = null) => value.ThrowIfNotIndexOf(count, name, what ?? name, Itself);

        /// <summary>As the check above, the index named by <paramref name="what"/> of
        /// <paramref name="state"/> only when it is refused.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ThrowIfNotIndexOf<TState>(int count, string name, TState state, Func<TState, string> what) =>
            (uint)value < (uint)count
                ? value
                : throw Refused(name, what(state), count > 0 ? string.Invariant($"must be from 0 to {count - 1}") : "must name an item, and there is none", value);
    }

    extension<T>(T value) where T : IFormattable
    {
        /// <summary>This value, refused when <paramref name="holds"/> is false: a rule no pair of bounds says (a range open at one
        /// end, a power of ten, a time above none), told by the site as a condition.</summary>
        /// <param name="holds">Whether the value is what it has to be.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="rule">What the value has to be, after its subject (<c>must be in [0, 1)</c>).</param>
        /// <param name="what">What the value is, when it is one of a few; the parameter's name by default.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ThrowIfNot(bool holds, string name, string rule, string? what = null) =>
            holds ? value : throw Refused(name, what ?? name, rule, value);
    }

    extension(double value)
    {
        /// <summary>This value, refused when it is NaN or an infinity.</summary>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="what">What the value is, when it is one of a few; the parameter's name by default.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is NaN or an infinity.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfNotFinite(string name, string? what = null) => value.ThrowIfNotFinite(name, what ?? name, Itself);

        /// <summary>As <see cref="ThrowIfNotFinite(double, string, string?)"/>, the value named by <paramref name="what"/> of
        /// <paramref name="state"/> only when it is refused.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfNotFinite<TState>(string name, TState state, Func<TState, string> what) =>
            double.IsFinite(value) ? value : throw Refused(name, what(state), "must be finite", value);

        /// <summary>This value, refused when it is <paramref name="other"/>: the one value a number has no use at (a scale of 0, a
        /// logarithm of base 1, both of which map every value to one).</summary>
        /// <param name="other">The value refused.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is <paramref name="other"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfEqual(double other, string name) =>
            value != other ? value : throw Refused(name, name, $"must be other than {other.InvariantText()}", value);

        /// <summary>This value, refused when it is not above <paramref name="floor"/> (the floor itself, below it, or not a number).</summary>
        /// <param name="floor">The value it has to be above.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <param name="what">What the value is, when it is one of a few; the parameter's name by default.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is not above <paramref name="floor"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfNotAbove(double floor, string name, string? what = null) => value.ThrowIfNotAbove(floor, name, what ?? name, Itself);

        /// <summary>As <see cref="ThrowIfNotAbove(double, double, string, string?)"/>, the value named by <paramref name="what"/> of
        /// <paramref name="state"/> only when it is refused; <paramref name="name"/> is <see langword="null"/> for a value that is no
        /// parameter of the caller's (the price of one bar of a struct).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfNotAbove<TState>(double floor, string? name, TState state, Func<TState, string> what) =>
            value > floor ? value : throw Refused(name, what(state), $"must be > {floor.InvariantText()}", value);

        /// <summary>This value, worked out from the caller's data (the span of a column, the bandwidth or the density of a sample),
        /// refused when a double cannot hold it: the data, not one parameter's range, is what cannot be drawn, so the refusal is an
        /// <see cref="ArgumentException"/> and not an <see cref="ArgumentOutOfRangeException"/>.</summary>
        /// <param name="name">The parameter that holds the data, for the error.</param>
        /// <param name="what">What the value is (<c>The span of 'huge'</c>).</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentException">The value is NaN or an infinity.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfBeyondADouble(string name, string what) => value.ThrowIfBeyondADouble(name, what, Itself);

        /// <summary>As <see cref="ThrowIfBeyondADouble(double, string, string)"/>, the value named by <paramref name="what"/> of
        /// <paramref name="state"/> only when it is refused.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfBeyondADouble<TState>(string name, TState state, Func<TState, string> what) =>
            double.IsFinite(value) ? value : throw new ArgumentException($"{what(state)} is beyond what a double holds (got {value.InvariantText()}).", name);

        /// <summary>This value, refused when it is not strictly between <paramref name="low"/> and <paramref name="high"/>.</summary>
        /// <param name="low">The value it has to be above.</param>
        /// <param name="high">The value it has to be below.</param>
        /// <param name="name">The name of the parameter, for the error.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is not in the open interval.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double ThrowIfNotBetween(double low, double high, string name) =>
            value > low && value < high
                ? value
                : throw Refused(name, name, $"must be in ({low.InvariantText()}, {high.InvariantText()})", value);
    }

    extension(double[] values)
    {
        /// <summary>These values, an amount each where NaN marks a missing one (the counts of a Pareto chart, the sizes of a
        /// Marimekko chart's segment, the thicknesses of a stream, the ranks of a bump chart): a missing value passes, any other has
        /// to be finite and at least <paramref name="least"/>. The first value refused is named by <paramref name="what"/> of
        /// <paramref name="state"/> and its index; the values accepted are only compared.</summary>
        /// <param name="name">The name of the parameter that holds the values, for the error.</param>
        /// <param name="state">What <paramref name="what"/> names a value from (the labels).</param>
        /// <param name="what">What the value at an index is (<c>The count of 'b'</c>).</param>
        /// <param name="least">The smallest value accepted; 0 by default (an amount).</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is an infinity or below <paramref name="least"/>.</exception>
        internal void ThrowIfAnyNotAmount<TState>(string name, TState state, Func<TState, int, string> what, double least = 0)
        {
            for (int i = 0; i < values.Length; i++)
            {
                double value = values[i];
                if (!value.IsMissing() && !(value >= least && value < double.PositiveInfinity))
                    value.ThrowIfNotFinite(name, what(state, i)).ThrowIfBelow(least, name, what(state, i));
            }
        }
    }

    /// <summary>A subject given as text: the text itself.</summary>
    private static string Itself(string subject) => subject;

    private static ArgumentOutOfRangeException Refused(string? name, string subject, string rule, IFormattable value) =>
        new ArgumentOutOfRangeException(name, $"{subject} {rule} (got {value.InvariantText()}).");
}
