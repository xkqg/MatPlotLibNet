// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;

namespace MatPlotLibNet.Numerics;

/// <summary>The one-sided spectrum of a whole signal: the transform of the weighted, padded signal at each frequency from 0 to
/// half the sample rate.</summary>
/// <param name="Frequencies">The frequency of each bin, k · SampleRate / M + CenterFrequency for k = 0 .. M / 2, where M is the
/// length of the transform.</param>
/// <param name="Bins">The transform X[k] of the weighted signal, not divided by anything; the first bin and the Nyquist bin of
/// an even M are real (their imaginary part is +0). Every bin is NaN when a sample is missing.</param>
/// <param name="WindowSum">The sum of the window's weights, what the magnitude is divided by.</param>
internal readonly record struct OneSidedSpectrum(double[] Frequencies, Complex[] Bins, double WindowSum)
{
    /// <summary>|X[k]| / Σw (matplotlib's <c>mode='magnitude'</c>): a constant signal has magnitude 1 at frequency 0.</summary>
    internal double[] Magnitudes()
    {
        double sum = WindowSum;
        return [.. Bins.Select(bin => Complex.Abs(bin) / sum)];
    }

    /// <summary>The angle of each bin, wrapped into (−π, π] (numpy's <c>angle</c>, matplotlib's <c>mode='angle'</c>).</summary>
    internal double[] Angles() => [.. Bins.Select(bin => bin.Phase)];

    /// <summary>The angle unwrapped along the frequencies (matplotlib's <c>mode='phase'</c>): see <see cref="Spectrum.Unwrap"/>.</summary>
    internal double[] Phases() => Spectrum.Unwrap(Angles());
}

/// <summary>The spectrum of a whole signal in one transform, as matplotlib's <c>mlab.magnitude_spectrum</c>,
/// <c>mlab.angle_spectrum</c> and <c>mlab.phase_spectrum</c> compute it: the signal is multiplied by the window of its own length,
/// padded with zeros to M = <see cref="SpectrumOptions.PadTo"/> (or its own length), transformed at exactly that length by
/// <see cref="Fft.Transform"/> and kept on one side, bins 0 to M / 2, none of them doubled.
/// <para>A missing sample (NaN or an infinity) makes the whole spectrum missing: every bin is NaN, and the frequencies are still
/// given.</para></summary>
internal static class Spectrum
{
    private const double TwoPi = 2 * Math.PI;

    /// <summary>The one-sided spectrum of <paramref name="signal"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="signal"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="signal"/> is empty, or <paramref name="options"/> asks for a padding
    /// shorter than the signal.</exception>
    internal static OneSidedSpectrum Of(double[] signal, SpectrumOptions options)
    {
        ArgumentNullException.ThrowIfNull(signal);
        if (signal.Length == 0)
        {
            throw new ArgumentException("The signal holds no samples; a spectrum is taken of at least one.", nameof(signal));
        }

        if (options.PadTo != 0 && options.PadTo < signal.Length)
        {
            throw new ArgumentException(
                $"A signal of {Shown(signal.Length)} samples cannot be padded to {Shown(options.PadTo)}; set PadTo to 0 or to at least {Shown(signal.Length)}.",
                nameof(options));
        }

        int m = options.PadTo == 0 ? signal.Length : options.PadTo;
        int count = m / 2 + 1;
        double[] window = options.Window.Coefficients(signal.Length);

        var frequencies = new double[count];
        for (int k = 0; k < count; k++) frequencies[k] = k * options.SampleRate / m + options.CenterFrequency;

        return new(frequencies, Transformed(signal, window, m, count), window.Sum());
    }

    /// <summary>The first <paramref name="count"/> bins of the weighted signal padded to <paramref name="m"/>, or NaN in every
    /// bin when a sample is missing. The first bin, and the Nyquist bin of an even <paramref name="m"/>, are the transform of a
    /// real signal there, which is real: written with an imaginary part of +0, their angle is 0 or +π, as numpy gives it, and
    /// never the −π a rounding error just below zero would make of it.</summary>
    private static Complex[] Transformed(double[] signal, double[] window, int m, int count)
    {
        if (!signal.All(double.IsFinite)) return [.. Enumerable.Repeat(new Complex(double.NaN, double.NaN), count)];

        var buffer = new Complex[m];
        for (int i = 0; i < signal.Length; i++) buffer[i] = signal[i] * window[i];

        Complex[] bins = Fft.Transform(buffer)[..count];
        bins[0] = new Complex(bins[0].Real, 0);
        if (m % 2 == 0) bins[^1] = new Complex(bins[^1].Real, 0);
        return bins;
    }

    /// <summary>Unwraps a run of angles as numpy's <c>unwrap</c> does (discontinuity π, period 2π): each step from one angle to
    /// the next is brought into [−π, π) by a whole number of turns, a step of exactly +π kept as +π, and the turns are added up
    /// along the run; a step whose size is below π is left as it is. A missing angle makes every angle after it missing.</summary>
    /// <param name="angles">The angles, in radians.</param>
    /// <returns>The unwrapped angles, as many as given.</returns>
    internal static double[] Unwrap(double[] angles)
    {
        var unwrapped = new double[angles.Length];
        double correction = 0;
        for (int k = 0; k < angles.Length; k++)
        {
            if (k > 0)
            {
                double step = angles[k] - angles[k - 1];
                double wrapped = FlooredRemainder(step + Math.PI) - Math.PI;
                if (wrapped == -Math.PI && step > 0) wrapped = Math.PI;
                if (!(Math.Abs(step) < Math.PI)) correction += wrapped - step;
            }

            unwrapped[k] = angles[k] + correction;
        }

        return unwrapped;
    }

    /// <summary>The remainder of <paramref name="value"/> by 2π with the sign of 2π (numpy's <c>mod</c>), in [0, 2π).</summary>
    private static double FlooredRemainder(double value)
    {
        double remainder = value % TwoPi;
        return remainder < 0 ? remainder + TwoPi : remainder;
    }

    private static string Shown(int count) => count.ToString(CultureInfo.InvariantCulture);
}
