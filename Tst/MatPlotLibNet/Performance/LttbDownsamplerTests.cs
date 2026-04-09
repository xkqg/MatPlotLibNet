// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Downsampling;

namespace MatPlotLibNet.Tests.Performance;

/// <summary>Verifies <see cref="LttbDownsampler"/> behavior.</summary>
public class LttbDownsamplerTests
{
    private static XYData MakeLine(int n)
    {
        double[] x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        double[] y = x.Select(v => v * 2).ToArray();
        return new(x, y);
    }

    /// <summary>Verifies that the downsampler implements IDownsampler.</summary>
    [Fact]
    public void ImplementsIDownsampler()
    {
        IDownsampler ds = new LttbDownsampler();
        Assert.NotNull(ds);
    }

    /// <summary>Verifies that output has exactly targetPoints when input exceeds target.</summary>
    [Theory]
    [InlineData(1000, 100)]
    [InlineData(500, 50)]
    [InlineData(10000, 200)]
    public void Downsample_OutputCountEqualsTarget(int inputCount, int target)
    {
        var ds = new LttbDownsampler();
        var (x, y) = MakeLine(inputCount);
        var (outX, outY) = ds.Downsample(x, y, target);
        Assert.Equal(target, outX.Length);
        Assert.Equal(target, outY.Length);
    }

    /// <summary>Verifies that when input is smaller than target, all points are returned.</summary>
    [Fact]
    public void Downsample_InputSmallerThanTarget_ReturnsAll()
    {
        var ds = new LttbDownsampler();
        var (x, y) = MakeLine(10);
        var (outX, outY) = ds.Downsample(x, y, 50);
        Assert.Equal(10, outX.Length);
        Assert.Equal(10, outY.Length);
    }

    /// <summary>Verifies that the first and last points are always preserved.</summary>
    [Fact]
    public void Downsample_PreservesFirstAndLastPoint()
    {
        var ds = new LttbDownsampler();
        double[] x = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        double[] y = [10, 5, 3, 8, 2, 9, 1, 7, 4, 6];
        var (outX, outY) = ds.Downsample(x, y, 5);
        Assert.Equal(0, outX[0]);
        Assert.Equal(10, outY[0]);
        Assert.Equal(9, outX[^1]);
        Assert.Equal(6, outY[^1]);
    }

    /// <summary>Verifies that a sharp peak is preserved by LTTB.</summary>
    [Fact]
    public void Downsample_PreservesSharpPeak()
    {
        var ds = new LttbDownsampler();
        // Flat line with a single spike at index 50
        double[] x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        double[] y = x.Select(v => v == 50 ? 1000.0 : 0.0).ToArray();
        var (outX, outY) = ds.Downsample(x, y, 20);
        // The spike (x=50, y=1000) should appear in the output
        bool spikePreserved = outX.Zip(outY).Any(p => p.First == 50 && p.Second == 1000);
        Assert.True(spikePreserved, "LTTB should preserve the sharp peak at x=50");
    }

    /// <summary>Verifies that X output values are a subset of input X values.</summary>
    [Fact]
    public void Downsample_OutputXValuesAreSubsetOfInput()
    {
        var ds = new LttbDownsampler();
        var (x, y) = MakeLine(500);
        var (outX, _) = ds.Downsample(x, y, 50);
        Assert.All(outX, v => Assert.Contains(v, x));
    }

    /// <summary>Verifies empty input returns empty output.</summary>
    [Fact]
    public void Downsample_EmptyInput_ReturnsEmpty()
    {
        var ds = new LttbDownsampler();
        var (outX, outY) = ds.Downsample([], [], 10);
        Assert.Empty(outX);
        Assert.Empty(outY);
    }
}
