// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Ehlers;

/// <summary>Ehlers' two-pole Butterworth low-pass ("SuperSmoother"). Reference: Ehlers
/// (2013) <i>Cycle Analytics for Traders</i>, Ch. 3.</summary>
internal static class SuperSmoother
{
    /// <summary>Applies the SuperSmoother filter with cutoff <paramref name="period"/>.
    /// Extension method — call as <c>signal.SuperSmooth(period)</c>. First 2 output bars
    /// equal the corresponding input bars.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> &lt; 2.</exception>
    public static double[] SuperSmooth(this ReadOnlySpan<double> signal, int period)
    {
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        int n = signal.Length;
        if (n == 0) return Array.Empty<double>();

        double a1 = Math.Exp(-1.414 * Math.PI / period);
        double b1 = 2 * a1 * Math.Cos(DegreesToRadians(1.414 * 180.0 / period));
        double c2 = b1;
        double c3 = -a1 * a1;
        double c1 = 1 - c2 - c3;

        var result = new double[n];
        if (n >= 1) result[0] = signal[0];
        if (n >= 2) result[1] = signal[1];

        for (int i = 2; i < n; i++)
        {
            result[i] = c1 * (signal[i] + signal[i - 1]) / 2
                      + c2 * result[i - 1]
                      + c3 * result[i - 2];
        }
        return result;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
