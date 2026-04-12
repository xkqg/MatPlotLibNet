// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace MatPlotLibNet.Numerics;

public static partial class Fft
{
    /// <summary>Computes the inverse DFT from a complex spectrum, returning real-valued samples.
    /// Length of the returned array equals <c>spectrum.Length</c>.</summary>
    /// <param name="spectrum">Complex frequency-domain spectrum (length must be a power of 2).</param>
    /// <returns>Real-valued time-domain samples of the same length.</returns>
    public static double[] Inverse(Complex[] spectrum)
    {
        int n = spectrum.Length;
        if (n == 0) return [];

        // Conjugate, apply forward FFT, conjugate again, divide by N
        var x = new Complex[n];
        for (int i = 0; i < n; i++)
            x[i] = Complex.Conjugate(spectrum[i]);

        CooleyTukey(x);

        var result = new double[n];
        for (int i = 0; i < n; i++)
            result[i] = Complex.Conjugate(x[i]).Real / n;

        return result;
    }

    /// <summary>Returns the DFT sample frequencies for <paramref name="n"/> points and
    /// sample spacing <paramref name="dt"/> (in seconds). Follows NumPy's <c>fftfreq</c> convention:
    /// positive frequencies first, then negative.</summary>
    /// <param name="n">Number of samples (same as FFT size).</param>
    /// <param name="dt">Sample spacing in seconds (default 1.0 → frequencies in cycles/sample).</param>
    /// <returns>Frequency array of length <paramref name="n"/>.</returns>
    public static double[] Frequencies(int n, double dt = 1.0)
    {
        var freq = new double[n];
        int half = (n + 1) / 2;
        for (int i = 0; i < half; i++)
            freq[i] = i / (n * dt);
        for (int i = half; i < n; i++)
            freq[i] = (i - n) / (n * dt);
        return freq;
    }

    /// <summary>Shifts the zero-frequency component to the centre of a real array
    /// (equivalent to NumPy's <c>fftshift</c>).</summary>
    /// <param name="x">Real-valued array to shift.</param>
    /// <returns>New array with the zero-frequency element at position <c>n/2</c>.</returns>
    public static double[] Shift(double[] x)
    {
        int n = x.Length;
        int shift = n / 2;
        var result = new double[n];
        for (int i = 0; i < n; i++)
            result[(i + shift) % n] = x[i];
        return result;
    }

    /// <summary>Shifts the zero-frequency component to the centre of a complex spectrum.</summary>
    /// <param name="x">Complex spectrum to shift.</param>
    /// <returns>New array with the zero-frequency element at position <c>n/2</c>.</returns>
    public static Complex[] Shift(Complex[] x)
    {
        int n = x.Length;
        int shift = n / 2;
        var result = new Complex[n];
        for (int i = 0; i < n; i++)
            result[(i + shift) % n] = x[i];
        return result;
    }
}
