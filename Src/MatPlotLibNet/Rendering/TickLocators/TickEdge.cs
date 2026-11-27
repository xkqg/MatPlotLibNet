// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>The edge of a ladder of ticks: matplotlib's <c>_Edge_integer(step, offset)</c>, the step and how close to a whole step, in
/// steps, a value counts as on it, worked out once from the offset (<see cref="Of"/>) and asked for the multiple at or below and at or
/// above a value (<see cref="AtOrBelow"/>, <see cref="AtOrAbove"/>). The one value the run of a matplotlib ladder
/// (<see cref="SteppedTicks.Over"/>), the library's ladders (<see cref="ArithmeticTicks"/>), a <see cref="MultipleLocator"/>'s rounded
/// range and the library's cube ticks count whole steps by.</summary>
/// <param name="Step">The step, positive.</param>
/// <param name="Tolerance">How close to a whole step, in steps, a remainder counts as one (<c>_Edge_integer.closeto</c>'s tolerance).</param>
internal readonly record struct TickEdge(double Step, double Tolerance)
{
    /// <summary>The tolerance with no offset: a ten-billionth of a step.</summary>
    private const double PlainTolerance = 1e-10;

    /// <summary>The most the tolerance grows to, short of half a step (<c>min(0.4999, tol)</c>).</summary>
    private const double MostTolerance = 0.4999;

    /// <summary>The edge of <paramref name="step"/> for a range taken off by <paramref name="offset"/>: <c>_Edge_integer.closeto</c>'s
    /// tolerance, <see cref="PlainTolerance"/> with no offset, more when the offset is large against the step
    /// (<c>10 ** (log10(|offset| / step) - 12)</c>, at most <see cref="MostTolerance"/>).</summary>
    /// <param name="step">The step, positive.</param>
    /// <param name="offset">The offset the range was taken off by (<see cref="SteppedTicks.ScaleOffset"/>); 0 for none.</param>
    /// <returns>The edge.</returns>
    internal static TickEdge Of(double step, double offset)
    {
        if (offset == 0) return new(step, PlainTolerance);
        double digits = Math.Log10(Math.Abs(offset) / step);
        return new(step, Math.Min(MostTolerance, Math.Max(PlainTolerance, Math.Pow(10, digits - 12))));
    }

    /// <summary><c>_Edge_integer.le</c>: the largest whole <c>n</c> with <c>n * step</c> at or below <paramref name="x"/>, a remainder
    /// within the tolerance of a whole step counted as one (<see cref="WholeStepExtensions"/>).</summary>
    /// <param name="x">The value.</param>
    /// <returns>The whole number of steps.</returns>
    internal double AtOrBelow(double x) => x.StepsAtOrBelow(Step, Tolerance);

    /// <summary><c>_Edge_integer.ge</c>: the smallest whole <c>n</c> with <c>n * step</c> at or above <paramref name="x"/>.</summary>
    /// <param name="x">The value.</param>
    /// <returns>The whole number of steps.</returns>
    internal double AtOrAbove(double x) => x.StepsAtOrAbove(Step, Tolerance);

    /// <summary>The multiple of the step at or below <paramref name="x"/>: <see cref="AtOrBelow"/> times the step.</summary>
    /// <param name="x">The value.</param>
    /// <returns>The multiple.</returns>
    internal double MultipleAtOrBelow(double x) => AtOrBelow(x) * Step;

    /// <summary>The multiple of the step at or above <paramref name="x"/>: <see cref="AtOrAbove"/> times the step.</summary>
    /// <param name="x">The value.</param>
    /// <returns>The multiple.</returns>
    internal double MultipleAtOrAbove(double x) => AtOrAbove(x) * Step;
}
