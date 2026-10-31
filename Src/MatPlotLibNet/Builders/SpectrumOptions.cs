// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>How the spectrum of a whole signal is taken in one transform (matplotlib's <c>magnitude_spectrum</c>,
/// <c>phase_spectrum</c> and <c>angle_spectrum</c>): the signal is weighted by the window, padded with zeros to
/// <see cref="PadTo"/> samples and transformed at exactly that length, and the frequencies run from 0 to half the sample rate,
/// shifted by <see cref="CenterFrequency"/>. Unlike <see cref="SpectralOptions"/> there are no segments and no averaging: the
/// signal is one segment.
/// The value you get from <c>default</c> holds matplotlib's defaults: a sample rate of 2, the Hann window, no padding and no
/// frequency offset.
/// <para>Set only what differs from those defaults with an object initializer; each property refuses, by its own name, a value
/// no spectrum can be computed with. Whether the padding is at least as long as the signal is checked where the options meet a
/// signal.</para></summary>
/// <example>
/// <code>
/// var options = new SpectrumOptions { SampleRate = 1000, Window = SpectralWindow.Rectangular, PadTo = 2048 };
/// </code>
/// </example>
public readonly record struct SpectrumOptions
{
    private readonly double? _sampleRate;
    private readonly int _padTo;
    private readonly double _centerFrequency;

    /// <summary>The number of samples per unit of time, in hertz for a signal sampled in seconds; it sets the frequency axis.
    /// Default 2 (matplotlib's <c>Fs</c>), so the frequencies run from 0 to 1, the Nyquist frequency.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a finite number above 0.</exception>
    public double SampleRate
    {
        get => _sampleRate ?? 2;
        init => _sampleRate = double.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(SampleRate), value, "The sample rate is a finite number above 0.");
    }

    /// <summary>The weights the signal is multiplied by before it is transformed; the magnitude is divided by their sum, so a
    /// constant signal has magnitude 1 at frequency 0 whatever the window. Default <see cref="SpectralWindow.Hann"/> (matplotlib's
    /// <c>window_hanning</c>); <see cref="SpectralWindow.Rectangular"/> takes the signal as it is.</summary>
    public SpectralWindow Window { get; init; }

    /// <summary>The length the signal is padded to with zeros before it is transformed (matplotlib's <c>pad_to</c>), which gives
    /// more frequencies, Δf = <see cref="SampleRate"/> / PadTo apart, without adding information. Default 0: no padding, the
    /// transform has as many samples as the signal. When set it must be at least the length of the signal.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0.</exception>
    public int PadTo
    {
        get => _padTo;
        init => _padTo = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(PadTo), value, "The padded length is 0 (no padding) or a number of samples.");
    }

    /// <summary>The frequency added to every frequency of the result (matplotlib's <c>Fc</c>), for a signal that was shifted
    /// down from a carrier. Default 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a finite number.</exception>
    public double CenterFrequency
    {
        get => _centerFrequency;
        init => _centerFrequency = double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(CenterFrequency), value, "The centre frequency is a finite number.");
    }
}
