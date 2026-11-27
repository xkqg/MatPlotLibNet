// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>How the spectrum of a whole signal is taken in one transform (matplotlib's <c>magnitude_spectrum</c>, <c>phase_spectrum</c> and
/// <c>angle_spectrum</c>): the signal is weighted by the window, padded with zeros to <see cref="PadTo"/> samples and transformed at
/// exactly that length, and the frequencies run from 0 to half the sample rate, shifted by <see cref="CenterFrequency"/>. Unlike
/// <see cref="SpectralOptions"/> there are no segments and no averaging: the signal is one segment. The value you get from <c>default</c>
/// holds matplotlib's defaults: a sample rate of <see cref="SpectralOptionExtensions.RestingSampleRate"/>, the Hann window, no padding, no
/// frequency offset and the one-sided frequencies.
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
    private readonly int _padTo;
    private readonly double _centerFrequency;
    private readonly SpectralSides _sides;

    /// <summary>The number of samples per unit of time, in hertz for a signal sampled in seconds; it sets the frequency axis. Default
    /// <see cref="SpectralOptionExtensions.RestingSampleRate"/> (matplotlib's <c>Fs</c>), so the frequencies run from 0 to 1, the Nyquist
    /// frequency.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a finite number above 0.</exception>
    public double SampleRate
    {
        get => GivenSampleRate ?? SpectralOptionExtensions.RestingSampleRate;
        init => GivenSampleRate = value.AsSampleRate(nameof(SampleRate));
    }

    /// <summary>The value of <see cref="SampleRate"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenSampleRate { get; private init; }

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
        init => _padTo = value.AsPadTo(nameof(PadTo));
    }

    /// <summary>The frequency added to every frequency of the result (matplotlib's <c>Fc</c>), for a signal that was shifted
    /// down from a carrier. Default 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a finite number.</exception>
    public double CenterFrequency
    {
        get => _centerFrequency;
        init => _centerFrequency = value.AsCenterFrequency(nameof(CenterFrequency));
    }

    /// <summary>Which frequencies the result covers (matplotlib's <c>sides</c>). Default <see cref="SpectralSides.Default"/>, the
    /// frequencies from 0 to half the sample rate. <see cref="SpectralSides.TwoSided"/> gives every frequency of the transform, from
    /// minus half the sample rate up; the signal is real, so the magnitude is the same at a frequency and at its negative and the
    /// angle has the opposite sign.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a member of <see cref="SpectralSides"/>.</exception>
    public SpectralSides Sides
    {
        get => _sides;
        init => _sides = value.AsSides(nameof(Sides));
    }
}
