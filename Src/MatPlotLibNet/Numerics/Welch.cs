// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;

namespace MatPlotLibNet.Numerics;

/// <summary>A one-sided power spectrum: the power at each frequency.</summary>
/// <param name="Frequencies">The frequency of each bin, k · SampleRate / M + CenterFrequency for k = 0 .. M / 2, where M is
/// the length of the transform.</param>
/// <param name="Power">The averaged power (or the coherence) at each frequency; every value is NaN when no segment could be
/// used.</param>
internal readonly record struct SpectralDensity(double[] Frequencies, double[] Power);

/// <summary>A one-sided cross spectrum: the complex cross power of two signals at each frequency.</summary>
/// <param name="Frequencies">The frequency of each bin, as in <see cref="SpectralDensity.Frequencies"/>.</param>
/// <param name="Power">The averaged conj(X)·Y at each frequency; every value is NaN when no segment could be used.</param>
internal readonly record struct CrossSpectralDensity(double[] Frequencies, Complex[] Power);

/// <summary>Welch's method of averaged periodograms, as matplotlib's <c>mlab.psd</c>, <c>mlab.csd</c> and <c>mlab.cohere</c>
/// compute it. The signal is cut into K = ⌊(N − L) / (L − O)⌋ + 1 segments of L = <see cref="SpectralOptions.SegmentLength"/>
/// samples, O = <see cref="SpectralOptions.Overlap"/> of them shared by neighbours (what is left at the end is not used).
/// Each segment is detrended, multiplied by the window, padded with zeros to M = <see cref="SpectralOptions.PadTo"/> (or L)
/// and transformed at exactly that length; conj(X)·Y is averaged over the segments, divided by SampleRate · Σw² (a density)
/// or (Σw)² (a spectrum), and folded to one side: every bin but DC, and but the Nyquist bin of an even M, is doubled.
/// <para>A missing sample (NaN or an infinity) makes its segment missing, and a missing segment is left out of the average;
/// for two signals a segment is left out when either signal is missing there. When no segment is left every value of the
/// result is NaN, and the frequencies are still given.</para></summary>
internal static class Welch
{
    /// <summary>The power spectral density of <paramref name="x"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="x"/> is empty, or <paramref name="options"/> asks for a segment longer
    /// than <paramref name="x"/>, an overlap that is not below the segment, or a padding shorter than the segment.</exception>
    internal static SpectralDensity Psd(double[] x, SpectralOptions options)
    {
        Averages averages = Average(x, x, options);
        return new(averages.Frequencies, averages.Left);
    }

    /// <summary>The cross spectral density of <paramref name="x"/> and <paramref name="y"/>, conj(X)·Y averaged.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="y"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A signal is empty, the two differ in length, or the options do not fit them (see
    /// <see cref="Psd"/>).</exception>
    internal static CrossSpectralDensity Csd(double[] x, double[] y, SpectralOptions options)
    {
        Averages averages = Average(x, y, options);
        return new(averages.Frequencies, averages.Cross);
    }

    /// <summary>The magnitude-squared coherence of <paramref name="x"/> and <paramref name="y"/>, |Pxy|² / (Pxx · Pyy),
    /// between 0 and 1; all three are averaged over the same segments.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="y"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">As for <see cref="Csd"/>.</exception>
    internal static SpectralDensity Coherence(double[] x, double[] y, SpectralOptions options)
    {
        Averages averages = Average(x, y, options);
        var coherence = new double[averages.Cross.Length];
        for (int k = 0; k < coherence.Length; k++)
            coherence[k] = Squared(averages.Cross[k]) / (averages.Left[k] * averages.Right[k]);

        return new(averages.Frequencies, coherence);
    }

    /// <summary>conj(X)·Y, |X|² and |Y|² averaged over the segments both signals hold whole, scaled and folded to one side.</summary>
    private static Averages Average(double[] x, double[] y, SpectralOptions options)
    {
        Refuse(x, y, options);

        int length = options.SegmentLength;
        int step = length - options.Overlap;
        int m = options.PadTo == 0 ? length : options.PadTo;
        int bins = m / 2 + 1;
        double[] window = options.Window.Coefficients(length);

        var cross = new Complex[bins];
        var left = new double[bins];
        var right = new double[bins];
        int used = 0;
        for (int start = 0; start + length <= x.Length; start += step)
        {
            if (!IsWhole(x, start, length) || !IsWhole(y, start, length)) continue;

            Complex[] xs = Spectrum(x, start, m, window, options.Detrend);
            Complex[] ys = ReferenceEquals(x, y) ? xs : Spectrum(y, start, m, window, options.Detrend);
            for (int k = 0; k < bins; k++)
            {
                cross[k] += Complex.Conjugate(xs[k]) * ys[k];
                left[k] += Squared(xs[k]);
                right[k] += Squared(ys[k]);
            }

            used++;
        }

        double scale = used == 0 ? double.NaN : 1.0 / (used * Normalisation(window, options));
        for (int k = 0; k < bins; k++)
        {
            double factor = scale * (k == 0 || (m % 2 == 0 && k == m / 2) ? 1 : 2);
            cross[k] *= factor;
            left[k] *= factor;
            right[k] *= factor;
        }

        var frequencies = new double[bins];
        for (int k = 0; k < bins; k++) frequencies[k] = k * options.SampleRate / m + options.CenterFrequency;

        return new(frequencies, cross, left, right);
    }

    /// <summary>What the averaged power is divided by: SampleRate · Σw² for a density, (Σw)² for a spectrum.</summary>
    private static double Normalisation(double[] window, SpectralOptions options)
    {
        if (options.Scaling == SpectralScaling.Spectrum)
        {
            double sum = window.Sum();
            return sum * sum;
        }

        return options.SampleRate * window.Sum(w => w * w);
    }

    /// <summary>The transform of the segment at <paramref name="start"/>: detrended, weighted, padded to <paramref name="m"/>.</summary>
    private static Complex[] Spectrum(double[] signal, int start, int m, double[] window, SpectralDetrend detrend)
    {
        double[] segment = detrend.Removed(signal[start..(start + window.Length)]);
        var buffer = new Complex[m];
        for (int i = 0; i < window.Length; i++) buffer[i] = segment[i] * window[i];
        return Fft.Transform(buffer);
    }

    private static double Squared(Complex value) => value.Real * value.Real + value.Imaginary * value.Imaginary;

    private static bool IsWhole(double[] signal, int start, int length)
    {
        for (int i = start; i < start + length; i++)
        {
            if (!double.IsFinite(signal[i])) return false;
        }

        return true;
    }

    /// <summary>Refuses, by the parameter's name, signals and options no spectrum can be computed from.</summary>
    private static void Refuse(double[] x, double[] y, SpectralOptions options)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Length == 0) throw new ArgumentException("The signal holds no samples.", nameof(x));
        if (y.Length != x.Length)
        {
            throw new ArgumentException(
                $"The two signals differ in length: {Shown(x.Length)} and {Shown(y.Length)} samples.", nameof(y));
        }

        if (options.SegmentLength > x.Length)
        {
            throw new ArgumentException(
                $"A segment of {Shown(options.SegmentLength)} samples is longer than the signal of {Shown(x.Length)}; set a shorter SegmentLength.",
                nameof(options));
        }

        if (options.Overlap >= options.SegmentLength)
        {
            throw new ArgumentException(
                $"An overlap of {Shown(options.Overlap)} samples is not below the segment of {Shown(options.SegmentLength)}.",
                nameof(options));
        }

        if (options.PadTo != 0 && options.PadTo < options.SegmentLength)
        {
            throw new ArgumentException(
                $"A segment of {Shown(options.SegmentLength)} samples cannot be padded to {Shown(options.PadTo)}.",
                nameof(options));
        }
    }

    private static string Shown(int count) => count.ToString(CultureInfo.InvariantCulture);

    /// <summary>The scaled one-sided averages of one pass over the segments.</summary>
    private readonly record struct Averages(double[] Frequencies, Complex[] Cross, double[] Left, double[] Right);
}
