// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>The exponents of the powers of ten a log locator lays (<see cref="LogLocator.DecadesLaid"/>): <paramref name="First"/>,
/// <paramref name="First"/> + <paramref name="Stride"/>, and so on, <paramref name="Count"/> of them.</summary>
/// <param name="First">The lowest exponent.</param>
/// <param name="Stride">The decades between two exponents, at least 1.</param>
/// <param name="Count">How many exponents there are.</param>
internal readonly record struct DecadeRun(int First, int Stride, int Count)
{
    /// <summary>The run of Python's <c>range(start, stop, stride)</c>.</summary>
    /// <param name="start">The first exponent.</param>
    /// <param name="stop">The exponent the run stops before.</param>
    /// <param name="stride">The step between two exponents.</param>
    /// <returns>The run.</returns>
    internal static DecadeRun From(int start, int stop, int stride) => new(start, stride, Math.Max(0, (stop - start + stride - 1) / stride));

    /// <summary>Every power of the run, ascending.</summary>
    /// <returns>The powers, ascending.</returns>
    internal double[] Powers()
    {
        var powers = new double[Count];
        for (int k = 0; k < Count; k++) powers[k] = Math.Pow(10, First + k * Stride);
        return powers;
    }

    /// <summary>The powers of the run from <paramref name="low"/> to <paramref name="high"/>, ascending.</summary>
    /// <param name="low">The lowest value kept.</param>
    /// <param name="high">The highest value kept.</param>
    /// <returns>The powers inside the span, ascending.</returns>
    internal double[] PowersBetween(double low, double high)
    {
        double[] powers = Powers();
        int from = 0, to = 0;
        for (int k = 0; k < powers.Length; k++)
        {
            if (powers[k] < low) from = k + 1;
            if (powers[k] <= high) to = k + 1;
        }

        return powers[from..Math.Max(from, to)];
    }
}
