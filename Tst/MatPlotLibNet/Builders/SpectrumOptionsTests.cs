// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Verifies <see cref="SpectrumOptions"/>: <c>default</c> holds matplotlib's defaults for <c>magnitude_spectrum</c>,
/// <c>phase_spectrum</c> and <c>angle_spectrum</c> (<c>Fs=2</c>, <c>window_hanning</c>, no <c>pad_to</c>, <c>Fc=0</c>), and every
/// property refuses, by its own name, a value no spectrum can be computed with.</summary>
public class SpectrumOptionsTests
{
    [Fact]
    public void Default_IsMatplotlibsDefaults()
    {
        SpectrumOptions options = default;

        Assert.Equal(2.0, options.SampleRate);
        Assert.Equal(SpectralWindow.Hann, options.Window);
        Assert.Equal(0, options.PadTo);
        Assert.Equal(0.0, options.CenterFrequency);
    }

    [Fact]
    public void New_IsTheDefault()
    {
        Assert.Equal(default, new SpectrumOptions());
        Assert.Equal(2.0, new SpectrumOptions().SampleRate);
    }

    [Fact]
    public void EveryProperty_KeepsWhatItIsGiven()
    {
        var options = new SpectrumOptions
        {
            SampleRate = 1000,
            Window = SpectralWindow.Blackman,
            PadTo = 64,
            CenterFrequency = -12.5,
        };

        Assert.Equal(1000.0, options.SampleRate);
        Assert.Equal(SpectralWindow.Blackman, options.Window);
        Assert.Equal(64, options.PadTo);
        Assert.Equal(-12.5, options.CenterFrequency);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void SampleRate_NotAFiniteNumberAboveZero_Refuses(double rate)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectrumOptions { SampleRate = rate });
        Assert.Equal(nameof(SpectrumOptions.SampleRate), error.ParamName);
    }

    [Fact]
    public void PadTo_Negative_Refuses()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectrumOptions { PadTo = -1 });
        Assert.Equal(nameof(SpectrumOptions.PadTo), error.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CenterFrequency_NotFinite_Refuses(double frequency)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SpectrumOptions { CenterFrequency = frequency });
        Assert.Equal(nameof(SpectrumOptions.CenterFrequency), error.ParamName);
    }
}
