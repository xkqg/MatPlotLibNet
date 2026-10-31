// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Charts of a signal: samples taken one after the other at a steady rate, handed over as a <c>double[]</c> in the
/// order they were taken, drawn as what the signal is made of rather than as the samples themselves. A signal is neither the
/// x and y arrays of <see cref="XYAxesExtensions"/> nor the sample of <see cref="DistributionAxesExtensions"/>, because its order
/// carries the meaning and the chart is computed from windows of it. How a spectrum is estimated from averaged segments is set
/// by one <see cref="SpectralOptions"/> value, and how the spectrum of the whole signal is taken in one transform by one
/// <see cref="SpectrumOptions"/> value, and how the signal is correlated with itself or with another, and drawn as stems or as
/// markers, by one <see cref="CorrelationOptions"/> value; the <c>default</c> of each holds matplotlib's defaults. Every verb here
/// composes a series the library already draws (one line, and with the stems of a correlation also a reference line) and sets no
/// axis limits; a spectrum verb titles its axes and a correlation verb titles none, as matplotlib's do. A value that is missing
/// is a gap: a sample that is not a finite number makes every segment that holds it missing, and when no segment is left, or
/// when a correlation or a spectrum of the whole signal is worked out from it, nothing is drawn.</summary>
public static class SignalAxesExtensions
{
    /// <summary>The title of the X axis of every spectrum: the unit is the caller's, since the sample rate is given in whatever
    /// unit of time the signal was sampled in.</summary>
    private const string FrequencyTitle = "Frequency";

    /// <summary>The title of the Y axis of the cross spectrum.</summary>
    private const string CrossSpectrumTitle = "Cross Spectrum Magnitude (dB)";

    /// <summary>The title of the Y axis of the coherence.</summary>
    private const string CoherenceTitle = "Coherence";

    /// <summary>The title of the Y axis of the magnitude spectrum on the linear scale (matplotlib's word for it).</summary>
    private const string LinearMagnitudeTitle = "Magnitude (energy)";

    /// <summary>The title of the Y axis of the magnitude spectrum in decibels.</summary>
    private const string DecibelMagnitudeTitle = "Magnitude (dB)";

    /// <summary>The title of the Y axis of the phase spectrum.</summary>
    private const string PhaseTitle = "Phase (radians)";

    /// <summary>The title of the Y axis of the angle spectrum.</summary>
    private const string AngleTitle = "Angle (radians)";

    /// <summary>The decibels a power is drawn in: ten times the logarithm to base ten of it.</summary>
    private const double DecibelsPerDecadeOfPower = 10;

    /// <summary>The decibels a magnitude (an amplitude, the square root of a power) is drawn in: twenty times the logarithm to
    /// base ten of it.</summary>
    private const double DecibelsPerDecadeOfMagnitude = 20;

    extension(AxesBuilder axes)
    {
        /// <summary>Draws the power spectral density of a signal (matplotlib's <c>psd</c>): how the power of the signal is spread
        /// over the frequencies, in decibels, so a reader sees at a glance which tones it holds and how loud each is.
        /// <para>The density is estimated by Welch's method: the signal is cut into segments of
        /// <see cref="SpectralOptions.SegmentLength"/> samples that start <see cref="SpectralOptions.SegmentLength"/> −
        /// <see cref="SpectralOptions.Overlap"/> samples apart, each segment is detrended, weighted by the window and transformed,
        /// and the powers of the segments are averaged, so the line is smoother than the spectrum of the whole signal and the
        /// noise in it falls with the number of segments. What is left at the end of the signal, less than a segment, is not used.
        /// With the options left out the defaults are matplotlib's: segments of 256 samples with no overlap, the Hann window, no
        /// detrend and a sample rate of 2, so the frequencies run from 0 to 1. A signal shorter than a segment is refused: give a
        /// shorter <see cref="SpectralOptions.SegmentLength"/>.</para>
        /// <para>The line is ONE <see cref="LineSeries"/> of the frequencies and ten times the logarithm to base ten of the
        /// density, wearing one colour of the theme's cycle. A frequency with no power at all (an exact zero) has no decibels:
        /// it is a gap in the line, never minus infinity. The X axis is titled "Frequency" and the Y axis "Power Spectral
        /// Density (dB/Hz)", or "(dB)" when the options ask for a spectrum instead of a density; a title of your own set after
        /// the call replaces either. The verb sets no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite makes every segment that holds it missing, and a missing segment is
        /// left out of the average. When every segment is missing there is nothing to estimate and nothing is drawn.</para>
        /// <para>The chart is its one line: written to JSON it is a line series whose missing points are the string "NaN", and
        /// read back it draws the same picture. It is not estimated again; change the signal or the options and call the verb
        /// again.</para></summary>
        /// <param name="signal">The samples, in the order they were taken, at least as many as one segment.</param>
        /// <param name="options">How the spectrum is estimated: the sample rate, the segment length and its overlap, the window,
        /// the detrend, the padding and the scaling. The default value holds matplotlib's defaults.</param>
        /// <param name="configure">Optional styling of the line, run once after the verb has built it, so what it sets wins: the
        /// colour, the width, the dash pattern, a label for the legend.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signal"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="signal"/> holds no samples; or <paramref name="options"/> asks for a
        /// segment longer than the signal, an overlap that is not below the segment, or a padding shorter than the segment (the
        /// message names the numbers).</exception>
        /// <example>
        /// <code>
        /// double[] signal = [.. Enumerable.Range(0, 1000).Select(i =>
        ///     Math.Sin(2 * Math.PI * 50 * i / 1000.0) + 0.4 * Math.Sin(2 * Math.PI * 120 * i / 1000.0))];
        /// var options = new SpectralOptions { SampleRate = 1000, SegmentLength = 200, Overlap = 100 };
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Two tones")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Psd(signal, options, line => line.Label = "vibration")
        ///         .SetXLabel("Frequency (Hz)")
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Psd(double[] signal, SpectralOptions options = default, Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(signal);
            if (signal.Length == 0)
            {
                throw new ArgumentException("The signal holds no samples; a spectrum is estimated from at least one segment of them.", nameof(signal));
            }

            // Estimated (and the options refused, by their own name) before the line is added, so a refused call leaves the axes as it was.
            SpectralDensity density = Welch.Psd(signal, options);
            string unit = options.Scaling == SpectralScaling.Spectrum ? "dB" : "dB/Hz";
            return axes.DrawSpectrum(density.Frequencies, density.Power.ToDecibels(DecibelsPerDecadeOfPower), $"Power Spectral Density ({unit})", configure);
        }

        /// <summary>Draws the cross spectral density of two signals (matplotlib's <c>csd</c>): how much power the two signals
        /// share at each frequency, as the size of their cross spectrum in decibels. A frequency at which both signals hold a
        /// tone, or one tone of which the other follows, stands out; the line says nothing of how well the two agree there, which
        /// is what <see cref="Cohere"/> draws.
        /// <para>The cross density is estimated by Welch's method, as <see cref="Psd"/> estimates a power density, with the
        /// segments of the two signals taken together: the product conj(X)·Y of the two transforms of a segment is averaged over
        /// the segments, and the line is ten times the logarithm to base ten of its MAGNITUDE (matplotlib's choice: the size, not
        /// the square of it). The cross density of a signal with itself is its power density. With the options left out the
        /// defaults are matplotlib's, as for <see cref="Psd"/>.</para>
        /// <para>The line is ONE <see cref="LineSeries"/> of the frequencies and the decibels, wearing one colour of the theme's
        /// cycle. A frequency with no cross power at all is a gap in the line, never minus infinity. The X axis is titled
        /// "Frequency" and the Y axis "Cross Spectrum Magnitude (dB)"; a title of your own set after the call replaces either.
        /// The verb sets no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite in either signal makes every segment that holds it missing, and a
        /// missing segment is left out of the average. When every segment is missing nothing is drawn.</para>
        /// <para>The chart is its one line: written to JSON it is a line series whose missing points are the string "NaN", and
        /// read back it draws the same picture. It is not estimated again; change the signals or the options and call the verb
        /// again.</para></summary>
        /// <param name="x">The samples of the first signal, in the order they were taken.</param>
        /// <param name="y">The samples of the second signal, one for each sample of <paramref name="x"/>.</param>
        /// <param name="options">How the spectrum is estimated: the sample rate, the segment length and its overlap, the window,
        /// the detrend, the padding and the scaling. The default value holds matplotlib's defaults.</param>
        /// <param name="configure">Optional styling of the line, run once after the verb has built it, so what it sets wins: the
        /// colour, the width, the dash pattern, a label for the legend.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="y"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> holds no samples; <paramref name="y"/> does not number as many
        /// as <paramref name="x"/> (the message names both counts); or <paramref name="options"/> asks for a segment longer than
        /// the signals, an overlap that is not below the segment, or a padding shorter than the segment.</exception>
        /// <example>
        /// <code>
        /// double[] input = [.. Enumerable.Range(0, 1000).Select(i => Math.Sin(2 * Math.PI * 50 * i / 1000.0))];
        /// double[] output = [.. Enumerable.Range(0, 1000).Select(i => 0.5 * Math.Sin(2 * Math.PI * 50 * (i - 3) / 1000.0) + 0.1 * Math.Sin(2 * Math.PI * 310 * i / 1000.0))];
        /// var options = new SpectralOptions { SampleRate = 1000, SegmentLength = 200, Overlap = 100 };
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("What the filter keeps")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Csd(input, output, options, line => line.Label = "input with output")
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Csd(double[] x, double[] y, SpectralOptions options = default, Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);

            CrossSpectralDensity cross = Welch.Csd(x, y, options);
            double[] magnitude = [.. cross.Power.Select(Complex.Abs)];
            return axes.DrawSpectrum(cross.Frequencies, magnitude.ToDecibels(DecibelsPerDecadeOfPower), CrossSpectrumTitle, configure);
        }

        /// <summary>Draws the coherence of two signals (matplotlib's <c>cohere</c>): at each frequency, how much of one signal
        /// is explained by the other as a straight-line (linear) relation, from 0 (none) to 1 (all of it), so a reader sees
        /// at which frequencies the two move together and where they do not.
        /// <para>The coherence is |Pxy|² / (Pxx · Pyy), the squared size of the cross density over the product of the two power
        /// densities, each estimated by Welch's method over the same segments (see <see cref="Psd"/>). It is drawn as it is, on
        /// the linear scale and not in decibels. A signal has coherence 1 with itself and with any scaled copy of itself. A
        /// frequency at which neither signal has any power has no coherence (0 / 0): it is a gap, never zero.</para>
        /// <para>The coherence of a single segment is 1 at every frequency, whatever the signals are, so the average needs at
        /// least two: a signal shorter than two segments is refused, as matplotlib refuses it. With the options left out the
        /// defaults are matplotlib's, segments of 256 samples, so the signals need at least 512.</para>
        /// <para>The line is ONE <see cref="LineSeries"/> of the frequencies and the coherence, wearing one colour of the
        /// theme's cycle. The X axis is titled "Frequency" and the Y axis "Coherence"; a title of your own set after the call
        /// replaces either. The verb sets no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite in either signal makes every segment that holds it missing, and a
        /// missing segment is left out of the average. When every segment is missing nothing is drawn.</para>
        /// <para>The chart is its one line: written to JSON it is a line series whose missing points are the string "NaN", and
        /// read back it draws the same picture. It is not estimated again; change the signals or the options and call the verb
        /// again.</para></summary>
        /// <param name="x">The samples of the first signal, in the order they were taken.</param>
        /// <param name="y">The samples of the second signal, one for each sample of <paramref name="x"/>.</param>
        /// <param name="options">How the spectra are estimated: the sample rate, the segment length and its overlap, the window,
        /// the detrend and the padding. The default value holds matplotlib's defaults.</param>
        /// <param name="configure">Optional styling of the line, run once after the verb has built it, so what it sets wins: the
        /// colour, the width, the dash pattern, a label for the legend.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="y"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> holds no samples; <paramref name="y"/> does not number as many
        /// as <paramref name="x"/> (the message names both counts); <paramref name="options"/> asks for a segment longer than the
        /// signals, an overlap that is not below the segment, or a padding shorter than the segment; or the signals are shorter
        /// than two segments (the message names the numbers).</exception>
        /// <example>
        /// <code>
        /// double[] input = [.. Enumerable.Range(0, 1000).Select(i => Math.Sin(2 * Math.PI * 50 * i / 1000.0) + 0.6 * Math.Sin(2 * Math.PI * 170 * i / 1000.0))];
        /// double[] output = [.. Enumerable.Range(0, 1000).Select(i => 0.5 * Math.Sin(2 * Math.PI * 50 * (i - 3) / 1000.0) + 0.3 * Math.Sin(2 * Math.PI * 310 * i / 1000.0))];
        /// var options = new SpectralOptions { SampleRate = 1000, SegmentLength = 200, Overlap = 100 };
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Where the output follows the input")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Cohere(input, output, options, line => line.Label = "coherence")
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Cohere(double[] x, double[] y, SpectralOptions options = default, Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);

            // Estimated first, so that a missing or empty signal, signals of other lengths and options that do not fit are refused by
            // the estimator, each by its own name; what is left to refuse is a signal that holds fewer than two segments.
            SpectralDensity coherence = Welch.Coherence(x, y, options);
            if (x.Length < 2 * options.SegmentLength)
            {
                throw new ArgumentException(
                    $"A coherence is averaged over at least two segments, and a signal of {Shown(x.Length)} samples does not hold two of {Shown(options.SegmentLength)}; set a shorter SegmentLength or give longer signals.",
                    nameof(options));
            }

            return axes.DrawSpectrum(coherence.Frequencies, coherence.Power, CoherenceTitle, configure);
        }

        /// <summary>Draws the magnitude spectrum of a signal (matplotlib's <c>magnitude_spectrum</c>): how large each frequency is
        /// in the whole signal, taken in one transform, so a reader sees which tones the signal holds and how strong each is.
        /// <para>The signal is multiplied by the window (Hann unless the options name another), padded with zeros to
        /// <see cref="SpectrumOptions.PadTo"/> samples when that is set, and transformed at exactly that length; the magnitude at
        /// each frequency is |X| divided by the sum of the window's weights, so a constant signal has magnitude 1 at frequency 0
        /// and a cosine that falls on a bin reads as half its amplitude there. The frequencies run from 0 to half the sample rate,
        /// as many as half the transform and one. Unlike <see cref="Psd"/> nothing is averaged: the whole signal is one segment,
        /// so the line shows every detail and every bit of noise. With the options left out the defaults are matplotlib's: the
        /// Hann window, no padding and a sample rate of 2, so the frequencies run from 0 to 1.</para>
        /// <para>The line is ONE <see cref="LineSeries"/> of the frequencies and the magnitudes, wearing one colour of the theme's
        /// cycle. In decibels (<see cref="MagnitudeScale.Decibel"/>) each magnitude is twenty times its logarithm to base ten, and
        /// a frequency with no magnitude at all (an exact zero) is a gap in the line, never minus infinity. The X axis is titled
        /// "Frequency" and the Y axis "Magnitude (energy)", or "Magnitude (dB)" in decibels; a title of your own set after the
        /// call replaces either. The verb sets no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite leaves the whole spectrum missing, since every frequency is computed
        /// from every sample: the frequencies are kept and nothing is drawn.</para>
        /// <para>The chart is its one line: written to JSON it is a line series whose missing points are the string "NaN", and
        /// read back it draws the same picture. It is not transformed again; change the signal or the options and call the verb
        /// again.</para></summary>
        /// <param name="signal">The samples, in the order they were taken, at least one.</param>
        /// <param name="options">How the spectrum is taken: the sample rate, the window, the padding and the frequency offset.
        /// The default value holds matplotlib's defaults.</param>
        /// <param name="scale">The scale of the magnitudes: <see cref="MagnitudeScale.Linear"/> (the default) or
        /// <see cref="MagnitudeScale.Decibel"/>.</param>
        /// <param name="configure">Optional styling of the line, run once after the verb has built it, so what it sets wins: the
        /// colour, the width, the dash pattern, a label for the legend.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signal"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> is not a member of
        /// <see cref="MagnitudeScale"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="signal"/> holds no samples; or <paramref name="options"/> asks for a
        /// padding shorter than the signal (the message names the numbers).</exception>
        /// <example>
        /// <code>
        /// double[] signal = [.. Enumerable.Range(0, 1000).Select(i =>
        ///     Math.Sin(2 * Math.PI * 50 * i / 1000.0) + 0.4 * Math.Sin(2 * Math.PI * 120 * i / 1000.0))];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Two tones")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .MagnitudeSpectrum(signal, new SpectrumOptions { SampleRate = 1000 }, MagnitudeScale.Decibel, line => line.Label = "vibration")
        ///         .SetXLabel("Frequency (Hz)")
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder MagnitudeSpectrum(double[] signal, SpectrumOptions options = default, MagnitudeScale scale = MagnitudeScale.Linear, Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            if (!Enum.IsDefined(scale))
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "The scale is Linear or Decibel.");
            }

            // Transformed (and the signal and the options refused, by their own names) before the line is added.
            OneSidedSpectrum spectrum = Spectrum.Of(signal, options);
            double[] magnitudes = spectrum.Magnitudes();
            return scale == MagnitudeScale.Decibel
                ? axes.DrawSpectrum(spectrum.Frequencies, magnitudes.ToDecibels(DecibelsPerDecadeOfMagnitude), DecibelMagnitudeTitle, configure)
                : axes.DrawSpectrum(spectrum.Frequencies, magnitudes, LinearMagnitudeTitle, configure);
        }

        /// <summary>Draws the phase spectrum of a signal (matplotlib's <c>phase_spectrum</c>): the phase of each frequency in the
        /// whole signal, UNWRAPPED along the frequencies, so a delay reads as a straight line falling with the frequency (its slope
        /// is the delay) instead of a saw that jumps back by a full turn each time it passes −π.
        /// <para>The signal is weighted, padded and transformed as <see cref="MagnitudeSpectrum"/> does. The angle of each bin is
        /// taken (between −π and π, as <see cref="AngleSpectrum"/> draws it) and unwrapped as numpy's <c>unwrap</c> does: wherever
        /// the angle steps by π or more from one frequency to the next, whole turns of 2π are added to bring the step within π,
        /// and those turns are carried on to every frequency after it. The phase of a frequency with no magnitude means nothing,
        /// as in matplotlib. With the options left out the defaults are matplotlib's (see <see cref="SpectrumOptions"/>).</para>
        /// <para>The line is ONE <see cref="LineSeries"/> of the frequencies and the phase in radians, wearing one colour of the
        /// theme's cycle. The X axis is titled "Frequency" and the Y axis "Phase (radians)"; a title of your own set after the call
        /// replaces either. The verb sets no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite leaves the whole spectrum missing: the frequencies are kept and nothing
        /// is drawn.</para>
        /// <para>The chart is its one line: written to JSON it is a line series whose missing points are the string "NaN", and
        /// read back it draws the same picture.</para></summary>
        /// <param name="signal">The samples, in the order they were taken, at least one.</param>
        /// <param name="options">How the spectrum is taken: the sample rate, the window, the padding and the frequency offset.
        /// The default value holds matplotlib's defaults.</param>
        /// <param name="configure">Optional styling of the line, run once after the verb has built it, so what it sets wins: the
        /// colour, the width, the dash pattern, a label for the legend.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signal"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="signal"/> holds no samples; or <paramref name="options"/> asks for a
        /// padding shorter than the signal (the message names the numbers).</exception>
        /// <example>
        /// <code>
        /// double[] echo = [.. Enumerable.Range(0, 256).Select(i => i == 10 ? 1.0 : i == 40 ? 0.5 : 0.0)];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("A delay is a straight line")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .PhaseSpectrum(echo, new SpectrumOptions { SampleRate = 1000, Window = SpectralWindow.Rectangular })
        ///         .SetXLabel("Frequency (Hz)"))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder PhaseSpectrum(double[] signal, SpectrumOptions options = default, Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);

            OneSidedSpectrum spectrum = Spectrum.Of(signal, options);
            return axes.DrawSpectrum(spectrum.Frequencies, spectrum.Phases(), PhaseTitle, configure);
        }

        /// <summary>Draws the angle spectrum of a signal (matplotlib's <c>angle_spectrum</c>): the angle of each frequency in the
        /// whole signal, WRAPPED between −π and π as the transform gives it, so every value stays on one turn; to follow the phase
        /// across turns, draw <see cref="PhaseSpectrum"/>.
        /// <para>The signal is weighted, padded and transformed as <see cref="MagnitudeSpectrum"/> does, and the angle of each bin
        /// is taken as numpy's <c>angle</c> takes it, above −π and up to π: a bin that is real and negative has the angle +π, never
        /// −π (the bins at frequency 0 and, for a transform of even length, at half the sample rate, are always real). The angle of
        /// a frequency with no magnitude means nothing, as in matplotlib. With the options left out the defaults are matplotlib's
        /// (see <see cref="SpectrumOptions"/>).</para>
        /// <para>The line is ONE <see cref="LineSeries"/> of the frequencies and the angle in radians, wearing one colour of the
        /// theme's cycle. The X axis is titled "Frequency" and the Y axis "Angle (radians)"; a title of your own set after the call
        /// replaces either. The verb sets no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite leaves the whole spectrum missing: the frequencies are kept and nothing
        /// is drawn.</para>
        /// <para>The chart is its one line: written to JSON it is a line series whose missing points are the string "NaN", and
        /// read back it draws the same picture.</para></summary>
        /// <param name="signal">The samples, in the order they were taken, at least one.</param>
        /// <param name="options">How the spectrum is taken: the sample rate, the window, the padding and the frequency offset.
        /// The default value holds matplotlib's defaults.</param>
        /// <param name="configure">Optional styling of the line, run once after the verb has built it, so what it sets wins: the
        /// colour, the width, the dash pattern, a label for the legend.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signal"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="signal"/> holds no samples; or <paramref name="options"/> asks for a
        /// padding shorter than the signal (the message names the numbers).</exception>
        /// <example>
        /// <code>
        /// double[] signal = [.. Enumerable.Range(0, 200).Select(i => Math.Cos(2 * Math.PI * 25 * i / 200.0 + 0.8))];
        ///
        /// string svg = Plt.Create()
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .AngleSpectrum(signal, new SpectrumOptions { SampleRate = 200 }, line => line.Label = "angle")
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder AngleSpectrum(double[] signal, SpectrumOptions options = default, Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);

            OneSidedSpectrum spectrum = Spectrum.Of(signal, options);
            return axes.DrawSpectrum(spectrum.Frequencies, spectrum.Angles(), AngleTitle, configure);
        }

        /// <summary>Draws the correlation of a signal with itself (matplotlib's <c>acorr</c>): how much the signal resembles a copy
        /// of itself moved along by each number of samples (the lag), so a reader sees at which lags it repeats, how quickly it
        /// forgets itself, and whether anything but noise is in it.
        /// <para>The correlation at lag k is the sum over n of x[n + k] · x[n], for the lags −<see cref="CorrelationOptions.MaxLags"/>
        /// to <see cref="CorrelationOptions.MaxLags"/>, each divided by the energy of the signal (the sum of its squares) unless
        /// <see cref="CorrelationOptions.Unnormalised"/> asks for the raw sums. Normalised, lag 0 is exactly 1, no lag is beyond 1,
        /// and the chart is mirrored about lag 0. With the options left out the defaults are matplotlib's: ten lags either side,
        /// normalised, nothing taken out of the signal, and stems. The signal needs more samples than lags: with the default,
        /// at least eleven.</para>
        /// <para>The stems are ONE <see cref="LineSeries"/> of a segment from 0 to the correlation at each lag, parted from the
        /// next by a missing point so each is a polyline of its own, wearing one colour of the theme's cycle and named by one
        /// legend entry, and a reference line at 0 that takes no colour of its own (<see cref="CorrelationParts.Zero"/>). With
        /// <see cref="CorrelationOptions.Markers"/> the chart is instead ONE line of the lags and the correlations drawn as circles
        /// with no line between them and no reference line, as matplotlib draws it with <c>usevlines=False</c>: the stems and the
        /// markers are never drawn together. The verb titles neither axis and sets no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite makes every lag missing, since every lag is worked out from every
        /// sample: nothing is drawn but the reference line. A normalised correlation of a signal with no energy (all zero, or
        /// nothing but the trend the options take out) is missing as well, a gap and never a zero.</para>
        /// <para>The chart is its parts: written to JSON it is a line series whose missing points are the string "NaN" and a
        /// reference line, and read back it draws the same picture. It is not worked out again; change the signal or the options
        /// and call the verb again.</para></summary>
        /// <param name="signal">The samples, in the order they were taken, more of them than <see cref="CorrelationOptions.MaxLags"/>.</param>
        /// <param name="options">How the correlation is worked out and drawn: the number of lags, whether it is normalised, the
        /// detrend and whether it is drawn as markers. The default value holds matplotlib's defaults.</param>
        /// <param name="configure">Optional styling of the parts, run once after the verb has built them, so what it sets wins:
        /// the colour, the width and the legend label of <see cref="CorrelationParts.Line"/>, and the colour and dash pattern of
        /// <see cref="CorrelationParts.Zero"/>, which is <see langword="null"/> when the correlation is drawn as markers.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signal"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="signal"/> holds no samples; or <paramref name="options"/> asks for as
        /// many lags as the signal has samples or more (the message names the numbers).</exception>
        /// <example>
        /// <code>
        /// double[] signal = [.. Enumerable.Range(0, 200).Select(i =>
        ///     Math.Sin(2 * Math.PI * i / 25.0) + 0.3 * Math.Sin(2 * Math.PI * i / 7.0))];
        /// var options = new CorrelationOptions { MaxLags = 60 };
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("A signal that repeats every 25 samples")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Acorr(signal, options, parts => parts.Line.Label = "autocorrelation")
        ///         .SetXLabel("Lag (samples)")
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Acorr(double[] signal, CorrelationOptions options = default, Action<CorrelationParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);

            // Worked out (and the signal and the options refused, by their own names) before anything is added, so a refused call
            // leaves the axes as it was.
            return axes.DrawCorrelation(Correlation.Auto(signal, options), options, configure);
        }

        /// <summary>Draws the correlation of two signals (matplotlib's <c>xcorr</c>): how much one signal resembles the other moved
        /// along by each number of samples (the lag), so a reader sees by how much one runs behind the other, and how alike the
        /// two are once that is allowed for.
        /// <para>The correlation at lag k is the sum over n of x[n + k] · y[n], for the lags −<see cref="CorrelationOptions.MaxLags"/>
        /// to <see cref="CorrelationOptions.MaxLags"/>, each divided by the root of the energy of <paramref name="x"/> times the
        /// root of the energy of <paramref name="y"/> unless <see cref="CorrelationOptions.Unnormalised"/> asks for the raw sums.
        /// A positive lag is <paramref name="x"/> delayed against <paramref name="y"/>: when <paramref name="y"/> is
        /// <paramref name="x"/> some samples later, the chart peaks at the NEGATIVE lag of that many samples, and swapping the
        /// signals mirrors the chart about lag 0. With the options left out the defaults are matplotlib's: ten lags either
        /// side, normalised, nothing taken out of the signals, and stems.</para>
        /// <para>It is drawn as <see cref="Acorr"/> draws the correlation of a signal with itself: as stems (ONE
        /// <see cref="LineSeries"/> of a segment from 0 to the correlation at each lag, and a reference line at 0), or, with
        /// <see cref="CorrelationOptions.Markers"/>, as the lags and the correlations alone, drawn as circles. One colour, one
        /// legend entry, no axis titles and no axis limits.</para>
        /// <para>A sample that is missing (NaN) or infinite in either signal makes every lag missing: nothing is drawn but the
        /// reference line. A normalised correlation with a signal that has no energy is missing as well, a gap and never a
        /// zero.</para>
        /// <para>The chart is its parts: written to JSON it is a line series whose missing points are the string "NaN" and a
        /// reference line, and read back it draws the same picture. It is not worked out again; change the signals or the
        /// options and call the verb again.</para></summary>
        /// <param name="x">The samples of the first signal, in the order they were taken, more of them than
        /// <see cref="CorrelationOptions.MaxLags"/>.</param>
        /// <param name="y">The samples of the second signal, one for each sample of <paramref name="x"/>.</param>
        /// <param name="options">How the correlation is worked out and drawn: the number of lags, whether it is normalised, the
        /// detrend and whether it is drawn as markers. The default value holds matplotlib's defaults.</param>
        /// <param name="configure">Optional styling of the parts, run once after the verb has built them, so what it sets wins:
        /// the colour, the width and the legend label of <see cref="CorrelationParts.Line"/>, and the colour and dash pattern of
        /// <see cref="CorrelationParts.Zero"/>, which is <see langword="null"/> when the correlation is drawn as markers.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="y"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> holds no samples; <paramref name="y"/> does not number as many
        /// as <paramref name="x"/> (the message names both counts); or <paramref name="options"/> asks for as many lags as the
        /// signals have samples or more.</exception>
        /// <example>
        /// <code>
        /// double[] sent = [.. Enumerable.Range(0, 120).Select(i => i % 20 &lt; 3 ? 1.0 : 0.0)];
        /// double[] received = [.. Enumerable.Range(0, 120).Select(i => i >= 4 &amp;&amp; (i - 4) % 20 &lt; 3 ? 1.0 : 0.0)];
        /// var options = new CorrelationOptions { MaxLags = 12, Markers = true };
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("The echo runs 4 samples behind")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Xcorr(sent, received, options, parts => parts.Line.Label = "sent against received")
        ///         .SetXLabel("Lag (samples)")
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Xcorr(double[] x, double[] y, CorrelationOptions options = default, Action<CorrelationParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);

            return axes.DrawCorrelation(Correlation.Cross(x, y, options), options, configure);
        }

        /// <summary>The one line of a spectrum, with the titles of its axes.</summary>
        private AxesBuilder DrawSpectrum(double[] frequencies, double[] values, string yTitle, Action<LineSeries>? configure) =>
            axes.AddSeries(new LineSeries(frequencies, values), configure).SetXLabel(FrequencyTitle).SetYLabel(yTitle);

        /// <summary>The line of a correlation, either as the stems (the segments from 0, and the reference line at 0) or as markers
        /// (the points alone), and the parts handed to the callback. The two forms are never drawn together.</summary>
        private AxesBuilder DrawCorrelation(Correlogram correlogram, CorrelationOptions options, Action<CorrelationParts>? configure)
        {
            LineSeries line;
            ReferenceLine? zero = null;
            if (options.Markers)
            {
                line = new LineSeries(correlogram.Lags, correlogram.Values) { Marker = MarkerStyle.Circle, LineStyle = LineStyle.None };
                axes.AddSeries(line);
            }
            else
            {
                var stems = SegmentPath.Of(correlogram.Lags, [0], correlogram.Lags, correlogram.Values);
                line = new LineSeries(stems.X, stems.Y);
                axes.AddSeries(line).AxHLine(0, reference => { zero = reference; reference.LineStyle = LineStyle.Solid; });
            }

            configure?.Invoke(new CorrelationParts(line, zero));
            return axes;
        }
    }

    extension(double[] values)
    {
        /// <summary>The decibels of each value: <paramref name="decibelsPerDecade"/> times its logarithm to base ten (10 for a
        /// power, 20 for a magnitude). A value that is not above zero has no logarithm: it is NaN, a gap in the line, and not
        /// minus infinity (zero) or an error (a missing value is NaN already).</summary>
        /// <param name="decibelsPerDecade">The decibels a factor of ten in the value adds.</param>
        /// <returns>The decibels, one for each value.</returns>
        private double[] ToDecibels(double decibelsPerDecade) =>
            [.. values.Select(value => value > 0 ? decibelsPerDecade * Math.Log10(value) : double.NaN)];
    }

    private static string Shown(int count) => count.ToString(CultureInfo.InvariantCulture);
}
