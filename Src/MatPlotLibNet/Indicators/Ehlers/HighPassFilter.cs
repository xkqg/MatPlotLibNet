// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Ehlers;

/// <summary>Ehlers' one-pole high-pass filter. Reference: Ehlers (2014),
/// <i>Predictive and Successful Indicators</i>, S&amp;C magazine.</summary>
internal static class HighPassFilter
{
    /// <summary>Applies the one-pole HP filter with cutoff <paramref name="period"/>.
    /// Extension method — call as <c>signal.HighPass(period)</c>. The first two bars of
    /// output carry the corresponding input bars (recurrence needs <c>x_{t-2}</c>);
    /// from bar 2 onward the recurrence takes over.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> &lt; 2.</exception>
    public static double[] HighPass(this ReadOnlySpan<double> signal, int period)
    {
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        int n = signal.Length;
        if (n == 0) return Array.Empty<double>();

        double theta = DegreesToRadians(0.707 * 360.0 / period);
        double cosT = Math.Cos(theta);
        double sinT = Math.Sin(theta);
        double alpha = (cosT + sinT - 1) / cosT;
        double omA = 1 - alpha;
        double coefX = (1 - alpha / 2) * (1 - alpha / 2);
        double coefHp1 = 2 * omA;
        double coefHp2 = -(omA * omA);

        // Canonical HP init: first two output bars are 0 so constant input settles to
        // output ≡ 0. Carrying the input through would inject a step that leaks for
        // many bars via the IIR tail.
        var result = new double[n];

        for (int i = 2; i < n; i++)
        {
            result[i] = coefX * (signal[i] - 2 * signal[i - 1] + signal[i - 2])
                      + coefHp1 * result[i - 1]
                      + coefHp2 * result[i - 2];
        }
        return result;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
