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

        AssertClose([0, 0.125, 0.25, 0.375, 0.5], psd.Frequencies);
        AssertClose([0, 0, 4, 0, 0], psd.Power);
        Assert.Equal(0.5, psd.Power.Sum() * 0.125, Tolerance);
    }

    [Fact]
    public void Psd_RectangularOnes_IsAllAtZero()
        => AssertClose([8, 0, 0, 0, 0], Welch.Psd([1, 1, 1, 1, 1, 1, 1, 1], Rect8).Power);

    [Fact]
    public void Psd_RectangularAlternating_IsAllAtNyquist_NotDoubled()
        => AssertClose([0, 0, 0, 0, 8], Welch.Psd([1, -1, 1, -1, 1, -1, 1, -1], Rect8).Power);

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
    public void Psd_OneSided_DoublesEveryBinButDcAndAnEvenNyquist(int length, int padTo)
    {
        double[] x = Signal(length, seed: 3);
        int m = padTo == 0 ? length : padTo;

        SpectralDensity psd = Welch.Psd(x, new SpectralOptions
        {
            SegmentLength = length, SampleRate = 1, Window = SpectralWindow.Rectangular, PadTo = padTo,
        });

        Complex[] spectrum = DirectDft([.. x, .. new double[m - length]]);
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

    [Fact]
    public void Psd_Spectrum_DividesBySumOfWindowSquared()
    {
        // scale_by_freq=False: |X|² / (Σw)², so the cosine's bin 2 is 16 / 64, doubled.
        SpectralDensity psd = Welch.Psd(Cosine8, Rect8 with { Scaling = SpectralScaling.Spectrum });

        AssertClose([0, 0, 0.5, 0, 0], psd.Power);
    }

    [Fact]
    public void Psd_CenterFrequency_ShiftsEveryFrequency()
    {
        SpectralDensity psd = Welch.Psd(Cosine8, Rect8 with { CenterFrequency = 100 });

        AssertClose([100, 100.125, 100.25, 100.375, 100.5], psd.Frequencies);
        AssertClose([0, 0, 4, 0, 0], psd.Power);
    }

    // ---- cross spectrum and coherence ----------------------------------------------------------------------------------------

    [Fact]
    public void Csd_IsConjugateOfXTimesY()
    {
        // matplotlib takes conj(X)·Y: X[2] = 4 for the cosine and Y[2] = −4i for the sine, so bin 2 is −16i / 8, doubled.
        CrossSpectralDensity csd = Welch.Csd(Cosine8, Sine8, Rect8);

        Assert.Equal(0.0, csd.Power[2].Real, Tolerance);
        Assert.Equal(-4.0, csd.Power[2].Imaginary, Tolerance);
        AssertClose([0, 0.125, 0.25, 0.375, 0.5], csd.Frequencies);
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

        AssertClose([0, 0.25, 0.5], psd.Frequencies);
        AssertClose([0, 0.25, 0.5], csd.Frequencies);
        AssertClose([0, 0.25, 0.5], coherence.Frequencies);
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

    /// <summary>The definition of the transform, summed directly; the phase index is reduced modulo N.</summary>
    private static Complex[] DirectDft(double[] x)
    {
        int n = x.Length;
        var result = new Complex[n];
        for (int k = 0; k < n; k++)
        {
            Complex sum = Complex.Zero;
            for (int j = 0; j < n; j++)
            {
                double angle = -2 * Math.PI * ((long)k * j % n) / n;
                sum += x[j] * new Complex(Math.Cos(angle), Math.Sin(angle));
            }

            result[k] = sum;
        }

        return result;
    }

    private static void AssertClose(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int k = 0; k < expected.Length; k++) Assert.Equal(expected[k], actual[k], Tolerance);
    }
}
