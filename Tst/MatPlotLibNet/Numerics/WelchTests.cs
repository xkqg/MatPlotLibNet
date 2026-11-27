// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Welch"/>: the averaged periodogram matplotlib's <c>mlab.psd</c>, <c>mlab.csd</c> and
/// <c>mlab.cohere</c> compute. The vectors marked "matplotlib" were printed by matplotlib 3.11.2; the others are held to a
/// direct O(N²) sum of the definition written below. Every comparison is by tolerance, never by stored bytes.</summary>
public class WelchTests
{
    private const double Tolerance = 1e-9;

    /// <summary>cos(πn/2) for n = 0..7, written out so no cosine is taken.</summary>
    private static readonly double[] Cosine8 = [1, 0, -1, 0, 1, 0, -1, 0];

    /// <summary>sin(πn/2) for n = 0..7.</summary>
    private static readonly double[] Sine8 = [0, 1, 0, -1, 0, 1, 0, -1];

    private static readonly SpectralOptions Rect8 = new() { SegmentLength = 8, SampleRate = 1, Window = SpectralWindow.Rectangular };

    // ---- matplotlib's vectors ----------------------------------------------------------------------------------------------

    [Fact]
    public void Psd_RectangularCosine_IsMatplotlibs()
    {
        // matplotlib: psd(cos(pi*n/2), NFFT=8, Fs=1, window=window_none) = [0, 0, 4, 0, 0], and sum(P)·df = 0.5.
        SpectralDensity psd = Welch.Psd(Cosine8, Rect8);

        (new double[] { 0, 0.125, 0.25, 0.375, 0.5 }).AssertCloseTo(psd.Frequencies, Tolerance);
        (new double[] { 0, 0, 4, 0, 0 }).AssertCloseTo(psd.Power, Tolerance);
        Assert.Equal(0.5, psd.Power.Sum() * 0.125, Tolerance);
    }

    [Fact]
    public void Psd_RectangularOnes_IsAllAtZero()
        => (new double[] { 8, 0, 0, 0, 0 }).AssertCloseTo(Welch.Psd([1, 1, 1, 1, 1, 1, 1, 1], Rect8).Power, Tolerance);

    [Fact]
    public void Psd_RectangularAlternating_IsAllAtNyquist_NotDoubled()
        => (new double[] { 0, 0, 0, 0, 8 }).AssertCloseTo(Welch.Psd([1, -1, 1, -1, 1, -1, 1, -1], Rect8).Power, Tolerance);

    [Fact]
    public void Psd_HannOnes_IsMatplotlibs()
    {
        // matplotlib: psd(ones(8), NFFT=8, Fs=1) with its default Hann window; sum(w) = 3.5, sum(w²) = 2.625.
        SpectralDensity psd = Welch.Psd([1, 1, 1, 1, 1, 1, 1, 1], new SpectralOptions { SegmentLength = 8, SampleRate = 1 });

        double[] expected = [4.666667, 3.29637, 0.03473, 0.002233, 0];
        for (int k = 0; k < expected.Length; k++) Assert.Equal(expected[k], psd.Power[k], 5e-6);
    }

    [Fact]
    public void Psd_DetrendNone_KeepsTheOffset_WhileMeanRemovesIt()
    {
        // matplotlib: x = 5 + cos(pi*n/2), psd(x, NFFT=8)[0][0] = 59.344 (detrend none, its default) and 0.004341 with
        // detrend='mean'. Hann window, Fs 2.
        double[] x = [.. Cosine8.Select(v => 5 + v)];

        double none = Welch.Psd(x, new SpectralOptions { SegmentLength = 8 }).Power[0];
        double mean = Welch.Psd(x, new SpectralOptions { SegmentLength = 8, Detrend = SpectralDetrend.Mean }).Power[0];

        Assert.Equal(59.344, none, 5e-4);
        Assert.Equal(0.004341, mean, 5e-7);
    }

    [Fact]
    public void Psd_LinearDetrend_OfALine_IsZero()
    {
        double[] line = [.. Enumerable.Range(0, 8).Select(i => 3 + 2.0 * i)];

        SpectralDensity psd = Welch.Psd(line, Rect8 with { Detrend = SpectralDetrend.Linear });

        foreach (double p in psd.Power) Assert.Equal(0.0, p, 1e-20);
    }

    // ---- segments ------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 578.0)]   // K = floor((20 − 8) / 8) + 1 = 2: segments 0..7 and 8..15; 16..19 is left over
    [InlineData(4, 882.0)]   // K = floor((20 − 8) / 4) + 1 = 4: segments at 0, 4, 8, 12
    public void Psd_SegmentCount_IsMatplotlibsRule(int overlap, double dc)
    {
        // x = 0..19, rectangular window, Fs 1: the DC bin of a segment is (its sum)² / 8, and the PSD is their mean.
        // Overlap 0: (28² + 92²) / 2 / 8 = 578. Overlap 4: (28² + 60² + 92² + 124²) / 4 / 8 = 882.
        double[] x = [.. Enumerable.Range(0, 20).Select(i => (double)i)];

        SpectralDensity psd = Welch.Psd(x, Rect8 with { Overlap = overlap });

        Assert.Equal(dc, psd.Power[0], Tolerance);
    }

    // ---- one-sided doubling --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(8, 0)]    // even transform: DC and Nyquist single, bins 1..3 doubled
    [InlineData(7, 0)]    // odd transform: only DC single, bins 1..3 doubled
    [InlineData(8, 9)]    // padded to an odd length: only DC single, bins 1..4 doubled
    [InlineData(8, 16)]   // padded to an even length: DC and Nyquist (bin 8) single
    [InlineData(7, 10)]   // an odd segment padded to an even length: DC and Nyquist (bin 5) single
    public void Psd_OneSided_DoublesEveryBinButDcAndAnEvenNyquist(int length, int padTo)
    {
        double[] x = Signal(length, seed: 3);
        int m = padTo == 0 ? length : padTo;

        SpectralDensity psd = Welch.Psd(x, new SpectralOptions
        {
            SegmentLength = length, SampleRate = 1, Window = SpectralWindow.Rectangular, PadTo = padTo,
        });

        Complex[] spectrum = ((IReadOnlyList<double>)[.. x, .. new double[m - length]]).DirectDft();
        Assert.Equal(m / 2 + 1, psd.Power.Length);
        for (int k = 0; k < psd.Power.Length; k++)
        {
            bool single = k == 0 || (m % 2 == 0 && k == m / 2);
            double expected = (single ? 1 : 2) * Math.Pow(spectrum[k].Magnitude, 2) / length;
            Assert.Equal(expected, psd.Power[k], Tolerance);
            Assert.Equal((double)k / m, psd.Frequencies[k], Tolerance);
        }
    }

    // ---- scaling, Parseval, frequencies --------------------------------------------------------------------------------------

    [Theory]
    [InlineData(8, 1.0)]
    [InlineData(7, 1.0)]
    [InlineData(16, 250.0)]
    public void Psd_Density_SumsToTheSignalPower(int length, double sampleRate)
    {
        // Parseval: with a rectangular window over one segment, Σ P·Δf is the mean of the squares of the signal.
        double[] x = Signal(length, seed: 11);

        SpectralDensity psd = Welch.Psd(x, new SpectralOptions
        {
            SegmentLength = length, SampleRate = sampleRate, Window = SpectralWindow.Rectangular,
        });

        double df = sampleRate / length;
        Assert.Equal(x.Average(v => v * v), psd.Power.Sum() * df, Tolerance);
    }

    /// <summary>Folding the two-sided spectrum onto one side keeps its power: every bin with a mirror image among the bins left out
    /// is doubled, and the Nyquist bin, which is its own mirror image, is that only when the transform's length M (the padded length)
    /// is even. matplotlib's <c>mlab._spectral_helper</c> (3.11.2) decides the Nyquist bin by the parity of the segment length instead
    /// (<c>if not NFFT % 2: slc = slice(1, -1)</c>), which is the same thing only when no padding changes the parity. Printed for
    /// x = [0.5, -1, 2, 0.25, 1.5, -0.75, 1, 0.3], a rectangular window and Fs = 1: <c>psd(NFFT=8, pad_to=9)</c> sums to 7.139656 one-sided
    /// against 10.366875 two-sided (its last bin, 4/9, an ordinary frequency, is left single), and <c>psd(NFFT=7, pad_to=10)</c> to
    /// 19.071429 against 13.035714 (the true Nyquist bin 5 doubled). The library keeps the power in both, as it does with no padding.</summary>
    [Theory]
    [InlineData(8, 9, 10.366875)]
    [InlineData(7, 10, 13.035714)]
    [InlineData(8, 16, 18.43)]
    public void Psd_OneSided_KeepsThePowerOfTheTwoSided_WhenThePaddingChangesTheParity(int length, int padTo, double twoSidedSum)
    {
        double[] x = [.. new double[] { 0.5, -1, 2, 0.25, 1.5, -0.75, 1, 0.3 }.Take(length)];
        var options = new SpectralOptions { SegmentLength = length, SampleRate = 1, Window = SpectralWindow.Rectangular, PadTo = padTo };

        double oneSided = Welch.Psd(x, options).Power.Sum();
        double twoSided = Welch.Psd(x, options with { Sides = SpectralSides.TwoSided }).Power.Sum();

        Assert.Equal(twoSidedSum, twoSided, 6);
        Assert.Equal(twoSided, oneSided, 9);
    }

    [Fact]
    public void Psd_Spectrum_DividesBySumOfWindowSquared()
    {
        // scale_by_freq=False: |X|² / (Σw)², so the cosine's bin 2 is 16 / 64, doubled.
        SpectralDensity psd = Welch.Psd(Cosine8, Rect8 with { Scaling = SpectralScaling.Spectrum });

        (new double[] { 0, 0, 0.5, 0, 0 }).AssertCloseTo(psd.Power, Tolerance);
    }

    [Fact]
    public void Psd_CenterFrequency_ShiftsEveryFrequency()
    {
        SpectralDensity psd = Welch.Psd(Cosine8, Rect8 with { CenterFrequency = 100 });

        (new double[] { 100, 100.125, 100.25, 100.375, 100.5 }).AssertCloseTo(psd.Frequencies, Tolerance);
        (new double[] { 0, 0, 4, 0, 0 }).AssertCloseTo(psd.Power, Tolerance);
    }

    // ---- cross spectrum and coherence ----------------------------------------------------------------------------------------

    [Fact]
    public void Csd_IsConjugateOfXTimesY()
    {
        // matplotlib takes conj(X)·Y: X[2] = 4 for the cosine and Y[2] = −4i for the sine, so bin 2 is −16i / 8, doubled.
        CrossSpectralDensity csd = Welch.Csd(Cosine8, Sine8, Rect8);

        Assert.Equal(0.0, csd.Power[2].Real, Tolerance);
        Assert.Equal(-4.0, csd.Power[2].Imaginary, Tolerance);
        (new double[] { 0, 0.125, 0.25, 0.375, 0.5 }).AssertCloseTo(csd.Frequencies, Tolerance);
    }

    [Fact]
    public void Csd_Swapped_IsTheConjugate()
    {
        double[] x = Signal(64, seed: 1), y = Signal(64, seed: 2);
        var options = new SpectralOptions { SegmentLength = 16, Overlap = 8 };

        Complex[] xy = Welch.Csd(x, y, options).Power;
        Complex[] yx = Welch.Csd(y, x, options).Power;

        for (int k = 0; k < xy.Length; k++)
        {
            Assert.Equal(xy[k].Real, yx[k].Real, Tolerance);
            Assert.Equal(xy[k].Imaginary, -yx[k].Imaginary, Tolerance);
        }
    }

    [Fact]
    public void Csd_OfASignalWithItself_IsItsPsd()
    {
        double[] x = Signal(64, seed: 5);
        var options = new SpectralOptions { SegmentLength = 16, Detrend = SpectralDetrend.Mean };

        Complex[] csd = Welch.Csd(x, x, options).Power;
        double[] psd = Welch.Psd(x, options).Power;

        for (int k = 0; k < psd.Length; k++)
        {
            Assert.Equal(psd[k], csd[k].Real, Tolerance);
            Assert.Equal(0.0, csd[k].Imaginary, Tolerance);
        }
    }

    [Fact]
    public void Coherence_OfASignalWithItself_IsOne()
    {
        // matplotlib: cohere(x, x, NFFT=16) = [1, 1, 1, 1, ...].
        double[] x = Signal(64, seed: 9);

        SpectralDensity coherence = Welch.Coherence(x, x, new SpectralOptions { SegmentLength = 16 });

        Assert.Equal(9, coherence.Power.Length);
        foreach (double c in coherence.Power) Assert.Equal(1.0, c, Tolerance);
    }

    [Fact]
    public void Coherence_IsAlwaysWithinZeroAndOne()
    {
        double[] x = Signal(256, seed: 21), y = Signal(256, seed: 22);
        for (int i = 0; i < y.Length; i++) y[i] += 0.5 * x[i];

        SpectralDensity coherence = Welch.Coherence(x, y, new SpectralOptions { SegmentLength = 32, Overlap = 16 });

        Assert.All(coherence.Power, c => Assert.InRange(c, 0.0, 1.0 + 1e-12));
        Assert.Contains(coherence.Power, c => c < 0.99);
    }

    [Fact]
    public void Coherence_IsTheSquaredCrossOverBothPowers()
    {
        double[] x = Signal(96, seed: 31), y = Signal(96, seed: 32);
        var options = new SpectralOptions { SegmentLength = 16, Overlap = 4, Detrend = SpectralDetrend.Linear };

        double[] coherence = Welch.Coherence(x, y, options).Power;
        Complex[] pxy = Welch.Csd(x, y, options).Power;
        double[] pxx = Welch.Psd(x, options).Power, pyy = Welch.Psd(y, options).Power;

        for (int k = 0; k < coherence.Length; k++)
            Assert.Equal(Math.Pow(pxy[k].Magnitude, 2) / (pxx[k] * pyy[k]), coherence[k], Tolerance);
    }

    // ---- missing samples -----------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Psd_ASegmentWithAMissingSample_IsLeftOut(double missing)
    {
        // Three segments of 4; the middle one holds a missing sample, so the result is the mean over the outer two only.
        double[] x = Signal(12, seed: 41);
        double[] outer = [.. x[..4], .. x[8..]];
        x[5] = missing;
        var options = new SpectralOptions { SegmentLength = 4, SampleRate = 1 };

        double[] gapped = Welch.Psd(x, options).Power;
        double[] expected = Welch.Psd(outer, options).Power;

        for (int k = 0; k < expected.Length; k++) Assert.Equal(expected[k], gapped[k], 1e-12);
    }

    [Fact]
    public void Csd_ASegmentMissingInEitherSignal_IsLeftOut()
    {
        double[] x = Signal(12, seed: 42), y = Signal(12, seed: 43);
        double[] xOuter = [.. x[..4], .. x[8..]], yOuter = [.. y[..4], .. y[8..]];
        y[6] = double.NaN;
        var options = new SpectralOptions { SegmentLength = 4, SampleRate = 1 };

        Complex[] gapped = Welch.Csd(x, y, options).Power;
        Complex[] expected = Welch.Csd(xOuter, yOuter, options).Power;
        double[] coherence = Welch.Coherence(x, y, options).Power;
        double[] expectedCoherence = Welch.Coherence(xOuter, yOuter, options).Power;

        for (int k = 0; k < expected.Length; k++)
        {
            Assert.Equal(expected[k].Real, gapped[k].Real, 1e-12);
            Assert.Equal(expected[k].Imaginary, gapped[k].Imaginary, 1e-12);
            Assert.Equal(expectedCoherence[k], coherence[k], 1e-12);
        }
    }

    [Fact]
    public void EveryFunction_NoSegmentLeft_IsAllMissing_WithTheFrequenciesStillGiven()
    {
        // Two segments of 4, each with a missing sample.
        double[] x = [1, double.NaN, 3, 4, 5, 6, double.NaN, 8];
        double[] y = Signal(8, seed: 44);
        var options = new SpectralOptions { SegmentLength = 4, SampleRate = 1 };

        SpectralDensity psd = Welch.Psd(x, options);
        CrossSpectralDensity csd = Welch.Csd(y, x, options);
        SpectralDensity coherence = Welch.Coherence(x, y, options);

        (new double[] { 0, 0.25, 0.5 }).AssertCloseTo(psd.Frequencies, Tolerance);
        (new double[] { 0, 0.25, 0.5 }).AssertCloseTo(csd.Frequencies, Tolerance);
        (new double[] { 0, 0.25, 0.5 }).AssertCloseTo(coherence.Frequencies, Tolerance);
        Assert.All(psd.Power, p => Assert.True(double.IsNaN(p)));
        Assert.All(csd.Power, p => Assert.True(double.IsNaN(p.Real) && double.IsNaN(p.Imaginary)));
        Assert.All(coherence.Power, p => Assert.True(double.IsNaN(p)));
    }

    // ---- refusals ------------------------------------------------------------------------------------------------------------

    [Fact]
    public void SegmentLongerThanTheSignal_Refuses()
    {
        var error = Assert.Throws<ArgumentException>(() => Welch.Psd(Signal(100, seed: 1), default));
        Assert.Equal("options", error.ParamName);
        Assert.Contains("256", error.Message);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void OverlapNotBelowTheSegmentLength_Refuses(int overlap)
    {
        var error = Assert.Throws<ArgumentException>(() => Welch.Psd(Cosine8, Rect8 with { Overlap = overlap }));
        Assert.Equal("options", error.ParamName);
    }

    [Fact]
    public void PadToBelowTheSegmentLength_Refuses()
    {
        var error = Assert.Throws<ArgumentException>(() => Welch.Psd(Cosine8, Rect8 with { PadTo = 4 }));
        Assert.Equal("options", error.ParamName);
    }

    [Fact]
    public void NullSignal_Refuses()
    {
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Welch.Psd(null!, Rect8)).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Welch.Csd(null!, Cosine8, Rect8)).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Welch.Csd(Cosine8, null!, Rect8)).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Welch.Coherence(Cosine8, null!, Rect8)).ParamName);
    }

    [Fact]
    public void EmptySignal_Refuses()
    {
        Assert.Equal("x", Assert.Throws<ArgumentException>(() => Welch.Psd([], Rect8)).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentException>(() => Welch.Csd(Cosine8, [], Rect8)).ParamName);
    }

    [Fact]
    public void UnequalLengths_Refuse()
    {
        var error = Assert.Throws<ArgumentException>(() => Welch.Csd(Cosine8, [1, 2, 3, 4, 5, 6, 7, 8, 9], Rect8));
        Assert.Equal("y", error.ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentException>(() => Welch.Coherence(Cosine8, [1, 2], Rect8)).ParamName);
    }

    // ---- the oracle ----------------------------------------------------------------------------------------------------------

    /// <summary>A deterministic signal: <see cref="Random"/> with a seed gives the same numbers on every platform.</summary>
    private static double[] Signal(int n, int seed)
    {
        var random = new Random(seed);
        return [.. Enumerable.Range(0, n).Select(_ => random.NextDouble() * 2 - 1)];
    }

    // ---- two-sided ----------------------------------------------------------------------------------------------------------

    /// <summary>Sixteen samples for the two-sided vectors below (the Hann window, mean detrend, segments of 8 that overlap by 4).</summary>
    private static readonly double[] Sixteen = [0.5, -1, 2, 0.25, 1.5, -0.75, 1.0, 0.1, -0.4, 0.9, 1.2, -0.3, 0.7, 0.05, -1.1, 0.6];

    /// <summary>A second signal for the cross density and the coherence: half of <see cref="Sixteen"/> two samples late, and a ramp.</summary>
    private static readonly double[] Other =
        [-0.55, 0.4, 0.45, -0.2, 1.4, 0.625, 1.35, 0.32499999999999996, 1.3, 0.9500000000000001, -0.2, 0.55, 0.8, 0.15, 0.75, 0.525];

    private static readonly SpectralOptions SixteenTwoSided = new()
    {
        SampleRate = 2, SegmentLength = 8, Overlap = 4, Detrend = SpectralDetrend.Mean, Sides = SpectralSides.TwoSided,
    };

    private static readonly double[] Seven = [0.5, -1, 2, 0.25, 1.5, -0.75, 1.0];

    [Fact]
    public void Psd_TwoSided_RectangularCosine_IsMatplotlibs()
    {
        // matplotlib: psd(cos(pi*n/2), NFFT=8, Fs=1, window=window_none, sides='twosided') = [0, 0, 2, 0, 0, 0, 2, 0] at
        // -0.5 .. 0.375: the one-sided 4 at 0.25 is two halves, at +0.25 and at -0.25.
        SpectralDensity psd = Welch.Psd(Cosine8, Rect8 with { Sides = SpectralSides.TwoSided });

        (new double[] { -0.5, -0.375, -0.25, -0.125, 0, 0.125, 0.25, 0.375 }).AssertCloseTo(psd.Frequencies, Tolerance);
        (new double[] { 0, 0, 2, 0, 0, 0, 2, 0 }).AssertCloseTo(psd.Power, Tolerance);
    }

    [Fact]
    public void Psd_TwoSided_OddLength_IsMatplotlibs()
    {
        // matplotlib: psd(Seven, NFFT=7, Fs=1, window=window_none, sides='twosided'); the bin at 0 stands in the middle.
        var options = new SpectralOptions { SegmentLength = 7, SampleRate = 1, Window = SpectralWindow.Rectangular, Sides = SpectralSides.TwoSided };

        SpectralDensity psd = Welch.Psd(Seven, options);

        (new double[] { -3.0 / 7, -2.0 / 7, -1.0 / 7, 0, 1.0 / 7, 2.0 / 7, 3.0 / 7 }).AssertCloseTo(psd.Frequencies, Tolerance);
        (new double[] { 2.677091265957017, 0.70094105088719, 0.3094676831557923, 1.75, 0.3094676831557923, 0.70094105088719, 2.677091265957017 }).AssertCloseTo(psd.Power, Tolerance);
    }

    [Fact]
    public void Psd_TwoSided_PaddedToAnEvenLength_IsMatplotlibs()
    {
        // matplotlib: the same seven samples padded to 10: frequencies -0.5 .. 0.4 in steps of 0.1.
        var options = new SpectralOptions { SegmentLength = 7, PadTo = 10, SampleRate = 1, Window = SpectralWindow.Rectangular, Sides = SpectralSides.TwoSided };

        SpectralDensity psd = Welch.Psd(Seven, options);

        (new double[] { -0.5, -0.4, -0.3, -0.2, -0.1, 0, 0.1, 0.2, 0.3, 0.4 }).AssertCloseTo(psd.Frequencies, Tolerance);
        (new double[] { 6.035714285714286, 1.010348693314703, 0.5816598038727706, 0.3914370209710112, 0.6415544818415149, 1.75,
            0.6415544818415149, 0.3914370209710112, 0.5816598038727706, 1.010348693314703 }).AssertCloseTo(psd.Power, Tolerance);
    }

    [Fact]
    public void Psd_TwoSided_HannMeanDetrendOverlap_IsMatplotlibs()
    {
        // matplotlib: psd(Sixteen, NFFT=8, Fs=2, noverlap=4, detrend='mean', sides='twosided'): frequencies -1 .. 0.75.
        SpectralDensity psd = Welch.Psd(Sixteen, SixteenTwoSided);

        (new double[] { -1, -0.75, -0.5, -0.25, 0, 0.25, 0.5, 0.75 }).AssertCloseTo(psd.Frequencies, Tolerance);
        (new double[] { 0.7724792229739293, 0.3834955004920435, 0.17290752746113383, 0.14666233487484837, 0.06902168999526924,
            0.14666233487484837, 0.17290752746113383, 0.3834955004920435 }).AssertCloseTo(psd.Power, Tolerance);
    }

    [Fact]
    public void Csd_TwoSided_IsMatplotlibs_AndTheNegativeHalfIsTheConjugateOfThePositive()
    {
        // matplotlib: csd(Sixteen, Other, NFFT=8, Fs=2, noverlap=4, sides='twosided'), no detrend.
        CrossSpectralDensity csd = Welch.Csd(Sixteen, Other, SixteenTwoSided with { Detrend = SpectralDetrend.None });

        (new double[] { 0.32402913859150007, 0.12123308392998092, -0.06369294194879169, 0.14849801535731827, 0.6232315085239646,
            0.14849801535731827, -0.06369294194879169, 0.12123308392998092 }).AssertCloseTo([.. csd.Power.Select(c => c.Real)], Tolerance);
        (new double[] { 0.0, -0.18494050545780566, -0.018479881673531946, 0.19481465542250234, 0.0,
            -0.19481465542250234, 0.018479881673531946, 0.18494050545780566 }).AssertCloseTo([.. csd.Power.Select(c => c.Imaginary)], Tolerance);
    }

    [Fact]
    public void Coherence_TwoSided_IsMatplotlibs()
    {
        // matplotlib: cohere(Sixteen, Other, NFFT=8, Fs=2, noverlap=4, sides='twosided'), no detrend.
        SpectralDensity coherence = Welch.Coherence(Sixteen, Other, SixteenTwoSided with { Detrend = SpectralDetrend.None });

        (new double[] { 0.5998450922800377, 0.5223966517897368, 0.20561268925243512, 0.44413068823263596, 0.7405646516089317,
            0.44413068823263596, 0.20561268925243512, 0.5223966517897368 }).AssertCloseTo(coherence.Power, Tolerance);
    }

    [Fact]
    public void Psd_TwoSided_NFFT64_RunsFromMinusNyquist_ToOneBinBelowNyquist()
    {
        // matplotlib: psd(x, NFFT=64, Fs=2, sides='twosided') has 64 frequencies, -1.0 to 0.96875, and 0 in the middle.
        SpectralDensity psd = Welch.Psd(Signal(64, seed: 5), new SpectralOptions { SegmentLength = 64, Sides = SpectralSides.TwoSided });

        Assert.Equal(64, psd.Frequencies.Length);
        Assert.Equal(-1.0, psd.Frequencies[0], Tolerance);
        Assert.Equal(0.96875, psd.Frequencies[^1], Tolerance);
        Assert.Equal(0.0, psd.Frequencies[32], Tolerance);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(7, 0)]
    [InlineData(8, 9)]
    [InlineData(8, 16)]
    public void Psd_TwoSided_IsTheOneSidedFoldedOut_HalfThePowerAtEveryBinButDcAndAnEvenNyquist(int length, int padTo)
    {
        // The one-sided power at a frequency is the two-sided power at it plus at its negative, which are equal for a real signal.
        int m = padTo == 0 ? length : padTo;
        var one = new SpectralOptions { SegmentLength = length, SampleRate = m, PadTo = padTo, Window = SpectralWindow.Rectangular };
        double[] x = Signal(length, seed: 3);

        SpectralDensity folded = Welch.Psd(x, one);
        SpectralDensity both = Welch.Psd(x, one with { Sides = SpectralSides.TwoSided });

        Assert.Equal(m, both.Power.Length);
        for (int k = 0; k < folded.Power.Length; k++)
        {
            bool single = k == 0 || (m % 2 == 0 && k == m / 2);
            int at = Array.FindIndex(both.Frequencies, f => Math.Abs(f - (k == m / 2 && m % 2 == 0 ? -k : k)) < 1e-9);
            Assert.True(at >= 0, $"no bin at {k}");
            Assert.Equal(single ? folded.Power[k] : folded.Power[k] / 2, both.Power[at], Tolerance);
            if (!single) Assert.Equal(both.Power[at], both.Power[Array.FindIndex(both.Frequencies, f => Math.Abs(f + k) < 1e-9)], Tolerance);
        }
    }

    [Fact]
    public void Psd_DefaultSides_IsTheOneSidedSpectrum()
    {
        double[] x = Signal(32, seed: 9);
        var options = new SpectralOptions { SegmentLength = 16, SampleRate = 1 };

        SpectralDensity byDefault = Welch.Psd(x, options);
        SpectralDensity oneSided = Welch.Psd(x, options with { Sides = SpectralSides.OneSided });

        Assert.Equal(byDefault.Frequencies, oneSided.Frequencies);
        Assert.Equal(byDefault.Power, oneSided.Power);
        Assert.Equal(9, byDefault.Power.Length);
    }

    [Fact]
    public void Psd_TwoSided_WithNoSegmentUsable_IsNaNInEveryBin_AndKeepsTheFrequencies()
    {
        double[] x = [1, 2, double.NaN, 4, 5, 6, 7, 8];

        SpectralDensity psd = Welch.Psd(x, Rect8 with { Sides = SpectralSides.TwoSided });

        Assert.Equal(8, psd.Frequencies.Length);
        Assert.All(psd.Power, p => Assert.True(double.IsNaN(p)));
    }
}
