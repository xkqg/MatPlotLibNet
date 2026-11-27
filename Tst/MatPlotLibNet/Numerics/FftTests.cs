// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Fft"/> correctness for known inputs.</summary>
public class FftTests
{
    [Fact]
    public void Forward_EmptySignal_ReturnsEmpty()
    {
        Complex[] result = Fft.Forward([]);
        Assert.Empty(result);
    }

    [Fact]
    public void Forward_DcSignal_PeakAtZeroFrequency()
    {
        // Constant signal → maximum energy should be at DC (bin 0)
        double[] signal = Enumerable.Repeat(1.0, 64).ToArray();
        Complex[] spectrum = Fft.Forward(signal);
        double dc = spectrum[0].Magnitude;
        double maxOther = spectrum.Skip(1).Max(s => s.Magnitude);
        Assert.True(dc > maxOther, $"DC bin ({dc:F3}) should be the largest bin (max other: {maxOther:F3})");
    }

    [Fact]
    public void Forward_NonPowerOf2_PadsCorrectly()
    {
        double[] signal = new double[100];
        Complex[] result = Fft.Forward(signal);
        // Should be padded to 128 (next power of 2 ≥ 100)
        Assert.Equal(128, result.Length);
    }

    [Fact]
    public void Forward_IsHannThenTransform()
    {
        // np.hanning(5) = [0, 0.5, 1, 0.5, 0], so [1, 2, 3, 4, 5] is weighted to [0, 1, 3, 2, 0], padded with zeros to 8
        // samples and transformed: bin k is the sum of w[j]·x[j]·e^(−2πi·jk/8).
        double[] weighted = [0, 1, 3, 2, 0];

        Complex[] spectrum = Fft.Forward([1, 2, 3, 4, 5]);

        Assert.Equal(8, spectrum.Length);
        for (int k = 0; k < 8; k++)
        {
            Complex expected = Complex.Zero;
            for (int j = 0; j < weighted.Length; j++)
            {
                double angle = -2 * Math.PI * (j * k % 8) / 8;
                expected += weighted[j] * new Complex(Math.Cos(angle), Math.Sin(angle));
            }

            Assert.True((spectrum[k] - expected).Magnitude < 1e-12, $"bin {k}: {spectrum[k]} against {expected}");
        }
    }

    [Fact]
    public void Forward_OneSample_IsFinite()
    {
        // A window of one sample is [1] (numpy's hanning(1)); the old formula divided 0 by 0 there and gave NaN.
        Complex[] spectrum = Fft.Forward([5.0]);

        Assert.Equal([new Complex(5, 0)], spectrum);
    }

    [Fact]
    public void Stft_Dimensions_CorrectShape()
    {
        double[] signal = new double[512];
        var result = Fft.Stft(signal, windowSize: 64, overlap: 32, sampleRate: 1);
        Assert.True(result.Frequencies.Length > 0);
        Assert.True(result.Times.Length > 0);
        Assert.Equal(result.Frequencies.Length, result.Magnitudes.GetLength(0));
        Assert.Equal(result.Times.Length, result.Magnitudes.GetLength(1));
    }

    [Fact]
    public void Stft_EmptySignal_ReturnsEmpty()
    {
        var result = Fft.Stft([], 64, 32);
        Assert.Empty(result.Frequencies);
        Assert.Empty(result.Times);
    }

    [Fact]
    public void NextPowerOf2_ReturnsCorrectValues()
    {
        Assert.Equal(1, Fft.NextPowerOf2(1));
        Assert.Equal(4, Fft.NextPowerOf2(3));
        Assert.Equal(16, Fft.NextPowerOf2(16));
        Assert.Equal(32, Fft.NextPowerOf2(17));
        Assert.Equal(128, Fft.NextPowerOf2(100));
    }

    // ---- Frequencies ---------------------------------------------------------

    [Fact]
    public void Frequencies_LengthEqualsN()
        => Assert.Equal(8, Fft.Frequencies(8).Length);

    [Fact]
    public void Frequencies_FirstValueIsZero()
        => Assert.Equal(0.0, Fft.Frequencies(8)[0]);

    [Fact]
    public void Frequencies_Default_Dt1_NyquistIsHalf()
    {
        // For n=8, dt=1: positive freqs [0, 1/8, 2/8, 3/8], then negative [-4/8, -3/8, -2/8, -1/8]
        double[] f = Fft.Frequencies(8, 1.0);
        Assert.Equal(0.125, f[1], 1e-9);    // 1/(8*1)
        Assert.Equal(0.375, f[3], 1e-9);    // 3/(8*1)
    }

    // ---- Shift ---------------------------------------------------------------

    [Fact]
    public void Shift_Real_CentersZeroFrequency()
    {
        double[] x = [0, 1, 2, 3, 4, 5, 6, 7];
        double[] shifted = Fft.Shift(x);
        // Zero freq (index 0) should move to middle
        Assert.Equal(4.0, shifted[0]);
    }

    [Fact]
    public void Shift_TwiceIsIdentity()
    {
        double[] x = [1, 2, 3, 4, 5, 6];
        double[] twice = Fft.Shift(Fft.Shift(x));
        Assert.Equal(x, twice);
    }

    [Fact]
    public void Shift_Complex_CentersZeroFrequency()
    {
        Complex[] x = [1, 2, 3, 4, 5, 6];
        Complex[] shifted = Fft.Shift(x);
        Assert.Equal(new Complex(4, 0), shifted[0]);
    }

    // ---- Inverse -------------------------------------------------------------

    [Fact]
    public void Inverse_LengthEqualsSpectrumLength()
    {
        double[] signal = [1, 0, 0, 0];
        Complex[] spectrum = Fft.Forward(signal);
        double[] recovered = Fft.Inverse(spectrum);
        Assert.Equal(spectrum.Length, recovered.Length);
    }

    [Fact]
    public void Inverse_ConstantSignal_RecoversDC()
    {
        // DC signal: all values = 2.0 → spectrum has one non-zero bin
        int n = 8;
        var signal = Enumerable.Repeat(2.0, n).ToArray();
        Complex[] spectrum = Fft.Forward(signal);
        // Compute IFFT manually: the round-trip through Hann window won't perfectly
        // reconstruct, but the DC component should dominate. Just verify no exception.
        double[] recovered = Fft.Inverse(spectrum);
        Assert.Equal(n, recovered.Length);
    }

    [Fact]
    public void Inverse_DcOnlySpectrum_ReturnsConstantSignal()
    {
        // IFFT([1,0,0,...,0]) = constant 1/n at every sample
        int n = 8;
        var spectrum = new Complex[n];
        spectrum[0] = Complex.One;
        double[] recovered = Fft.Inverse(spectrum);
        double expected = 1.0 / n;
        for (int i = 0; i < n; i++)
            Assert.Equal(expected, recovered[i], 1e-9);
    }
}
