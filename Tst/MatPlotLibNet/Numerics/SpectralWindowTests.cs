// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="SpectralWindow"/>: the symmetric windows numpy's <c>hanning</c>, <c>hamming</c> and
/// <c>blackman</c> give, the rectangular window, and a window of your own through <see cref="SpectralWindow.Of"/>.</summary>
public class SpectralWindowTests
{
    public static TheoryData<string> Windows => ["Hann", "Hamming", "Blackman", "Rectangular"];

    private static SpectralWindow Named(string name) => name switch
    {
        "Hann" => SpectralWindow.Hann,
        "Hamming" => SpectralWindow.Hamming,
        "Blackman" => SpectralWindow.Blackman,
        _ => SpectralWindow.Rectangular,
    };

    [Fact]
    public void Hann_Eight_IsNumpysHanning()
    {
        // np.hanning(8): the symmetric window matplotlib's psd uses. Its sum is 3.5 and the sum of its squares 2.625; scipy's
        // periodic Hann would sum to 4.
        double[] expected = [0, 0.1882550990706332, 0.6112604669781572, 0.9504844339512095,
                             0.9504844339512095, 0.6112604669781572, 0.1882550990706332, 0];

        double[] w = SpectralWindow.Hann.Coefficients(8);

        Assert.Equal(8, w.Length);
        for (int k = 0; k < 8; k++) Assert.Equal(expected[k], w[k], 1e-12);
        Assert.Equal(3.5, w.Sum(), 1e-12);
        Assert.Equal(2.625, w.Sum(v => v * v), 1e-12);
    }

    [Fact]
    public void Hann_IsWhatDefaultMeans()
        => Assert.Equal(SpectralWindow.Hann.Coefficients(7), default(SpectralWindow).Coefficients(7));

    [Fact]
    public void Hamming_Five_IsNumpysHamming()
        => AssertClose([0.08, 0.54, 1, 0.54, 0.08], SpectralWindow.Hamming.Coefficients(5));

    [Fact]
    public void Blackman_Five_IsNumpysBlackman()
        => AssertClose([0, 0.34, 1, 0.34, 0], SpectralWindow.Blackman.Coefficients(5));

    [Fact]
    public void Rectangular_IsAllOnes()
        => Assert.Equal([1.0, 1.0, 1.0, 1.0], SpectralWindow.Rectangular.Coefficients(4));

    [Theory]
    [MemberData(nameof(Windows))]
    public void OneSample_IsOne(string name)
        => Assert.Equal([1.0], Named(name).Coefficients(1));

    [Theory]
    [MemberData(nameof(Windows))]
    public void IsSymmetric(string name)
    {
        double[] w = Named(name).Coefficients(9);

        for (int k = 0; k < 9; k++) Assert.Equal(w[k], w[8 - k], 1e-12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void NoSamples_Refuses(int n)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpectralWindow.Hann.Coefficients(n));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpectralWindow.Rectangular.Coefficients(n));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpectralWindow.Of(m => new double[m]).Coefficients(n));
    }

    [Fact]
    public void Of_GivesTheCoefficientsOfTheFunction()
    {
        var triangle = SpectralWindow.Of(n => [.. Enumerable.Range(0, n).Select(k => 1.0 - Math.Abs(k - (n - 1) / 2.0) / n)]);

        AssertClose([0.6, 0.8, 1.0, 0.8, 0.6], triangle.Coefficients(5));
    }

    [Fact]
    public void Of_NullFunction_Refuses()
        => Assert.Throws<ArgumentNullException>(() => SpectralWindow.Of(null!));

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Of_WrongNumberOfCoefficients_Refuses(int given)
    {
        var window = SpectralWindow.Of(_ => new double[given]);

        var error = Assert.Throws<InvalidOperationException>(() => window.Coefficients(4));
        Assert.Contains("4", error.Message);
    }

    [Fact]
    public void Of_NoCoefficients_Refuses()
        => Assert.Throws<InvalidOperationException>(() => SpectralWindow.Of(_ => null!).Coefficients(4));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Of_NonFiniteCoefficient_Refuses(double bad)
        => Assert.Throws<InvalidOperationException>(() => SpectralWindow.Of(n => [1, bad, 1]).Coefficients(3));

    private static void AssertClose(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int k = 0; k < expected.Length; k++) Assert.Equal(expected[k], actual[k], 1e-12);
    }
}
