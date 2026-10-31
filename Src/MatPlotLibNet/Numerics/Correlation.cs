// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Numerics;

/// <summary>A correlation at each lag.</summary>
/// <param name="Lags">The lags, −MaxLags .. MaxLags, as whole numbers.</param>
/// <param name="Values">The correlation at each lag; every value is NaN when a sample is missing, and so is every value of a
/// normalised correlation of a signal with no energy.</param>
internal readonly record struct Correlogram(double[] Lags, double[] Values);

/// <summary>The cross-correlation of two signals, as matplotlib's <c>Axes.xcorr</c> works it out: the value at lag k is the sum
/// over n of x[n + k] · y[n] (numpy's <c>correlate</c> in "full" mode, so a positive lag is x delayed against y), for the lags
/// −<see cref="CorrelationOptions.MaxLags"/> .. <see cref="CorrelationOptions.MaxLags"/>, both signals detrended first, and each
/// sum divided by the root of the energy of x times the root of the energy of y unless the options ask for the raw sums.
/// <para>A missing sample (NaN or an infinity) in either signal makes every lag missing: the lags are given and every value is
/// NaN. A normalised correlation of a signal with no energy (all zero, or nothing but its trend) is NaN at every lag, a gap and
/// never a zero.</para></summary>
internal static class Correlation
{
    /// <summary>The correlation of <paramref name="signal"/> with itself.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="signal"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="signal"/> is empty, or <paramref name="options"/> asks for as many lags
    /// as the signal has samples or more.</exception>
    internal static Correlogram Auto(double[] signal, CorrelationOptions options) =>
        Of(signal, signal, options, nameof(signal), nameof(signal));

    /// <summary>The correlation of <paramref name="x"/> with <paramref name="y"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="y"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="x"/> is empty, <paramref name="y"/> differs from it in length, or
    /// <paramref name="options"/> asks for as many lags as the signals have samples or more.</exception>
    internal static Correlogram Cross(double[] x, double[] y, CorrelationOptions options) =>
        Of(x, y, options, nameof(x), nameof(y));

    private static Correlogram Of(double[] x, double[] y, CorrelationOptions options, string xName, string yName)
    {
        ArgumentNullException.ThrowIfNull(x, xName);
        ArgumentNullException.ThrowIfNull(y, yName);
        if (x.Length == 0)
        {
            throw new ArgumentException("The signal holds no samples; a correlation is worked out from at least two.", xName);
        }

        if (y.Length != x.Length)
        {
            throw new ArgumentException($"The two signals differ in length: {Shown(x.Length)} and {Shown(y.Length)} samples.", yName);
        }

        int maxLags = options.MaxLags;
        if (maxLags >= x.Length)
        {
            throw new ArgumentException(
                $"{Shown(maxLags)} lags either side need more than {Shown(maxLags)} samples, and the signal holds {Shown(x.Length)}; set MaxLags to at most {Shown(x.Length - 1)}.",
                nameof(options));
        }

        double[] lags = new double[2 * maxLags + 1];
        for (int i = 0; i < lags.Length; i++) lags[i] = i - maxLags;

        if (!x.All(double.IsFinite) || !y.All(double.IsFinite))
        {
            return new(lags, [.. Enumerable.Repeat(double.NaN, lags.Length)]);
        }

        double[] detrendedX = options.Detrend.Removed(x);
        double[] detrendedY = options.Detrend.Removed(y);

        // The roots are taken one by one: the product of two small energies can be below the smallest double when each is not. A
        // signal with no energy has no scale to divide by, and its normalised correlation is missing, not 0.
        double scale = options.Unnormalised ? 1 : Math.Sqrt(Energy(detrendedX)) * Math.Sqrt(Energy(detrendedY));

        double[] values = new double[lags.Length];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = scale > 0 ? Sum(detrendedX, detrendedY, i - maxLags) / scale : double.NaN;
        }

        return new(lags, values);
    }

    /// <summary>The sum over n of x[n + lag] · y[n], for every n that puts both samples inside the signals.</summary>
    private static double Sum(double[] x, double[] y, int lag)
    {
        double sum = 0;
        for (int n = Math.Max(0, -lag); n < Math.Min(x.Length, x.Length - lag); n++)
        {
            sum += x[n + lag] * y[n];
        }

        return sum;
    }

    /// <summary>The sum of the squares of the samples.</summary>
    private static double Energy(double[] signal)
    {
        double energy = 0;
        foreach (double sample in signal) energy += sample * sample;
        return energy;
    }

    private static string Shown(int count) => count.ToString(CultureInfo.InvariantCulture);
}
