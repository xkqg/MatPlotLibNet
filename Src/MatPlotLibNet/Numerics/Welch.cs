// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace MatPlotLibNet.Numerics;

/// <summary>A power spectrum: the power at each frequency.</summary>
/// <param name="Frequencies">The frequency of each bin, k · SampleRate / M + CenterFrequency, for k = 0 .. M / 2 one-sided and for
/// k = -M / 2 .. M / 2 - 1 (an even M) two-sided, where M is the length of the transform.</param>
/// <param name="Power">The averaged power (or the coherence) at each frequency; every value is NaN when no segment could be
/// used.</param>
internal readonly record struct SpectralDensity(double[] Frequencies, double[] Power);

/// <summary>A cross spectrum: the complex cross power of two signals at each frequency.</summary>
/// <param name="Frequencies">The frequency of each bin, as in <see cref="SpectralDensity.Frequencies"/>.</param>
/// <param name="Power">The averaged conj(X)·Y at each frequency; every value is NaN when no segment could be used.</param>
internal readonly record struct CrossSpectralDensity(double[] Frequencies, Complex[] Power);

/// <summary>Welch's method of averaged periodograms, as matplotlib's <c>mlab.psd</c>, <c>mlab.csd</c> and <c>mlab.cohere</c>
/// compute it. The signal is cut into K = ⌊(N − L) / (L − O)⌋ + 1 segments of L = <see cref="SpectralOptions.SegmentLength"/>
/// samples, O = <see cref="SpectralOptions.Overlap"/> of them shared by neighbours (what is left at the end is not used).
/// Each segment is detrended, multiplied by the window, padded with zeros to M = <see cref="SpectralOptions.PadTo"/> (or L)
/// and transformed at exactly that length; conj(X)·Y is averaged over the segments, divided by SampleRate · Σw² (a density)
/// or (Σw)² (a spectrum). One-sided (<see cref="SpectralSides"/>) it is folded: every bin but DC, and but the Nyquist bin of an
/// even M, is doubled. Two-sided nothing is doubled and the bins stand from the most negative frequency up, the negative ones the
/// conjugates of the positive (<see cref="SpectralSidesExtensions"/>).
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

    /// <summary>conj(X)·Y, |X|² and |Y|² averaged over the segments both signals hold whole, scaled, and placed one-sided or
    /// two-sided as the options say.</summary>
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

        double scale = used == 0 ? double.NaN : 1.0 / (used * options.Scaling.DivisorOf(window, options.SampleRate));
        for (int k = 0; k < bins; k++)
        {
            double factor = scale * options.Sides.FoldOf(k, m);
            cross[k] *= factor;
            left[k] *= factor;
            right[k] *= factor;
        }

        SpectralSides sides = options.Sides;
        return new(
            sides.FrequenciesOf(m, options.SampleRate, options.CenterFrequency),
            sides.Arranged(cross, m),
            sides.Arranged(left, m),
            sides.Arranged(right, m));
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
        x.ThrowIfEmpty("The signal holds no samples.");
        y.Length.ThrowIfMisaligned(x.Length, nameof(y));

        x.Length.ThrowIfFewerThan(options.SegmentLength, nameof(options), new("The signal", "sample", "a segment of SegmentLength", "set a shorter SegmentLength"));
        options.SegmentLength.ThrowIfFewerThan(options.Overlap + 1L, nameof(options), new("The segment", "sample", "an overlap of Overlap samples", "set a smaller Overlap"));
        if (options.PadTo != 0)
            options.SegmentLength.ThrowIfMoreThan(options.PadTo, nameof(options), new("The segment", "sample", "a padding to PadTo", "set PadTo to 0 or to at least SegmentLength"));
    }

    /// <summary>The scaled averages of one pass over the segments, in the order of the result.</summary>
    private readonly record struct Averages(double[] Frequencies, Complex[] Cross, double[] Left, double[] Right);
}
