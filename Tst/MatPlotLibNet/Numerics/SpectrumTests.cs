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

    // ---- matplotlib's vectors ----------------------------------------------------------------------------------------------

    [Fact]
    public void Magnitude_OfOnes_WithTheDefaultHannWindow_IsMatplotlibs()
    {
        // matplotlib: magnitude_spectrum(ones(8)) — Hann by default, divided by sum(w) = 3.5, so the first bin is 1.
        SignalSpectrum spectrum = Spectrum.Of(Ones8, default);

        (new double[] { 0, 0.25, 0.5, 0.75, 1 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 1, 0.5942915774844614, 0.06100063442391807, 0.015469346261788391, 0 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
    }

    [Fact]
    public void Magnitude_AndAngle_OfACosine_AreMatplotlibs()
    {
        SignalSpectrum spectrum = Spectrum.Of(Cosine8, default);

        (new double[] { 0.04313396225783401, 0.2972464380538211, 0.5, 0.2972464380538211, 0.04313396225783401 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
        (new double[] { 0, 2.722869555250066, 0, -2.722869555250066, 0 }).AssertCloseTo(spectrum.Angles(), Tolerance);
        (new double[] { 0, 2.722869555250066, 0, -2.722869555250066, 0 }).AssertCloseTo(spectrum.Phases(), Tolerance);
    }

    [Fact]
    public void TheAngle_IsWrapped_AndThePhase_IsUnwrapped()
    {
        // matplotlib: angle_spectrum and phase_spectrum of a unit impulse at sample 3 of 7 (an odd, Bluestein, length).
        SignalSpectrum spectrum = Spectrum.Of(SpectralSignals.Impulse7, default);

        (new double[] { 0, 2 / 7.0, 4 / 7.0, 6 / 7.0 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 1 / 3.0, 1 / 3.0, 1 / 3.0, 1 / 3.0 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
        (new double[] { 0, -2.6927937030769655, 0.8975979010256553, -1.7951958020513104 }).AssertCloseTo(spectrum.Angles(), Tolerance);
        (new double[] { 0, -2.6927937030769655, -5.385587406153931, -8.078381109230897 }).AssertCloseTo(spectrum.Phases(), Tolerance);
    }

    [Fact]
    public void AWindowOfTheCallersChoice_AndPadding_AreMatplotlibs()
    {
        // matplotlib: magnitude/angle/phase_spectrum(x, window=np.hamming(11), pad_to=14, Fs=1).
        var options = new SpectrumOptions { SampleRate = 1, Window = SpectralWindow.Hamming, PadTo = 14 };

        SignalSpectrum spectrum = Spectrum.Of(Eleven, options);

        Enumerable.Range(0, 8).Select(k => k / 14.0).ToList().AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 1.2060568374384963, 0.9574313632242288, 0.8461624886956842, 1.7848445075520665, 3.165356160341915, 3.2707376322161315, 1.8513206989633022, 0.9563392494639374 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
        (new double[] { 0, -2.163700435605824, 1.6126056059677956, -0.7647854269807199, 3.095851653265369, 0.704217416526966, -1.4985695578332388, Math.PI }).AssertCloseTo(spectrum.Angles(), Tolerance);
        (new double[] { 0, -2.163700435605824, -4.670579701211791, -7.047970734160306, -9.470518961093804, -11.862153197832207, -14.064940172192411, -15.707963267948966 }).AssertCloseTo(spectrum.Phases(), Tolerance);
    }

    [Fact]
    public void AnOddSignal_WithTheDefaults_IsMatplotlibs()
    {
        SignalSpectrum spectrum = Spectrum.Of(Eleven, default);

        (new double[] { 1.2628677106875996, 0.9583081459258918, 1.4476207109570938, 3.1679864191679554, 3.3433511243899843, 1.611814185497395 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
        (new double[] { 0, -2.8322433931574174, -5.966370579287538, -9.057653862994302, -12.036742962314417, -14.82445488359241 }).AssertCloseTo(spectrum.Phases(), Tolerance);
    }

    [Fact]
    public void Padding_GivesTheBinsBetween_AndKeepsTheBinsOfTheSignalItself()
    {
        // matplotlib: magnitude_spectrum(ones(8), pad_to=16): 9 bins Fs/16 apart; every second one is a bin of the unpadded spectrum.
        SignalSpectrum spectrum = Spectrum.Of(Ones8, new SpectrumOptions { PadTo = 16 });

        (new double[] { 0, 0.125, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875, 1 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 1, 0.8828883284707109, 0.5942915774844614, 0.27794862418810845, 0.06100063442391807, 0.019844378373677325, 0.015469346261788391, 0.001348907688211372, 0 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
    }

    [Fact]
    public void TheSampleRate_SpacesTheFrequencies_AndTheCentreFrequency_ShiftsThem()
    {
        // matplotlib: Axes.magnitude_spectrum(ones(8), Fs=100, Fc=1000) — freqs += Fc.
        SignalSpectrum spectrum = Spectrum.Of(Ones8, new SpectrumOptions { SampleRate = 100, CenterFrequency = 1000 });

        (new double[] { 1000, 1012.5, 1025, 1037.5, 1050 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 1, 0.5942915774844614, 0.06100063442391807, 0.015469346261788391, 0 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
    }

    [Fact]
    public void AnOddLength_KeepsHalfTheBinsAndOne_WithNoNyquistBin()
    {
        SignalSpectrum spectrum = Spectrum.Of([1, 1, 1, 1, 1, 1, 1], default);

        (new double[] { 0, 0.2857142857142857, 0.5714285714285714, 0.8571428571428571 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
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
        SignalSpectrum spectrum = Spectrum.Of([-2, -2], new SpectrumOptions { Window = SpectralWindow.Rectangular });

        Assert.Equal([Math.PI, 0.0], spectrum.Angles());
        Assert.Equal([Math.PI, 0.0], spectrum.Phases());
    }

    [Fact]
    public void TheFirstBin_AndTheNyquistBinOfAnEvenTransform_AreReal()
    {
        // The transform of a real signal is real at 0 and at M / 2; written so, the angle there is 0 or +pi, as numpy gives it,
        // and never the -pi a rounding error below zero in the imaginary part would make of it.
        var options = new SpectrumOptions { SampleRate = 1, Window = SpectralWindow.Hamming, PadTo = 14 };

        SignalSpectrum spectrum = Spectrum.Of(Eleven, options);

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
        expected.AssertCloseTo(Spectrum.Unwrap(angles), Tolerance);
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

        SignalSpectrum spectrum = Spectrum.Of(signal, default);

        Enumerable.Range(0, 6).Select(k => 2 * k / 11.0).ToList().AssertCloseTo(spectrum.Frequencies, Tolerance);
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

    // ---- two-sided ---------------------------------------------------------------------------------------------------------

    /// <summary>Seven samples and eight, for the two-sided vectors below (a rectangular window, so the weights do not enter).</summary>
    private static readonly double[] Seven = [0.5, -1, 2, 0.25, 1.5, -0.75, 1.0];

    private static readonly double[] Eight = [1, 0.5, -1, 2, 0.25, 1.5, -0.75, 1.0];

    private static readonly SpectrumOptions TwoSidedRectangular = new() { SampleRate = 1, Window = SpectralWindow.Rectangular, Sides = SpectralSides.TwoSided };

    [Fact]
    public void Magnitude_TwoSided_OddLength_IsMatplotlibs()
    {
        // matplotlib: magnitude_spectrum(Seven, Fs=1, window=window_none, sides='twosided'): the magnitude is even, bin 0 stands in the middle.
        SignalSpectrum spectrum = Spectrum.Of(Seven, TwoSidedRectangular);

        (new double[] { -3.0 / 7, -2.0 / 7, -1.0 / 7, 0, 1.0 / 7, 2.0 / 7, 3.0 / 7 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 0.6184186360568632, 0.3164402563534342, 0.21026095458324176, 0.5, 0.21026095458324176, 0.3164402563534342, 0.6184186360568632 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
    }

    [Fact]
    public void Magnitude_TwoSided_EvenLength_PutsNyquistFirst_AtMinusHalfTheRate()
    {
        // matplotlib: magnitude_spectrum(Eight, Fs=1, window=window_none, sides='twosided'): -0.5 .. 0.375, the bin at -0.5 the Nyquist one.
        SignalSpectrum spectrum = Spectrum.Of(Eight, TwoSidedRectangular);

        (new double[] { -0.5, -0.375, -0.25, -0.125, 0, 0.125, 0.25, 0.375 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 0.6875, 0.2723256421421226, 0.39528470752104744, 0.08871299020932948, 0.5625, 0.08871299020932948, 0.39528470752104744, 0.2723256421421226 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
    }

    [Fact]
    public void Magnitude_TwoSided_Padded_IsMatplotlibs()
    {
        // matplotlib: magnitude_spectrum(Seven, Fs=1, window=window_none, pad_to=10, sides='twosided').
        SignalSpectrum spectrum = Spectrum.Of(Seven, TwoSidedRectangular with { PadTo = 10 });

        (new double[] { -0.5, -0.4, -0.3, -0.2, -0.1, 0, 0.1, 0.2, 0.3, 0.4 }).AssertCloseTo(spectrum.Frequencies, Tolerance);
        (new double[] { 0.9285714285714286, 0.3799151584451273, 0.288260746020179, 0.23647320022452897, 0.30273856751836814, 0.5,
            0.30273856751836814, 0.23647320022452897, 0.28826074602017887, 0.3799151584451273 }).AssertCloseTo(spectrum.Magnitudes(), Tolerance);
    }

    [Fact]
    public void Magnitude_TwoSided_Hann_IsMatplotlibs_AndItsUpperHalfIsTheOneSidedSpectrum()
    {
        // matplotlib: magnitude_spectrum(Eight, Fs=2, sides='twosided') with its default Hann window, and without sides.
        SignalSpectrum both = Spectrum.Of(Eight, new SpectrumOptions { Sides = SpectralSides.TwoSided });
        SignalSpectrum one = Spectrum.Of(Eight, default);

        (new double[] { 0.9790908001995147, 0.5972396558779314, 0.3803603499966434, 0.6237860032351742, 0.6849018386033262,
            0.6237860032351742, 0.3803603499966434, 0.5972396558779314 }).AssertCloseTo(both.Magnitudes(), Tolerance);
        (new double[] { 0.6849018386033262, 0.6237860032351742, 0.3803603499966434, 0.5972396558779314, 0.9790908001995147 }).AssertCloseTo(one.Magnitudes(), Tolerance);
    }

    [Fact]
    public void Angle_TwoSided_OddLength_IsMatplotlibs_OddInFrequency()
    {
        // matplotlib: angle_spectrum(Seven, Fs=1, window=window_none, sides='twosided'): the angle of the negative frequency is minus the positive's.
        double[] angles = Spectrum.Of(Seven, TwoSidedRectangular).Angles();

        (new double[] { -1.3637378504223887, -1.3593465488511514, 2.740207063919485, 0.0, -2.740207063919485, 1.3593465488511514, 1.3637378504223887 }).AssertCloseTo(angles, Tolerance);
    }

    [Fact]
    public void Angle_TwoSided_EvenLength_GivesTheNyquistBinPlusPi_AtMinusHalfTheRate()
    {
        // matplotlib: angle_spectrum(Eight, Fs=1, window=window_none, sides='twosided'): the real, negative Nyquist bin is +pi, as numpy gives it.
        double[] angles = Spectrum.Of(Eight, TwoSidedRectangular).Angles();

        (new double[] { Math.PI, 0.11500567189500142, -0.3217505543966422, -2.781608318567928, 0.0, 2.781608318567928, 0.3217505543966422, -0.11500567189500142 }).AssertCloseTo(angles, Tolerance);
    }

    [Fact]
    public void Phase_TwoSided_IsUnwrappedFromTheMostNegativeFrequencyUp_AsMatplotlibs()
    {
        // matplotlib: phase_spectrum(Seven, Fs=1, window=window_none, sides='twosided') unwraps after it puts the frequencies in order.
        double[] phases = Spectrum.Of(Seven, TwoSidedRectangular).Phases();

        (new double[] { -1.3637378504223887, -1.3593465488511514, -3.542978243260101, -6.283185307179586, -9.02339237109907, -11.20702406550802, -11.202632763936784 }).AssertCloseTo(phases, Tolerance);
    }

    /// <summary>The two-sided angle and the unwrapped phase of seven samples padded to ten, as matplotlib's angle_spectrum and
    /// phase_spectrum give them with window_none, pad_to=10 and sides='twosided'.</summary>
    [Theory]
    [InlineData(false, new[] { 0.0, -1.863848373976588, -1.0997208023323044, -2.898800124529379, 2.084178390201194, 0.0, -2.084178390201194,
        2.898800124529379, 1.0997208023323046, 1.863848373976588 })]
    [InlineData(true, new[] { 0.0, -1.863848373976588, -1.0997208023323044, -2.898800124529379, -4.199006916978393, -6.283185307179587,
        -8.367363697380782, -9.667570489829796, -11.46664981202687, -10.702522240382587 })]
    public void TwoSided_Padded_IsMatplotlibs(bool unwrapped, double[] expected)
    {
        var spectrum = Spectrum.Of(Seven, TwoSidedRectangular with { PadTo = 10 });

        expected.AssertCloseTo(unwrapped ? spectrum.Phases() : spectrum.Angles(), Tolerance);
    }

    [Fact]
    public void TwoSided_WithAMissingSample_IsNaNInEveryBin_AndKeepsTheFrequencies()
    {
        SignalSpectrum spectrum = Spectrum.Of([1, double.NaN, 3, 4], TwoSidedRectangular);

        Assert.Equal(4, spectrum.Frequencies.Length);
        Assert.All(spectrum.Bins, bin => Assert.True(double.IsNaN(bin.Real) && double.IsNaN(bin.Imaginary)));
    }

    [Fact]
    public void DefaultSides_IsTheOneSidedSpectrum()
    {
        SignalSpectrum byDefault = Spectrum.Of(Eleven, default);
        SignalSpectrum oneSided = Spectrum.Of(Eleven, new SpectrumOptions { Sides = SpectralSides.OneSided });

        Assert.Equal(byDefault.Frequencies, oneSided.Frequencies);
        Assert.Equal(byDefault.Bins, oneSided.Bins);
        Assert.Equal(6, byDefault.Bins.Length);
    }
}
