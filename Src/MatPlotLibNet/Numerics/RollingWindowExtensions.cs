// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Numerics;

/// <summary>The windows a series of values is read through, each in one pass: the mean of every full window, the lowest and the
/// highest value of every window, and the population standard deviation of every full window about its mean. The moving-average,
/// band and oscillator indicators and the relative rotation graph read them.</summary>
internal static class RollingWindowExtensions
{
    extension(ReadOnlySpan<double> source)
    {
        /// <summary>The mean of every full window of <paramref name="period"/> values: the first window summed in order, then each
        /// window the one before it plus the value that comes in less the value that goes out, which is the running sum the streaming
        /// averages take a value at a time (<see cref="RollingMeanStep"/>): the two agree to the last bit (held by a test), and this form
        /// reads the values it is given instead of keeping a copy of the window.</summary>
        /// <param name="period">The width of a window.</param>
        /// <returns>One mean per full window (<c>Length - period + 1</c>), none when the values are fewer than a window.</returns>
        internal double[] RollingMean(int period)
        {
            int n = source.Length;
            if (n < period) return [];

            var result = new double[n - period + 1];
            double sum = 0;
            for (int i = 0; i < period; i++) sum += source[i];
            result[0] = sum / period;
            for (int i = period; i < n; i++)
            {
                sum += source[i] - source[i - period];
                result[i - period + 1] = sum / period;
            }
            return result;
        }

        /// <summary>The lowest value of every window of <paramref name="period"/> values ending at each index, a partial window
        /// before the first full one (<see cref="RollingExtremeStep{TKeep}"/>).</summary>
        /// <param name="period">The width of a window.</param>
        /// <param name="dst">One value per source value.</param>
        internal void RollingMin(int period, Span<double> dst) => RollingExtreme<KeepLowest>(source, period, dst);

        /// <summary>The highest value of every window of <paramref name="period"/> values ending at each index, a partial window
        /// before the first full one (<see cref="RollingExtremeStep{TKeep}"/>).</summary>
        /// <param name="period">The width of a window.</param>
        /// <param name="dst">One value per source value.</param>
        internal void RollingMax(int period, Span<double> dst) => RollingExtreme<KeepHighest>(source, period, dst);

        /// <summary>The population standard deviation of every full window of <paramref name="period"/> values about its mean, the
        /// mean each window already has (<see cref="MomentsExtensions"/>).</summary>
        /// <param name="period">The width of a window.</param>
        /// <param name="means">The mean of every full window (<see cref="RollingMean"/>).</param>
        /// <param name="dst">One value per window, as many as <paramref name="means"/>.</param>
        internal void RollingStdDev(int period, ReadOnlySpan<double> means, Span<double> dst)
        {
            for (int i = 0; i < means.Length; i++) dst[i] = source.Slice(i, period).StdDev(means[i]);
        }
    }

    /// <summary>The extreme of every window ending at each index: <see cref="RollingExtremeStep{TKeep}"/> folded over the values,
    /// the lowest and the highest being the one walk with the comparison <typeparamref name="TKeep"/> names. A window longer than
    /// the values is as long as they are.</summary>
    private static void RollingExtreme<TKeep>(ReadOnlySpan<double> source, int period, Span<double> dst) where TKeep : struct, IExtremeKeep
    {
        int n = source.Length;
        if (n == 0) return;

        var step = new RollingExtremeStep<TKeep>(Math.Min(Math.Max(period, 1), n));
        for (int i = 0; i < n; i++) dst[i] = step.Next(source[i]);
    }
}
