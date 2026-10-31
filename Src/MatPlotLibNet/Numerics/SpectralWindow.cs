// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Numerics;

/// <summary>The weights a segment of a signal is multiplied by before its spectrum is taken, so the ends of the segment
/// fade to zero and a tone that does not fit a whole number of cycles leaks less into the bins next to it. The value you get
/// from <c>default</c> is <see cref="Hann"/>, the window matplotlib's spectral functions use when none is given.
/// <para>Make one with a factory: <see cref="Hann"/>, <see cref="Hamming"/>, <see cref="Blackman"/>,
/// <see cref="Rectangular"/> (no weighting), or <see cref="Of"/> for a window of your own. The named windows are the
/// symmetric ones numpy's <c>hanning</c>, <c>hamming</c> and <c>blackman</c> give: a window of <c>n</c> samples reaches its
/// ends at sample 0 and sample <c>n − 1</c>, and a window of one sample is <c>[1]</c>.</para></summary>
/// <example>
/// <code>
/// SpectralWindow window = SpectralWindow.Hamming;
/// SpectralWindow triangle = SpectralWindow.Of(n => [.. Enumerable.Range(0, n).Select(k => 1.0 - Math.Abs(k - (n - 1) / 2.0) / n)]);
/// </code>
/// </example>
public readonly record struct SpectralWindow
{
    private readonly Func<int, double[]>? _coefficients;

    private SpectralWindow(Func<int, double[]> coefficients) => _coefficients = coefficients;

    /// <summary>The Hann window, 0.5 − 0.5·cos(2πk/(n − 1)) (numpy's <c>hanning</c>). It is what <c>default</c> means.</summary>
    public static SpectralWindow Hann => default;

    /// <summary>The Hamming window, 0.54 − 0.46·cos(2πk/(n − 1)) (numpy's <c>hamming</c>): a Hann window raised so its ends
    /// stay at 0.08, which lowers the first side lobe.</summary>
    public static SpectralWindow Hamming => new(static n => Cosine(n, 0.54, 0.46, 0));

    /// <summary>The Blackman window, 0.42 − 0.5·cos(2πk/(n − 1)) + 0.08·cos(4πk/(n − 1)) (numpy's <c>blackman</c>): a wider
    /// main lobe than Hann, with side lobes far lower.</summary>
    public static SpectralWindow Blackman => new(static n => Cosine(n, 0.42, 0.5, 0.08));

    /// <summary>The rectangular window: every weight is 1, so the segment is taken as it is.</summary>
    public static SpectralWindow Rectangular => new(static n => Cosine(n, 1, 0, 0));

    /// <summary>A window of your own, given by the function that makes its weights for a segment of <c>n</c> samples.</summary>
    /// <param name="coefficients">The weights of a window of <c>n</c> samples (n at least 1): exactly <c>n</c> finite
    /// numbers. They are checked each time a window is made from them.</param>
    /// <returns>The window.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    public static SpectralWindow Of(Func<int, double[]> coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        return new(n => Checked(coefficients(n), n));
    }

    /// <summary>The weights of a window of <paramref name="n"/> samples.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is not at least 1.</exception>
    /// <exception cref="InvalidOperationException">A window from <see cref="Of"/> gave no weights, a different number than
    /// <paramref name="n"/>, or one that is not a finite number.</exception>
    internal double[] Coefficients(int n)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
        return _coefficients is null ? Cosine(n, 0.5, 0.5, 0) : _coefficients(n);
    }

    /// <summary>The symmetric cosine-sum window a0 − a1·cos(2πk/(n − 1)) + a2·cos(4πk/(n − 1)); one sample is [1].</summary>
    private static double[] Cosine(int n, double a0, double a1, double a2)
    {
        if (n == 1) return [1.0];

        var w = new double[n];
        for (int k = 0; k < n; k++)
        {
            double phase = 2 * Math.PI * k / (n - 1);
            w[k] = a0 - a1 * Math.Cos(phase) + a2 * Math.Cos(2 * phase);
        }

        return w;
    }

    private static double[] Checked(double[]? weights, int n)
    {
        if (weights is null || weights.Length != n)
        {
            throw new InvalidOperationException(
                $"The window function gave {(weights is null ? "no" : weights.Length.ToString(CultureInfo.InvariantCulture))} weights for a window of {n.ToString(CultureInfo.InvariantCulture)} samples.");
        }

        foreach (double weight in weights)
        {
            if (!double.IsFinite(weight))
            {
                throw new InvalidOperationException(
                    $"The window function gave a weight of {weight.ToString(CultureInfo.InvariantCulture)}; every weight is a finite number.");
            }
        }

        return weights;
    }
}
