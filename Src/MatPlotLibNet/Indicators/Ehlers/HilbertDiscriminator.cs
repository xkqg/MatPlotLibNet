// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Ehlers;

/// <summary>Ehlers' Hilbert transform + homodyne discriminator. Produces per-bar
/// dominant-cycle period, phase, and the in-phase / quadrature components used by
/// MAMA/FAMA, Sinewave, and Adaptive Stochastic. First 6 bars are warmup (zeros).
/// Reference: Ehlers (2001), <i>Rocket Science for Traders</i>, Ch. 15; canonical
/// cross-check: TA-Lib <c>ta_MAMA.c</c>.</summary>
internal static class HilbertDiscriminator
{
    extension(ReadOnlySpan<double> price)
    {
        /// <summary>Runs the full Hilbert + homodyne pipeline over <paramref name="price"/>.
        /// Extension method — call as <c>price.HilbertDiscriminate()</c>.</summary>
        /// <returns><see cref="HilbertResult"/> with parallel arrays of length <c>price.Length</c>;
        /// entries <c>[0..5]</c> are 0.</returns>
        public HilbertResult HilbertDiscriminate()
        {
            int n = price.Length;
            if (n == 0)
                return new HilbertResult(Array.Empty<double>(), Array.Empty<double>(),
                                         Array.Empty<double>(), Array.Empty<double>());

            var period = new double[n];
            var phase = new double[n];
            var i1 = new double[n];
            var q1 = new double[n];
            var step = new HilbertStep();
            for (int i = 0; i < n; i++)
            {
                var row = step.Next(price[i]);
                period[i] = row.Period;
                phase[i] = row.Phase;
                i1[i] = row.I1;
                q1[i] = row.Q1;
            }

            return new HilbertResult(period, phase, i1, q1);
        }
    }
}
