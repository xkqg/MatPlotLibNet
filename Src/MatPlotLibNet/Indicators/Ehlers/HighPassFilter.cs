// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Ehlers;

/// <summary>Ehlers' one-pole high-pass filter. Reference: Ehlers (2014),
/// <i>Predictive and Successful Indicators</i>, S&amp;C magazine.</summary>
internal static class HighPassFilter
{
    extension(ReadOnlySpan<double> signal)
    {
        /// <summary>Applies the one-pole HP filter with cutoff <paramref name="period"/>.
        /// Extension method — call as <c>signal.HighPass(period)</c>. The first two bars of
        /// output carry the corresponding input bars (recurrence needs <c>x_{t-2}</c>);
        /// from bar 2 onward the recurrence takes over.</summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> &lt; 2.</exception>
        public double[] HighPass(int period)
        {
            if (period < 2)
                throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
            int n = signal.Length;
            if (n == 0) return Array.Empty<double>();

            // Canonical HP init: the first two output bars are 0 so a constant input settles to output 0.
            // Carrying the input through would inject a step that leaks for many bars via the IIR tail.
            var result = new double[n];
            var step = new HighPassStep(period);
            for (int i = 0; i < n; i++) result[i] = step.Next(signal[i]);
            return result;
        }
    }
}
