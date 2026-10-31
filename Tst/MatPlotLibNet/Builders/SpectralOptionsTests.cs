// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Verifies <see cref="SpectralOptions"/>: <c>default</c> holds matplotlib's defaults for <c>psd</c>, <c>csd</c> and
/// <c>cohere</c> (<c>Fs=2</c>, <c>window_hanning</c>, <c>detrend_none</c>, <c>NFFT=256</c>, <c>noverlap=0</c>, no
/// <c>pad_to</c>, <c>Fc=0</c>, <c>scale_by_freq=True</c>), and every property refuses, by its own name, a value no spectrum
/// can be computed with.</summary>
public class SpectralOptionsTests
{
    [Fact]
    public void Default_IsMatplotlibsDefaults()
    {
        SpectralOptions options = default;

        Assert.Equal(2.0, options.SampleRate);
        Assert.Equal(SpectralWindow.Hann, options.Window);
        Assert.Equal(SpectralDetrend.None, options.Detrend);
        Assert.Equal(256, options.SegmentLength);
        Assert.Equal(0, options.Overlap);
        Assert.Equal(0, options.PadTo);
        Assert.Equal(0.0, options.CenterFrequency);
        Assert.Equal(SpectralScaling.Density, options.Scaling);
    }

    [Fact]
    public void New_IsTheDefault()
    {
        var options = new SpectralOptions();

        Assert.Equal(2.0, options.SampleRate);
        Assert.Equal(256, options.SegmentLength);
    }

    [Fact]
    public void EveryProperty_KeepsWhatItIsGiven()
    {
        var options = new SpectralOptions
        {
            SampleRate = 1000,
            Window = SpectralWindow.Hamming,
            Detrend = SpectralDetrend.Linear,
            SegmentLength = 64,
            Overlap = 32,
            PadTo = 128,
            CenterFrequency = -25.5,
            Scaling = SpectralScaling.Spectrum,
        };

        Assert.Equal(1000.0, options.SampleRate);
        Assert.Equal(SpectralWindow.Hamming, options.Window);
        Assert.Equal(SpectralDetrend.Linear, options.Detrend);
        Assert.Equal(64, options.SegmentLength);
        Assert.Equal(32, options.Overlap);
        Assert.Equal(128, options.PadTo);
        Assert.Equal(-25.5, options.CenterFrequency);
        Assert.Equal(SpectralScaling.Spectrum, options.Scaling);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void SampleRate_NotAFiniteNumberAboveZero_Refuses(double rate)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralOptions { SampleRate = rate });
        Assert.Equal(nameof(SpectralOptions.SampleRate), error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void SegmentLength_BelowOne_Refuses(int length)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralOptions { SegmentLength = length });
        Assert.Equal(nameof(SpectralOptions.SegmentLength), error.ParamName);
    }

    [Fact]
    public void Overlap_Negative_Refuses()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralOptions { Overlap = -1 });
        Assert.Equal(nameof(SpectralOptions.Overlap), error.ParamName);
    }

    [Fact]
    public void PadTo_Negative_Refuses()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralOptions { PadTo = -1 });
        Assert.Equal(nameof(SpectralOptions.PadTo), error.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void CenterFrequency_NotFinite_Refuses(double frequency)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralOptions { CenterFrequency = frequency });
        Assert.Equal(nameof(SpectralOptions.CenterFrequency), error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Scaling_NotAMember_Refuses(int scaling)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralOptions { Scaling = (SpectralScaling)scaling });
        Assert.Equal(nameof(SpectralOptions.Scaling), error.ParamName);
    }

    [Fact]
    public void Scaling_KeepsItsOrdinals()
    {
        // Append-only: the numbers a stored value means never change.
        Assert.Equal(0, (int)SpectralScaling.Density);
        Assert.Equal(1, (int)SpectralScaling.Spectrum);
    }
}
