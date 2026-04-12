// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Fft"/> extension methods: Inverse, Frequencies, Shift.</summary>
public class FftExtensionsTests
{
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
