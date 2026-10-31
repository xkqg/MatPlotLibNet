// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>How a spectrum is estimated by Welch's method of averaged periodograms (matplotlib's <c>psd</c>, <c>csd</c> and
/// <c>cohere</c>): the signal is cut into segments of <see cref="SegmentLength"/> samples that start
/// <see cref="SegmentLength"/> − <see cref="Overlap"/> samples apart, each segment is detrended, weighted by the window and
/// transformed, and the spectra of the segments are averaged. A segment that would run past the end of the signal is left out.
/// The value you get from <c>default</c> holds matplotlib's defaults: a sample rate of 2, the Hann window, no detrend, segments
/// of 256 samples with no overlap and no padding, no frequency offset, and a density.
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
    private readonly double? _sampleRate;
    private readonly int? _segmentLength;
    private readonly int _overlap;
    private readonly int _padTo;
    private readonly double _centerFrequency;
    private readonly SpectralScaling _scaling;

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

    /// <summary>The weights each segment is multiplied by before it is transformed. Default <see cref="SpectralWindow.Hann"/>
    /// (matplotlib's <c>window_hanning</c>).</summary>
    public SpectralWindow Window { get; init; }

    /// <summary>What is taken out of each segment before it is weighted. Default <see cref="SpectralDetrend.None"/>
    /// (matplotlib's <c>detrend_none</c>).</summary>
    public SpectralDetrend Detrend { get; init; }

    /// <summary>The number of samples in each segment (matplotlib's <c>NFFT</c>), any length, not only a power of two. Default
    /// 256. The signal must hold at least one segment.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int SegmentLength
    {
        get => _segmentLength ?? 256;
        init => _segmentLength = value >= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(SegmentLength), value, "A segment holds at least one sample.");
    }

    /// <summary>The number of samples two neighbouring segments share (matplotlib's <c>noverlap</c>). Default 0. It must be
    /// below <see cref="SegmentLength"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0.</exception>
    public int Overlap
    {
        get => _overlap;
        init => _overlap = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Overlap), value, "The overlap of two segments is 0 samples or more.");
    }

    /// <summary>The length each segment is padded to with zeros before it is transformed (matplotlib's <c>pad_to</c>), which
    /// gives more frequencies, Δf = <see cref="SampleRate"/> / PadTo apart, without adding information. Default 0: no padding,
    /// the transform has <see cref="SegmentLength"/> samples. When set it must be at least <see cref="SegmentLength"/>.</summary>
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

    /// <summary>Whether the result is a density per hertz or a power per bin. Default <see cref="SpectralScaling.Density"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a member of <see cref="SpectralScaling"/>.</exception>
    public SpectralScaling Scaling
    {
        get => _scaling;
        init => _scaling = Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Scaling), value, "The scaling is Density or Spectrum.");
    }
}
