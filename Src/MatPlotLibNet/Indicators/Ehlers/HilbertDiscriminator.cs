// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Ehlers;

/// <summary>Ehlers' Hilbert transform + homodyne discriminator. Produces per-bar
/// dominant-cycle period, phase, and the in-phase / quadrature components used by
/// MAMA/FAMA, Sinewave, and Adaptive Stochastic. First 6 bars are warmup (zeros).
/// Reference: Ehlers (2001), <i>Rocket Science for Traders</i>, Ch. 15; canonical
/// cross-check: TA-Lib <c>ta_MAMA.c</c>.</summary>
internal static class HilbertDiscriminator
{
    /// <summary>Runs the full Hilbert + homodyne pipeline over <paramref name="price"/>.
    /// Extension method — call as <c>price.HilbertDiscriminate()</c>.</summary>
    /// <returns><see cref="HilbertResult"/> with parallel arrays of length <c>price.Length</c>;
    /// entries <c>[0..5]</c> are 0.</returns>
    public static HilbertResult HilbertDiscriminate(this ReadOnlySpan<double> price)
    {
        int n = price.Length;
        if (n == 0)
            return new HilbertResult(Array.Empty<double>(), Array.Empty<double>(),
                                     Array.Empty<double>(), Array.Empty<double>());

        var smooth = new double[n];
        var detrender = new double[n];
        var i1 = new double[n];
        var q1 = new double[n];
        var i2 = new double[n];
        var q2 = new double[n];
        var re = new double[n];
        var im = new double[n];
        var period = new double[n];
        var phase = new double[n];

        for (int i = 0; i < n; i++)
        {
            smooth[i] = i >= 3
                ? (4 * price[i] + 3 * price[i - 1] + 2 * price[i - 2] + price[i - 3]) / 10.0
                : price[i];

            if (i < 6) continue;

            double adjPrev = 0.075 * period[i - 1] + 0.54;

            detrender[i] = (0.0962 * smooth[i] + 0.5769 * smooth[i - 2]
                          - 0.5769 * smooth[i - 4] - 0.0962 * smooth[i - 6]) * adjPrev;

            q1[i] = (0.0962 * detrender[i] + 0.5769 * detrender[i - 2]
                   - 0.5769 * detrender[i - 4] - 0.0962 * detrender[i - 6]) * adjPrev;
            i1[i] = detrender[i - 3];

            double jI = (0.0962 * i1[i] + 0.5769 * i1[i - 2]
                       - 0.5769 * i1[i - 4] - 0.0962 * i1[i - 6]) * adjPrev;
            double jQ = (0.0962 * q1[i] + 0.5769 * q1[i - 2]
                       - 0.5769 * q1[i - 4] - 0.0962 * q1[i - 6]) * adjPrev;

            double i2Raw = i1[i] - jQ;
            double q2Raw = q1[i] + jI;
            i2[i] = 0.2 * i2Raw + 0.8 * i2[i - 1];
            q2[i] = 0.2 * q2Raw + 0.8 * q2[i - 1];

            double reRaw = i2[i] * i2[i - 1] + q2[i] * q2[i - 1];
            double imRaw = i2[i] * q2[i - 1] - q2[i] * i2[i - 1];
            re[i] = 0.2 * reRaw + 0.8 * re[i - 1];
            im[i] = 0.2 * imRaw + 0.8 * im[i - 1];

            double pNew;
            if (im[i] != 0 && re[i] != 0)
                pNew = 2.0 * Math.PI / Math.Atan(im[i] / re[i]);
            else
                pNew = period[i - 1];

            if (pNew > 1.5 * period[i - 1]) pNew = 1.5 * period[i - 1];
            if (pNew < 0.67 * period[i - 1]) pNew = 0.67 * period[i - 1];
            if (pNew < 6) pNew = 6;
            if (pNew > 50) pNew = 50;
            period[i] = 0.2 * pNew + 0.8 * period[i - 1];

            if (q1[i] != 0)
                phase[i] = (180.0 / Math.PI) * Math.Atan(i1[i] / q1[i]);
            else
                phase[i] = phase[i - 1];
        }

        return new HilbertResult(period, phase, i1, q1);
    }
}
