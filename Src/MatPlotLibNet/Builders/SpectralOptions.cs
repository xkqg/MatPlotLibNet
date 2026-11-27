// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>How a spectrum is estimated by Welch's method of averaged periodograms (matplotlib's <c>psd</c>, <c>csd</c> and <c>cohere</c>):
/// the signal is cut into segments of <see cref="SegmentLength"/> samples that start <see cref="SegmentLength"/> − <see cref="Overlap"/>
/// samples apart, each segment is detrended, weighted by the window and transformed, and the spectra of the segments are averaged. A
/// segment that would run past the end of the signal is left out. The value you get from <c>default</c> holds matplotlib's defaults: a
/// sample rate of <see cref="SpectralOptionExtensions.RestingSampleRate"/>, the Hann window, no detrend, segments of
/// <see cref="RestingSegmentLength"/> samples with no overlap and no padding, no frequency offset, a density, and the one-sided
/// frequencies.
/// <para>Set only what differs from those defaults with an object initializer; each property refuses, by its own name, a value
/// no spectrum can be computed with. Whether the segment fits the signal, the overlap fits the segment and the padding is at
/// least the segment is checked where the options meet a signal.</para></summary>
/// <example>
/// <code>
/// var options = new SpectralOptions { SampleRate = 1000, SegmentLength = 128, Overlap = 64, Detrend = SpectralDetrend.Mean };
/// </code>
/// </example>
public readonly record struct SpectralOptions
{
    internal const int RestingSegmentLength = 256;
    private readonly int _overlap;
    private readonly int _padTo;
    private readonly double _centerFrequency;
    private readonly SpectralScaling _scaling;
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

    /// <summary>The weights each segment is multiplied by before it is transformed. Default <see cref="SpectralWindow.Hann"/>
    /// (matplotlib's <c>window_hanning</c>).</summary>
    public SpectralWindow Window { get; init; }

    /// <summary>What is taken out of each segment before it is weighted. Default <see cref="SpectralDetrend.None"/>
    /// (matplotlib's <c>detrend_none</c>).</summary>
    public SpectralDetrend Detrend { get; init; }

    /// <summary>The number of samples in each segment (matplotlib's <c>NFFT</c>), any length, not only a power of two. Default
    /// <see cref="RestingSegmentLength"/>. The signal must hold at least one segment.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int SegmentLength
    {
        get => GivenSegmentLength ?? RestingSegmentLength;
        init => GivenSegmentLength = value.ThrowIfBelow(1, nameof(SegmentLength), "The samples in a segment");
    }

    /// <summary>The value of <see cref="SegmentLength"/> its author gave, <see langword="null"/> while none was.</summary>
    internal int? GivenSegmentLength { get; private init; }

    /// <summary>The number of samples two neighbouring segments share (matplotlib's <c>noverlap</c>). Default 0. It must be
    /// below <see cref="SegmentLength"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0.</exception>
    public int Overlap
    {
        get => _overlap;
        init => _overlap = value.ThrowIfBelow(0, nameof(Overlap), "The overlap of two segments");
    }

    /// <summary>The length each segment is padded to with zeros before it is transformed (matplotlib's <c>pad_to</c>), which
    /// gives more frequencies, Δf = <see cref="SampleRate"/> / PadTo apart, without adding information. Default 0: no padding,
    /// the transform has <see cref="SegmentLength"/> samples. When set it must be at least <see cref="SegmentLength"/>.</summary>
    /// <remarks>On one side every frequency but 0 is doubled, and so is the last one except when it is exactly half the sample rate,
    /// which is so when the padded length is even. matplotlib decides that last frequency by the segment length instead, so where the
    /// padding makes the length odd from even, or even from odd, its last value is half or twice what it should be and its
    /// spectrum no longer adds up to the power of the signal. Here it does, padded or not.</remarks>
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

    /// <summary>Whether the result is a density per hertz or a power per bin. Default <see cref="SpectralScaling.Density"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a member of <see cref="SpectralScaling"/>.</exception>
    public SpectralScaling Scaling
    {
        get => _scaling;
        init => _scaling = value.ThrowIfNotDefined(nameof(Scaling), "The scaling is Density or Spectrum.");
    }

    /// <summary>Which frequencies the result covers (matplotlib's <c>sides</c>). Default <see cref="SpectralSides.Default"/>, the
    /// frequencies from 0 to half the sample rate. <see cref="SpectralSides.TwoSided"/> gives every frequency of the transform, from
    /// minus half the sample rate up, each at half the power the one-sided spectrum gives its frequency; the signals are real, so
    /// the two halves mirror one another and the coherence is the same on either.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a member of <see cref="SpectralSides"/>.</exception>
    public SpectralSides Sides
    {
        get => _sides;
        init => _sides = value.AsSides(nameof(Sides));
    }
}
