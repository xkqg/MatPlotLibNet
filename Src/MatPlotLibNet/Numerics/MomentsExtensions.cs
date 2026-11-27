// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MatPlotLibNet.Numerics;

/// <summary>The one place the mean, the variance and the standard deviation of a run of values are worked out, over the count less the
/// degrees of freedom the caller names (numpy's <c>ddof</c>: 0 for a population, 1 for a sample, which is a decision of each statistic
/// and is read at the call). A Bollinger band, a z-score, a kernel bandwidth, a standardised column and the error bar of a point plot
/// read it, and so do the rolling form (<see cref="RollingWindowExtensions"/>) and the vector (<see cref="Vec.Std"/>).
/// <para>A run shorter than <see cref="SimdFrom"/> values is summed in order, value by value: that is the arithmetic the indicators of a
/// window of 20 bars computed before this owner existed, and it is already the cheaper form there. From <see cref="SimdFrom"/> on the
/// squared distances are summed by one pass of SIMD (two accumulators of <see cref="Vector{T}"/>, the tail in order), which centres
/// nothing in a buffer, so no run of any length allocates or takes room from the stack. The SIMD kernel is compiled fully optimised on its
/// first call (<see cref="MethodImplOptions.AggressiveOptimization"/>): the generic vector calls of the tensor library it replaces ran
/// as unoptimised code for the first thousands of draws of a process before the runtime promoted them.</para></summary>
internal static class MomentsExtensions
{
    /// <summary>The length from which the sum of squares is taken by SIMD: a run of fewer values is summed in order.</summary>
    internal const int SimdFrom = 32;

    extension(ReadOnlySpan<double> values)
    {
        /// <summary>The arithmetic mean: the sum over the count.</summary>
        /// <returns>The mean; NaN for no values.</returns>
        internal double Mean() => Sum(values) / values.Length;

        /// <summary>The variance about the mean of the values.</summary>
        /// <param name="ddof">The degrees of freedom taken from the count: 0 divides by the count (a population), 1 by the count less one (a
        /// sample).</param>
        /// <returns>The variance; NaN when the count is not above <paramref name="ddof"/>.</returns>
        internal double Variance(int ddof = 0) => values.Variance(values.Mean(), ddof);

        /// <summary>The variance about a mean the caller already has (a running sum has it without another pass).</summary>
        /// <param name="about">The centre the deviations are taken from.</param>
        /// <param name="ddof">The degrees of freedom taken from the count.</param>
        /// <returns>The variance; NaN when the count is not above <paramref name="ddof"/>.</returns>
        internal double Variance(double about, int ddof = 0) =>
            (values.Length < SimdFrom ? SumOfSquaresInOrder(values, about) : SumOfSquaresBySimd(values, about)) / (values.Length - ddof);

        /// <summary>The standard deviation about the mean of the values.</summary>
        /// <param name="ddof">The degrees of freedom taken from the count: 0 for a population, 1 for a sample.</param>
        /// <returns>The standard deviation; NaN when the count is not above <paramref name="ddof"/>.</returns>
        internal double StdDev(int ddof = 0) => Math.Sqrt(values.Variance(ddof));

        /// <summary>The standard deviation about a mean the caller already has.</summary>
        /// <param name="about">The centre the deviations are taken from.</param>
        /// <param name="ddof">The degrees of freedom taken from the count.</param>
        /// <returns>The standard deviation; NaN when the count is not above <paramref name="ddof"/>.</returns>
        internal double StdDev(double about, int ddof = 0) => Math.Sqrt(values.Variance(about, ddof));
    }

    private static double Sum(ReadOnlySpan<double> values)
    {
        if (values.Length >= SimdFrom) return TensorPrimitives.Sum<double>(values);

        double sum = 0;
        for (int i = 0; i < values.Length; i++) sum += values[i];
        return sum;
    }

    /// <summary>The sum of the squares of the values' distances from <paramref name="about"/>, value by value in order. Inlined into its
    /// callers: left as a call it took 1.4 % of a draw of a point plot of four groups of 20 values (measured in processor cycles).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SumOfSquaresInOrder(ReadOnlySpan<double> values, double about)
    {
        double squares = 0;
        for (int i = 0; i < values.Length; i++)
        {
            double d = values[i] - about;
            squares += d * d;
        }

        return squares;
    }

    /// <summary>The sum of the squares of the values' distances from <paramref name="about"/> in one pass: two accumulators of a whole
    /// vector each, so that the adds of one do not wait for the other, and what does not fill a vector summed in order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double SumOfSquaresBySimd(ReadOnlySpan<double> values, double about)
    {
        int width = Vector<double>.Count;
        var centre = new Vector<double>(about);
        var first = Vector<double>.Zero;
        var second = Vector<double>.Zero;
        ref double start = ref MemoryMarshal.GetReference(values);
        int i = 0;
        for (; i <= values.Length - 2 * width; i += 2 * width)
        {
            var a = Vector.LoadUnsafe(ref start, (nuint)i) - centre;
            var b = Vector.LoadUnsafe(ref start, (nuint)(i + width)) - centre;
            first += a * a;
            second += b * b;
        }

        double squares = Vector.Sum(first + second);
        for (; i < values.Length; i++)
        {
            double d = values[i] - about;
            squares += d * d;
        }

        return squares;
    }
}
