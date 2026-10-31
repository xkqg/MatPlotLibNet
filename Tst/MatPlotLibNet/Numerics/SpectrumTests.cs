// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Spectrum"/>: the spectrum of a whole signal in one transform, as matplotlib's
/// <c>mlab.magnitude_spectrum</c>, <c>mlab.angle_spectrum</c> and <c>mlab.phase_spectrum</c> compute it (its
/// <c>_single_spectrum_helper</c>): the signal weighted by the window (Hann when none is given), padded with zeros to
/// <c>pad_to</c>, transformed at exactly that length and kept on one side; the magnitude divided by the sum of the window, the
/// angle wrapped into (−π, π], the phase unwrapped along the frequencies (numpy's <c>unwrap</c>). The vectors marked
/// "matplotlib" were printed by matplotlib 3.11.2 (numpy 2) and are compared by tolerance, never by stored bytes.</summary>
public class SpectrumTests
{
    private const double Tolerance = 1e-12;

    private static readonly double[] Ones8 = [1, 1, 1, 1, 1, 1, 1, 1];

    /// <summary>cos(πn/2) for n = 0..7, written out so no cosine is taken.</summary>
    private static readonly double[] Cosine8 = [1, 0, -1, 0, 1, 0, -1, 0];

    /// <summary>Eleven integers, so the transform is not a power of two (Bluestein's) and no bin is zero.</summary>
    private static readonly double[] Eleven = [3, -1, 4, 1, -5, 9, 2, -6, 5, 3, -5];

    /// <summary>A unit impulse at sample 3 of 7: its spectrum is e^(−2πi·3k/7), so the angle runs down by 6π/7 per bin and wraps.</summary>
    private static readonly double[] Impulse7 = [0, 0, 0, 1, 0, 0, 0];

    private static void AssertClose(IReadOnlyList<double> expected, IReadOnlyList<double> actual, double tolerance = Tolerance)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    // ---- matplotlib's vectors ----------------------------------------------------------------------------------------------

    [Fact]
    public void Magnitude_OfOnes_WithTheDefaultHannWindow_IsMatplotlibs()
    {
        // matplotlib: magnitude_spectrum(ones(8)) — Hann by default, divided by sum(w) = 3.5, so the first bin is 1.
        OneSidedSpectrum spectrum = Spectrum.Of(Ones8, default);

        AssertClose([0, 0.25, 0.5, 0.75, 1], spectrum.Frequencies);
        AssertClose([1, 0.5942915774844614, 0.06100063442391807, 0.015469346261788391, 0], spectrum.Magnitudes());
    }

    [Fact]
    public void Magnitude_AndAngle_OfACosine_AreMatplotlibs()
    {
        OneSidedSpectrum spectrum = Spectrum.Of(Cosine8, default);

        AssertClose([0.04313396225783401, 0.2972464380538211, 0.5, 0.2972464380538211, 0.04313396225783401], spectrum.Magnitudes());
        AssertClose([0, 2.722869555250066, 0, -2.722869555250066, 0], spectrum.Angles());
        AssertClose([0, 2.722869555250066, 0, -2.722869555250066, 0], spectrum.Phases());
    }

    [Fact]
    public void TheAngle_IsWrapped_AndThePhase_IsUnwrapped()
    {
        // matplotlib: angle_spectrum and phase_spectrum of a unit impulse at sample 3 of 7 (an odd, Bluestein, length).
        OneSidedSpectrum spectrum = Spectrum.Of(Impulse7, default);

        AssertClose([0, 2 / 7.0, 4 / 7.0, 6 / 7.0], spectrum.Frequencies);
        AssertClose([1 / 3.0, 1 / 3.0, 1 / 3.0, 1 / 3.0], spectrum.Magnitudes());
        AssertClose([0, -2.6927937030769655, 0.8975979010256553, -1.7951958020513104], spectrum.Angles());
        AssertClose([0, -2.6927937030769655, -5.385587406153931, -8.078381109230897], spectrum.Phases());
    }

    [Fact]
    public void AWindowOfTheCallersChoice_AndPadding_AreMatplotlibs()
    {
        // matplotlib: magnitude/angle/phase_spectrum(x, window=np.hamming(11), pad_to=14, Fs=1).
        var options = new SpectrumOptions { SampleRate = 1, Window = SpectralWindow.Hamming, PadTo = 14 };

        OneSidedSpectrum spectrum = Spectrum.Of(Eleven, options);

        AssertClose([.. Enumerable.Range(0, 8).Select(k => k / 14.0)], spectrum.Frequencies);
        AssertClose(
            [1.2060568374384963, 0.9574313632242288, 0.8461624886956842, 1.7848445075520665, 3.165356160341915, 3.2707376322161315, 1.8513206989633022, 0.9563392494639374],
            spectrum.Magnitudes());
        AssertClose(
            [0, -2.163700435605824, 1.6126056059677956, -0.7647854269807199, 3.095851653265369, 0.704217416526966, -1.4985695578332388, Math.PI],
            spectrum.Angles());
        AssertClose(
            [0, -2.163700435605824, -4.670579701211791, -7.047970734160306, -9.470518961093804, -11.862153197832207, -14.064940172192411, -15.707963267948966],
            spectrum.Phases());
    }

    [Fact]
    public void AnOddSignal_WithTheDefaults_IsMatplotlibs()
    {
        OneSidedSpectrum spectrum = Spectrum.Of(Eleven, default);

        AssertClose([1.2628677106875996, 0.9583081459258918, 1.4476207109570938, 3.1679864191679554, 3.3433511243899843, 1.611814185497395], spectrum.Magnitudes());
        AssertClose([0, -2.8322433931574174, -5.966370579287538, -9.057653862994302, -12.036742962314417, -14.82445488359241], spectrum.Phases());
    }

    [Fact]
    public void Padding_GivesTheBinsBetween_AndKeepsTheBinsOfTheSignalItself()
    {
        // matplotlib: magnitude_spectrum(ones(8), pad_to=16): 9 bins Fs/16 apart; every second one is a bin of the unpadded spectrum.
        OneSidedSpectrum spectrum = Spectrum.Of(Ones8, new SpectrumOptions { PadTo = 16 });

        AssertClose([0, 0.125, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875, 1], spectrum.Frequencies);
        AssertClose(
            [1, 0.8828883284707109, 0.5942915774844614, 0.27794862418810845, 0.06100063442391807, 0.019844378373677325, 0.015469346261788391, 0.001348907688211372, 0],
            spectrum.Magnitudes());
    }

    [Fact]
    public void TheSampleRate_SpacesTheFrequencies_AndTheCentreFrequency_ShiftsThem()
    {
        // matplotlib: Axes.magnitude_spectrum(ones(8), Fs=100, Fc=1000) — freqs += Fc.
        OneSidedSpectrum spectrum = Spectrum.Of(Ones8, new SpectrumOptions { SampleRate = 100, CenterFrequency = 1000 });

        AssertClose([1000, 1012.5, 1025, 1037.5, 1050], spectrum.Frequencies);
        AssertClose([1, 0.5942915774844614, 0.06100063442391807, 0.015469346261788391, 0], spectrum.Magnitudes());
    }

    [Fact]
    public void AnOddLength_KeepsHalfTheBinsAndOne_WithNoNyquistBin()
    {
        OneSidedSpectrum spectrum = Spectrum.Of([1, 1, 1, 1, 1, 1, 1], default);

        AssertClose([0, 0.2857142857142857, 0.5714285714285714, 0.8571428571428571], spectrum.Frequencies);
    }

    [Fact]
    public void OneSample_IsItsOwnSpectrum()
    {
        // matplotlib: magnitude_spectrum([5]) = [5] at 0; angle_spectrum([-5]) = [pi]. A Hann window of one sample is [1].
        Assert.Equal([5.0], Spectrum.Of([5], default).Magnitudes());
        Assert.Equal([0.0], Spectrum.Of([5], default).Frequencies);
        Assert.Equal([Math.PI], Spectrum.Of([-5], default).Angles());
    }

    // ---- the branch cut --------------------------------------------------------------------------------------------------

    [Fact]
    public void ANegativeRealBin_HasTheAnglePlusPi_NeverMinusPi()
    {
        // matplotlib: angle_spectrum([-2, -2], window=window_none) = [pi, 0], and the phase keeps it (numpy keeps an exact pi step).
        OneSidedSpectrum spectrum = Spectrum.Of([-2, -2], new SpectrumOptions { Window = SpectralWindow.Rectangular });

        Assert.Equal([Math.PI, 0.0], spectrum.Angles());
        Assert.Equal([Math.PI, 0.0], spectrum.Phases());
    }

    [Fact]
    public void TheFirstBin_AndTheNyquistBinOfAnEvenTransform_AreReal()
    {
        // The transform of a real signal is real at 0 and at M / 2; written so, the angle there is 0 or +pi, as numpy gives it,
        // and never the -pi a rounding error below zero in the imaginary part would make of it.
        var options = new SpectrumOptions { SampleRate = 1, Window = SpectralWindow.Hamming, PadTo = 14 };

        OneSidedSpectrum spectrum = Spectrum.Of(Eleven, options);

        Assert.Equal(0.0, spectrum.Bins[0].Imaginary);
        Assert.Equal(0.0, spectrum.Bins[^1].Imaginary);
        Assert.Equal(Math.PI, spectrum.Angles()[^1]);
    }

    [Theory]
    [InlineData(new[] { 3.0, -3.0 }, new[] { 3.0, 3.2831853071795862 })]
    [InlineData(new[] { 0.0, Math.PI }, new[] { 0.0, Math.PI })]
    [InlineData(new[] { 0.0, -Math.PI }, new[] { 0.0, -Math.PI })]
    [InlineData(new[] { 0.0, 1.5 * Math.PI }, new[] { 0.0, -1.5707963267948966 })]
    [InlineData(new[] { Math.PI, 0.0 }, new[] { Math.PI, 0.0 })]
    [InlineData(new[] { 0.0, 3.0, -3.0, 3.0 }, new[] { 0.0, 3.0, 3.2831853071795862, 3.0 })]
    [InlineData(new double[0], new double[0])]
    public void Unwrap_IsNumpys(double[] angles, double[] expected)
    {
        // numpy: unwrap(angles). A step of exactly +pi or -pi is kept, not corrected.
        AssertClose(expected, Spectrum.Unwrap(angles));
    }

    // ---- what cannot be computed --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AMissingSample_MakesEveryValueMissing_AndKeepsTheFrequencies(double missing)
    {
        double[] signal = [.. Eleven];
        signal[4] = missing;

        OneSidedSpectrum spectrum = Spectrum.Of(signal, default);

        AssertClose([.. Enumerable.Range(0, 6).Select(k => 2 * k / 11.0)], spectrum.Frequencies);
        Assert.All(spectrum.Magnitudes(), value => Assert.True(double.IsNaN(value)));
        Assert.All(spectrum.Angles(), value => Assert.True(double.IsNaN(value)));
        Assert.All(spectrum.Phases(), value => Assert.True(double.IsNaN(value)));
    }

    [Fact]
    public void AWindowThatSumsToZero_HasNoMagnitude()
    {
        // The Hann window of two samples is [0, 0]: matplotlib divides 0 by 0 and gets nan, and so does this.
        Assert.All(Spectrum.Of([1, 1], default).Magnitudes(), value => Assert.True(double.IsNaN(value)));
    }

    [Fact]
    public void ANullSignal_IsRefusedByItsName()
    {
        Assert.Equal("signal", Assert.Throws<ArgumentNullException>(() => Spectrum.Of(null!, default)).ParamName);
    }

    [Fact]
    public void AnEmptySignal_IsRefusedByItsName()
    {
        var refused = Assert.Throws<ArgumentException>(() => Spectrum.Of([], default));

        Assert.Equal("signal", refused.ParamName);
        Assert.Contains("no samples", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APaddingShorterThanTheSignal_IsRefusedByTheNameOfTheOptions()
    {
        var refused = Assert.Throws<ArgumentException>(() => Spectrum.Of(Eleven, new SpectrumOptions { PadTo = 10 }));

        Assert.Equal("options", refused.ParamName);
        Assert.Contains("11", refused.Message, StringComparison.Ordinal);
        Assert.Contains("10", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APaddingOfTheSignalsOwnLength_IsNoPadding()
    {
        Assert.Equal(Spectrum.Of(Eleven, default).Magnitudes(), Spectrum.Of(Eleven, new SpectrumOptions { PadTo = 11 }).Magnitudes());
    }
}
