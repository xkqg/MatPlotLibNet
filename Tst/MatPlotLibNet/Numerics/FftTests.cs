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
}
