// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="SpectralDetrend"/>: what is taken out of each segment before its spectrum is computed —
/// nothing (matplotlib's <c>detrend_none</c>, the default), the mean (<c>detrend_mean</c>), the least-squares line
/// (<c>detrend_linear</c>), or a function of the caller's own through <see cref="SpectralDetrend.Of"/>.</summary>
public class SpectralDetrendTests
{
    [Fact]
    public void None_IsWhatDefaultMeans() => Assert.Equal(default, SpectralDetrend.None);

    [Fact]
    public void None_KeepsTheSegment()
    {
        double[] segment = [5, 6, 8];

        double[] result = SpectralDetrend.None.Removed(segment);

        Assert.Equal([5.0, 6.0, 8.0], result);
    }

    [Fact]
    public void Mean_SubtractsTheMean()
    {
        double[] segment = [1, 2, 6];

        double[] result = SpectralDetrend.Mean.Removed(segment);

        Assert.Equal([-2.0, -1.0, 3.0], result);
        Assert.Equal([1.0, 2.0, 6.0], segment);
    }

    [Fact]
    public void Linear_OfALine_IsZero()
    {
        double[] line = [3, 5, 7, 9, 11];

        double[] result = SpectralDetrend.Linear.Removed(line);

        foreach (double value in result) Assert.Equal(0.0, value, 1e-12);
    }

    [Fact]
    public void Linear_RemovesTheLeastSquaresLine()
    {
        // [0, 1, 0, 1]: the least-squares line through (0,0), (1,1), (2,0), (3,1) is 0.2 + 0.2·i
        // (numpy: polyfit(arange(4), [0,1,0,1], 1) = [0.2, 0.2]), so the residuals are [-0.2, 0.6, -0.6, 0.2].
        double[] segment = [0, 1, 0, 1];

        double[] result = SpectralDetrend.Linear.Removed(segment);

        double[] expected = [-0.2, 0.6, -0.6, 0.2];
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], result[i], 1e-12);
        Assert.Equal([0.0, 1.0, 0.0, 1.0], segment);
    }

    [Fact]
    public void Linear_OneSample_IsZero()
        => Assert.Equal([0.0], SpectralDetrend.Linear.Removed([7.5]));

    [Fact]
    public void Of_GivesWhatTheFunctionGives()
    {
        var halved = SpectralDetrend.Of(s => [.. s.Select(v => v / 2)]);

        Assert.Equal([1.0, 2.0], halved.Removed([2, 4]));
    }

    [Fact]
    public void Of_NullFunction_Refuses()
        => Assert.Throws<ArgumentNullException>(() => SpectralDetrend.Of(null!));

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Of_WrongNumberOfValues_Refuses(int given)
    {
        var detrend = SpectralDetrend.Of(_ => new double[given]);

        var error = Assert.Throws<InvalidOperationException>(() => detrend.Removed([1, 2]));
        Assert.Contains("2", error.Message);
    }

    [Fact]
    public void Of_NoValues_Refuses()
        => Assert.Throws<InvalidOperationException>(() => SpectralDetrend.Of(_ => null!).Removed([1, 2]));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Of_NonFiniteValue_Refuses(double bad)
        => Assert.Throws<InvalidOperationException>(() => SpectralDetrend.Of(_ => [1, bad]).Removed([1, 2]));
}
