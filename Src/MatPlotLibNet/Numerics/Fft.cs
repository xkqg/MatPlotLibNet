// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace MatPlotLibNet.Numerics;

/// <summary>Short-time Fourier transform result containing magnitude, frequency, and time arrays.</summary>
/// <param name="Magnitudes">2D array of magnitudes: [frequencyBin, timeFrame].</param>
/// <param name="Frequencies">Frequency values in Hz for each row.</param>
/// <param name="Times">Center time values in seconds for each column.</param>
public sealed record StftResult(double[,] Magnitudes, double[] Frequencies, double[] Times);

/// <summary>Fast Fourier transforms: Cooley-Tukey radix-2 for power-of-two lengths and Bluestein's chirp-z transform for
/// any other length.</summary>
public static partial class Fft
{
    /// <summary>Computes the forward DFT of a real-valued signal after weighting it with a Hann window.
    /// The signal is multiplied by the symmetric Hann window of its own length (<see cref="SpectralWindow.Hann"/>, numpy's
    /// <c>hanning</c>; a one-sample signal is weighted by 1), then padded with zeros to the next power of two, then transformed.
    /// Bin <c>k</c> is therefore at <c>k / paddedLength</c> cycles per sample, not <c>k / signal.Length</c>.</summary>
    /// <param name="signal">Input signal values.</param>
    /// <returns>Complex spectrum of the windowed, zero-padded signal. Length is the next power of 2 ≥ signal.Length; an empty
    /// signal gives an empty spectrum.</returns>
    public static Complex[] Forward(double[] signal)
    {
        if (signal.Length == 0) return [];
        double[] window = SpectralWindow.Hann.Coefficients(signal.Length);
        var x = new Complex[NextPowerOf2(signal.Length)];
        for (int i = 0; i < signal.Length; i++) x[i] = signal[i] * window[i];
        return Transform(x);
    }

    /// <summary>The discrete Fourier transform of exactly <paramref name="x"/>'s length, X[k] = Σ x[j]·e^(−2πi·jk/N), with no
    /// window and no padding (numpy's <c>fft.fft</c>). A power-of-two length is transformed by radix-2 Cooley-Tukey; any
    /// other length by Bluestein's chirp-z transform, which rewrites the transform as a convolution of power-of-two length.
    /// The input is left as it was.</summary>
    /// <param name="x">The samples.</param>
    /// <returns>The N bins; an empty input gives an empty array and one sample gives a copy of it.</returns>
    internal static Complex[] Transform(Complex[] x)
    {
        int n = x.Length;
        if ((n & (n - 1)) == 0)
        {
            Complex[] result = [.. x];
            CooleyTukey(result);
            return result;
        }

        return Bluestein(x);
    }

    /// <summary>Computes the Short-Time Fourier Transform using overlapping windows. Each frame of
    /// <paramref name="windowSize"/> samples is weighted once by the symmetric Hann window of that length
    /// (<see cref="SpectralWindow.Hann"/>, as matplotlib's <c>specgram</c> does) and transformed at exactly that length, with
    /// no padding, so bin <c>b</c> is at <c>b · sampleRate / windowSize</c> Hz, for <c>b</c> from 0 to
    /// <c>windowSize / 2</c>. A frame that runs past the end of the signal is filled with zeros.</summary>
    /// <param name="signal">Input signal values.</param>
    /// <param name="windowSize">Number of samples per window, any length (it is not rounded to a power of two).</param>
    /// <param name="overlap">Number of overlapping samples between adjacent windows.</param>
    /// <param name="sampleRate">Sample rate in Hz used to compute frequency and time axes.</param>
    /// <returns>A <see cref="StftResult"/> with the magnitudes |X[b]| (<c>windowSize / 2 + 1</c> rows, one column per frame),
    /// the frequency of each row and the centre time of each frame; empty for an empty signal or a window of no samples.</returns>
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

        double[] window = SpectralWindow.Hann.Coefficients(windowSize);

        for (int f = 0; f < nFrames; f++)
        {
            int start = f * hopSize;
            var frame = new Complex[windowSize];
            for (int i = 0; i < windowSize && start + i < signal.Length; i++)
                frame[i] = signal[start + i] * window[i];

            var spectrum = Transform(frame);
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

    /// <summary>Bluestein's chirp-z transform: with the chirp w[k] = e^(−πi·k²/N), jk = (k² + j² − (k − j)²)/2 turns the
    /// transform into X[k] = w[k]·Σ (x[j]·w[j])·conj(w[k − j]), a convolution computed by power-of-two transforms of length
    /// M ≥ 2N − 1. The chirp phase is taken from k² mod 2N, so it stays below 2π however long the signal is.</summary>
    private static Complex[] Bluestein(Complex[] x)
    {
        int n = x.Length;
        int m = NextPowerOf2(2 * n - 1);

        var chirp = new Complex[n];
        long period = 2L * n;
        for (int k = 0; k < n; k++)
        {
            double angle = -Math.PI * ((long)k * k % period) / n;
            chirp[k] = new Complex(Math.Cos(angle), Math.Sin(angle));
        }

        var a = new Complex[m];
        var b = new Complex[m];
        for (int k = 0; k < n; k++)
        {
            a[k] = x[k] * chirp[k];
            b[k] = Complex.Conjugate(chirp[k]);
        }

        for (int k = 1; k < n; k++) b[m - k] = b[k];

        CooleyTukey(a);
        CooleyTukey(b);

        // The inverse transform of a·b by the conjugate trick: conj(FFT(conj(a·b))) / M.
        for (int k = 0; k < m; k++) a[k] = Complex.Conjugate(a[k] * b[k]);
        CooleyTukey(a);

        var result = new Complex[n];
        for (int k = 0; k < n; k++) result[k] = chirp[k] * Complex.Conjugate(a[k]) / m;
        return result;
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
