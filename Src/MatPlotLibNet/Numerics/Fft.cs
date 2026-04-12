// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace MatPlotLibNet.Numerics;

/// <summary>Short-time Fourier transform result containing magnitude, frequency, and time arrays.</summary>
/// <param name="Magnitudes">2D array of magnitudes: [frequencyBin, timeFrame].</param>
/// <param name="Frequencies">Frequency values in Hz for each row.</param>
/// <param name="Times">Center time values in seconds for each column.</param>
public sealed record StftResult(double[,] Magnitudes, double[] Frequencies, double[] Times);

/// <summary>Fast Fourier transform utilities using Cooley-Tukey radix-2 DIT algorithm.</summary>
public static class Fft
{
    /// <summary>Computes the forward DFT of a real-valued signal.</summary>
    /// <param name="signal">Input signal values.</param>
    /// <returns>Complex spectrum. Length is the next power of 2 ≥ signal.Length.</returns>
    public static Complex[] Forward(double[] signal)
    {
        if (signal.Length == 0) return [];
        int n = NextPowerOf2(signal.Length);
        var x = new Complex[n];
        for (int i = 0; i < signal.Length; i++) x[i] = signal[i];
        // Apply Hann window
        for (int i = 0; i < signal.Length; i++)
        {
            double w = 0.5 * (1.0 - Math.Cos(2 * Math.PI * i / (signal.Length - 1)));
            x[i] *= w;
        }
        CooleyTukey(x);
        return x;
    }

    /// <summary>Computes the Short-Time Fourier Transform using overlapping windows.</summary>
    /// <param name="signal">Input signal values.</param>
    /// <param name="windowSize">Number of samples per window (will be rounded to power of 2).</param>
    /// <param name="overlap">Number of overlapping samples between adjacent windows.</param>
    /// <param name="sampleRate">Sample rate in Hz used to compute frequency and time axes.</param>
    /// <returns>A <see cref="StftResult"/> with magnitude, frequency, and time arrays.</returns>
    public static StftResult Stft(double[] signal, int windowSize, int overlap, int sampleRate = 1)
    {
        if (signal.Length == 0 || windowSize <= 0)
            return new StftResult(new double[0, 0], [], []);

        int hopSize = Math.Max(1, windowSize - overlap);
        int nFrames = Math.Max(1, (signal.Length - windowSize) / hopSize + 1);
        int nBins = windowSize / 2 + 1;

        var magnitudes = new double[nBins, nFrames];
        var times = new double[nFrames];
        var frequencies = new double[nBins];

        for (int b = 0; b < nBins; b++)
            frequencies[b] = b * (double)sampleRate / windowSize;

        var window = new double[windowSize];
        for (int i = 0; i < windowSize; i++)
            window[i] = 0.5 * (1.0 - Math.Cos(2 * Math.PI * i / (windowSize - 1)));

        for (int f = 0; f < nFrames; f++)
        {
            int start = f * hopSize;
            var frame = new double[windowSize];
            for (int i = 0; i < windowSize && start + i < signal.Length; i++)
                frame[i] = signal[start + i] * window[i];

            var spectrum = Forward(frame);
            for (int b = 0; b < nBins; b++)
                magnitudes[b, f] = spectrum[b].Magnitude;

            times[f] = (start + windowSize / 2.0) / sampleRate;
        }

        return new StftResult(magnitudes, frequencies, times);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private static void CooleyTukey(Complex[] x)
    {
        int n = x.Length;
        if (n <= 1) return;

        // Bit-reversal permutation
        int bits = (int)Math.Log2(n);
        for (int i = 0; i < n; i++)
        {
            int rev = BitReverse(i, bits);
            if (rev > i) (x[i], x[rev]) = (x[rev], x[i]);
        }

        // Iterative Cooley-Tukey butterfly
        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2 * Math.PI / len;
            var wlen = new Complex(Math.Cos(angle), Math.Sin(angle));
            for (int i = 0; i < n; i += len)
            {
                Complex w = Complex.One;
                for (int j = 0; j < len / 2; j++)
                {
                    Complex u = x[i + j];
                    Complex v = x[i + j + len / 2] * w;
                    x[i + j] = u + v;
                    x[i + j + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }
    }

    private static int BitReverse(int n, int bits)
    {
        int rev = 0;
        for (int i = 0; i < bits; i++) { rev = (rev << 1) | (n & 1); n >>= 1; }
        return rev;
    }

    /// <summary>Returns the smallest power of two that is greater than or equal to <paramref name="n"/>.</summary>
    internal static int NextPowerOf2(int n)
    {
        if (n <= 0) return 1;
        if ((n & (n - 1)) == 0) return n;
        int p = 1;
        while (p < n) p <<= 1;
        return p;
    }
}
