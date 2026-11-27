// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Verifies <see cref="SpectralOptionExtensions"/>: the four settings the options of a spectrum share, refused by the name
/// of the property that was given them and in the same words, and the same on both kinds of options, <see cref="SpectralOptions"/>
/// (a Welch estimate) and <see cref="SpectrumOptions"/> (a whole-signal spectrum): each starts from matplotlib's default, keeps what
/// it is given and refuses a value by the name of its own property, as every setting only a Welch estimate has refuses one too. The
/// defaults of those and the values they keep are held by <see cref="SpectralOptionsTests"/>.</summary>
public class SpectralOptionExtensionsTests
{
    /// <summary>The settings both kinds of options have, read off one of them.</summary>
    /// <param name="SampleRate">The sample rate.</param>
    /// <param name="Window">The window.</param>
    /// <param name="PadTo">The padded length.</param>
    /// <param name="CenterFrequency">The centre frequency.</param>
    /// <param name="Sides">The sides.</param>
    public readonly record struct SharedSettings(double SampleRate, SpectralWindow Window, int PadTo, double CenterFrequency, SpectralSides Sides)
    {
        /// <summary>The shared settings of the options of a Welch estimate.</summary>
        public static SharedSettings Of(SpectralOptions options) =>
            new(options.SampleRate, options.Window, options.PadTo, options.CenterFrequency, options.Sides);

        /// <summary>The shared settings of the options of a whole-signal spectrum.</summary>
        public static SharedSettings Of(SpectrumOptions options) =>
            new(options.SampleRate, options.Window, options.PadTo, options.CenterFrequency, options.Sides);
    }

    /// <summary>matplotlib's defaults for both: <c>Fs=2</c>, <c>window_hanning</c>, no <c>pad_to</c>, <c>Fc=0</c> and the one-sided
    /// frequencies.</summary>
    private static readonly SharedSettings MatplotlibsDefaults = new(2.0, SpectralWindow.Hann, 0, 0.0, SpectralSides.Default);

    /// <summary>Each kind of options at rest, from <c>default</c> and from <c>new()</c>.</summary>
    public static TheoryData<SharedSettings> AtRest => new()
    {
        SharedSettings.Of(default(SpectralOptions)), SharedSettings.Of(new SpectralOptions()),
        SharedSettings.Of(default(SpectrumOptions)), SharedSettings.Of(new SpectrumOptions()),
    };

    /// <summary>What each kind of options was given, and what it keeps.</summary>
    public static TheoryData<SharedSettings, SharedSettings> Given => new()
    {
        { new(1000, SpectralWindow.Hamming, 128, -25.5, SpectralSides.TwoSided), SharedSettings.Of(new SpectralOptions
            { SampleRate = 1000, Window = SpectralWindow.Hamming, PadTo = 128, CenterFrequency = -25.5, Sides = SpectralSides.TwoSided }) },
        { new(1000, SpectralWindow.Blackman, 64, -12.5, SpectralSides.OneSided), SharedSettings.Of(new SpectrumOptions
            { SampleRate = 1000, Window = SpectralWindow.Blackman, PadTo = 64, CenterFrequency = -12.5, Sides = SpectralSides.OneSided }) },
    };

    /// <summary>A setting of either kind of options given a value no spectrum can be computed with, and the property it names: the
    /// four shared settings of each kind, then the settings only a Welch estimate has.</summary>
    public static TheoryData<Action, string> Refused => new()
    {
        { () => _ = new SpectralOptions { SampleRate = 0 }, nameof(SpectralOptions.SampleRate) },
        { () => _ = new SpectralOptions { PadTo = -1 }, nameof(SpectralOptions.PadTo) },
        { () => _ = new SpectralOptions { CenterFrequency = double.NaN }, nameof(SpectralOptions.CenterFrequency) },
        { () => _ = new SpectralOptions { Sides = (SpectralSides)3 }, nameof(SpectralOptions.Sides) },
        { () => _ = new SpectrumOptions { SampleRate = double.PositiveInfinity }, nameof(SpectrumOptions.SampleRate) },
        { () => _ = new SpectrumOptions { PadTo = -1 }, nameof(SpectrumOptions.PadTo) },
        { () => _ = new SpectrumOptions { CenterFrequency = double.PositiveInfinity }, nameof(SpectrumOptions.CenterFrequency) },
        { () => _ = new SpectrumOptions { Sides = (SpectralSides)(-1) }, nameof(SpectrumOptions.Sides) },
        { () => _ = new SpectralOptions { SegmentLength = 0 }, nameof(SpectralOptions.SegmentLength) },
        { () => _ = new SpectralOptions { SegmentLength = -4 }, nameof(SpectralOptions.SegmentLength) },
        { () => _ = new SpectralOptions { Overlap = -1 }, nameof(SpectralOptions.Overlap) },
        { () => _ = new SpectralOptions { Scaling = (SpectralScaling)(-1) }, nameof(SpectralOptions.Scaling) },
        { () => _ = new SpectralOptions { Scaling = (SpectralScaling)2 }, nameof(SpectralOptions.Scaling) },
    };

    [Theory]
    [MemberData(nameof(AtRest))]
    public void TheSharedSettings_AtRest_AreMatplotlibsDefaults(SharedSettings settings) => Assert.Equal(MatplotlibsDefaults, settings);

    [Fact]
    public void NewOptions_AreTheDefault()
    {
        Assert.Equal(default, new SpectralOptions());
        Assert.Equal(default, new SpectrumOptions());
    }

    [Theory]
    [MemberData(nameof(Given))]
    public void TheSharedSettings_KeepWhatTheyAreGiven(SharedSettings given, SharedSettings kept) => Assert.Equal(given, kept);

    [Theory]
    [MemberData(nameof(Refused))]
    public void ASetting_IsRefusedByTheNameOfItsProperty(Action set, string property) =>
        Assert.Equal(property, Assert.Throws<ArgumentOutOfRangeException>(set).ParamName);

    [Theory]
    [InlineData(SpectralSides.Default)]
    [InlineData(SpectralSides.OneSided)]
    [InlineData(SpectralSides.TwoSided)]
    public void TheSides_AreHandedBack(SpectralSides sides) => Assert.Equal(sides, sides.AsSides("Sides"));

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void SidesThatAreNoMember_AreRefusedByTheNameGiven(int sides)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => ((SpectralSides)sides).AsSides("Side"));

        Assert.Equal("Side", error.ParamName);
        Assert.StartsWith("The sides are Default, OneSided or TwoSided.", error.Message);
    }

    [Fact]
    public void AGoodSampleRate_IsHandedBack() => Assert.Equal(1000.0, 1000.0.AsSampleRate("SampleRate"));

    [Theory]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ASampleRateThatIsNoFiniteNumberAboveZero_IsRefusedByTheNameGiven(double rate)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => rate.AsSampleRate("Rate"));

        Assert.Equal("Rate", error.ParamName);
        Assert.StartsWith("The sample rate must be ", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4096)]
    public void AGoodPaddedLength_IsHandedBack(int length) => Assert.Equal(length, length.AsPadTo("PadTo"));

    [Fact]
    public void ANegativePaddedLength_IsRefusedByTheNameGiven()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => (-1).AsPadTo("Padding"));

        Assert.Equal("Padding", error.ParamName);
        Assert.StartsWith("The padded length must be >= 0 (got -1).", error.Message);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.5)]
    [InlineData(1e6)]
    public void AGoodCentreFrequency_IsHandedBack(double frequency) => Assert.Equal(frequency, frequency.AsCenterFrequency("CenterFrequency"));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void ACentreFrequencyThatIsNotFinite_IsRefusedByTheNameGiven(double frequency)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => frequency.AsCenterFrequency("Centre"));

        Assert.Equal("Centre", error.ParamName);
        Assert.StartsWith("The centre frequency must be finite (got ", error.Message);
    }
}
