// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Fft.Transform"/>: the discrete Fourier transform of exactly the length it is given, radix-2
/// for a power of two and Bluestein's chirp-z transform for any other length, held to a direct O(N²) sum.</summary>
public class FftTransformTests
{
    [Fact]
    public void Transform_Empty_IsEmpty() => Assert.Empty(Fft.Transform([]));

    [Fact]
    public void Transform_OneSample_IsACopy()
    {
        Complex[] x = [new(2.5, -1)];

        Complex[] result = Fft.Transform(x);

        Assert.Equal(x, result);
        Assert.NotSame(x, result);
    }

    [Theory]
    [InlineData(8)]   // radix-2
    [InlineData(6)]   // Bluestein
    public void Transform_Impulse_IsFlat(int n)
    {
        var x = new Complex[n];
        x[0] = Complex.One;

        Complex[] result = Fft.Transform(x);

        Assert.Equal(n, result.Length);
        foreach (Complex bin in result)
        {
            Assert.Equal(1.0, bin.Real, 1e-12);
            Assert.Equal(0.0, bin.Imaginary, 1e-12);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(64)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Transform_AgreesWithTheDirectSum(int n)
    {
        Complex[] x = Signal(n);

        Complex[] result = Fft.Transform(x);

        Assert.Equal(n, result.Length);
        Complex[] expected = DirectDft(x);
        double scale = Math.Max(1.0, x.Sum(v => v.Magnitude));
        for (int k = 0; k < n; k++)
        {
            Assert.True((result[k] - expected[k]).Magnitude <= 1e-9 * scale,
                $"bin {k} of {n}: {result[k]} against the direct sum {expected[k]}");
        }
    }

    [Fact]
    public void Transform_Bluestein_ToneLandsInItsOwnBin()
    {
        // 100 is not a power of two, so this goes through the chirp-z path. A cosine of 7 cycles in 100 samples puts
        // N/2 = 50 in bins 7 and 93 and nothing anywhere else; a padded transform would smear it over neighbouring bins.
        const int n = 100;
        var x = new Complex[n];
        for (int i = 0; i < n; i++) x[i] = Math.Cos(2 * Math.PI * 7 * i / n);

        Complex[] result = Fft.Transform(x);

        for (int k = 0; k < n; k++)
        {
            double expected = k is 7 or 93 ? 50.0 : 0.0;
            Assert.Equal(expected, result[k].Magnitude, 1e-9);
        }
    }

    [Fact]
    public void Transform_LeavesItsInputAsItWas()
    {
        Complex[] x = Signal(12);
        Complex[] before = [.. x];

        Fft.Transform(x);

        Assert.Equal(before, x);
    }

    /// <summary>A deterministic complex signal with no symmetry that would hide a wrong bin.</summary>
    private static Complex[] Signal(int n)
    {
        var x = new Complex[n];
        for (int i = 0; i < n; i++) x[i] = new Complex((i % 7) - 3 + 0.25 * i / n, 0.5 * ((i % 5) - 2));
        return x;
    }

    /// <summary>The definition of the transform, summed directly. The phase index is reduced modulo N so the angle stays small.</summary>
    private static Complex[] DirectDft(Complex[] x)
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
}
