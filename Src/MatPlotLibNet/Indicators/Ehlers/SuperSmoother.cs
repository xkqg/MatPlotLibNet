// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Ehlers;

/// <summary>Ehlers' two-pole Butterworth low-pass ("SuperSmoother"). Reference: Ehlers
/// (2013) <i>Cycle Analytics for Traders</i>, Ch. 3.</summary>
internal static class SuperSmoother
{
    extension(ReadOnlySpan<double> signal)
    {
        /// <summary>Applies the SuperSmoother filter with cutoff <paramref name="period"/>.
        /// Extension method — call as <c>signal.SuperSmooth(period)</c>. First 2 output bars
        /// equal the corresponding input bars.</summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> &lt; 2.</exception>
        public double[] SuperSmooth(int period)
        {
            if (period < 2)
                throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
            int n = signal.Length;
            if (n == 0) return Array.Empty<double>();

            var result = new double[n];
            var step = new SuperSmootherStep(period);
            for (int i = 0; i < n; i++) result[i] = step.Next(signal[i]);
            return result;
        }
    }
}
