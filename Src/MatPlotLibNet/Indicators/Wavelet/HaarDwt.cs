// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Wavelet;

/// <summary>Discrete Haar wavelet transform — single-pass decomposition at each level
/// produces approximation + detail coefficients; multi-level recurses on the approximation.
/// Shared infrastructure for <c>WaveletEnergyRatio</c> and <c>WaveletEntropy</c>.</summary>
internal static class HaarDwt
{
    /// <summary>Decomposes <paramref name="signal"/> into <paramref name="levels"/> detail
    /// bands plus a final approximation. Signal length must be a power of 2 and ≥ 2^levels.
    /// Empty signal returns empty arrays without throwing.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="levels"/> &lt; 1, the
    /// signal length is not a power of two, or is below 2^levels.</exception>
    public static DwtResult Decompose(ReadOnlySpan<double> signal, int levels)
    {
        if (levels < 1)
            throw new ArgumentException($"levels must be >= 1 (got {levels}).", nameof(levels));

        int n = signal.Length;
        if (n == 0)
            return new DwtResult(Array.Empty<double[]>(), Array.Empty<double>());

        if ((n & (n - 1)) != 0)
            throw new ArgumentException(
                $"signal length must be a power of 2 (got {n}).", nameof(signal));

        int required = 1 << levels;
        if (n < required)
            throw new ArgumentException(
                $"signal length must be >= 2^levels = {required} (got {n}).", nameof(signal));

        double invSqrt2 = 1.0 / Math.Sqrt(2);
        var details = new double[levels][];
        var current = signal.ToArray();
        int curLen = n;

        for (int k = 0; k < levels; k++)
        {
            int half = curLen / 2;
            var approx = new double[half];
            var detail = new double[half];
            for (int i = 0; i < half; i++)
            {
                double a = current[2 * i];
                double b = current[2 * i + 1];
                approx[i] = (a + b) * invSqrt2;
                detail[i] = (a - b) * invSqrt2;
            }
            details[k] = detail;
            current = approx;
            curLen = half;
        }
        return new DwtResult(details, current);
    }

    /// <summary>Returns per-level energy: sum of squared coefficients. Length =
    /// <paramref name="levels"/> + 1; indices 0..levels-1 hold detail-band energies,
    /// index <c>levels</c> holds approximation energy.</summary>
    public static double[] EnergyPerLevel(ReadOnlySpan<double> signal, int levels)
    {
        var (details, approx) = Decompose(signal, levels);
        if (details.Length == 0) return Array.Empty<double>();

        var energy = new double[levels + 1];
        for (int k = 0; k < levels; k++)
        {
            double sum = 0;
            var d = details[k];
            for (int i = 0; i < d.Length; i++) sum += d[i] * d[i];
            energy[k] = sum;
        }
        double approxSum = 0;
        for (int i = 0; i < approx.Length; i++) approxSum += approx[i] * approx[i];
        energy[levels] = approxSum;
        return energy;
    }
}
