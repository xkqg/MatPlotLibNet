// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="GaussianKde"/> bandwidth and evaluation math.</summary>
public class GaussianKdeTests
{
    // --- SilvermanBandwidth ---

    /// <summary>Bandwidth is positive for typical data.</summary>
    [Fact]
    public void SilvermanBandwidth_PositiveResult_ForTypicalData()
    {
        double[] data = [1.0, 2.0, 3.0, 4.0, 5.0];
        double bw = GaussianKde.SilvermanBandwidth(data);
        Assert.True(bw > 0);
    }

    /// <summary>Bandwidth follows 1.06 * σ * n^(-0.2) for known data.</summary>
    [Fact]
    public void SilvermanBandwidth_FollowsFormula()
    {
        // data = {1,2,3,4,5}: mean=3, var=2.5, σ=√2.5, n=5
        double[] data = [1.0, 2.0, 3.0, 4.0, 5.0];
        double sigma = Math.Sqrt(2.5);
        double expected = 1.06 * sigma * Math.Pow(5, -0.2);
        double actual = GaussianKde.SilvermanBandwidth(data);
        Assert.Equal(expected, actual, precision: 10);
    }

    /// <summary>Single-point data returns a fallback bandwidth of 1.0.</summary>
    [Fact]
    public void SilvermanBandwidth_SinglePoint_ReturnsFallback()
    {
        double bw = GaussianKde.SilvermanBandwidth([42.0]);
        Assert.Equal(1.0, bw);
    }

    /// <summary>Constant data (σ=0) returns fallback bandwidth of 1.0.</summary>
    [Fact]
    public void SilvermanBandwidth_ConstantData_ReturnsFallback()
    {
        double[] data = [5.0, 5.0, 5.0, 5.0];
        double bw = GaussianKde.SilvermanBandwidth(data);
        Assert.Equal(1.0, bw, precision: 10);
    }

    // --- Evaluate ---

    /// <summary>Evaluate returns exactly numPoints evaluation points.</summary>
    [Fact]
    public void Evaluate_ReturnsRequestedNumberOfPoints()
    {
        double[] sorted = [1.0, 2.0, 3.0];
        var (xs, density) = GaussianKde.Evaluate(sorted, 0.5, numPoints: 50);
        Assert.Equal(50, xs.Length);
        Assert.Equal(50, density.Length);
    }

    /// <summary>Default numPoints is 100.</summary>
    [Fact]
    public void Evaluate_DefaultNumPoints_Is100()
    {
        double[] sorted = [1.0, 2.0, 3.0];
        var (xs, density) = GaussianKde.Evaluate(sorted, 0.5);
        Assert.Equal(100, xs.Length);
    }

    /// <summary>Density peak is near the mean for symmetric data.</summary>
    [Fact]
    public void Evaluate_PeakNearMean_ForSymmetricData()
    {
        double[] sorted = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double bw = GaussianKde.SilvermanBandwidth(sorted);
        var (xs, density) = GaussianKde.Evaluate(sorted, bw);
        int peakIdx = Array.IndexOf(density, density.Max());
        double peakX = xs[peakIdx];
        Assert.True(Math.Abs(peakX) < 0.5, $"Peak at {peakX}, expected near 0");
    }

    /// <summary>All density values are non-negative.</summary>
    [Fact]
    public void Evaluate_AllDensitiesNonNegative()
    {
        double[] sorted = [1.0, 2.0, 3.0, 4.0, 5.0];
        double bw = GaussianKde.SilvermanBandwidth(sorted);
        var (_, density) = GaussianKde.Evaluate(sorted, bw);
        Assert.All(density, d => Assert.True(d >= 0));
    }

    /// <summary>Density integrates approximately to 1 (trapezoidal rule).</summary>
    [Fact]
    public void Evaluate_IntegratesApproximatelyToOne()
    {
        double[] sorted = [1.0, 2.0, 2.5, 3.0, 4.0, 5.0, 5.5, 6.0];
        double bw = GaussianKde.SilvermanBandwidth(sorted);
        var (xs, density) = GaussianKde.Evaluate(sorted, bw, numPoints: 500);
        double integral = 0;
        for (int i = 1; i < xs.Length; i++)
            integral += 0.5 * (density[i - 1] + density[i]) * (xs[i] - xs[i - 1]);
        Assert.True(Math.Abs(integral - 1.0) < 0.02, $"Integral = {integral}, expected ~1.0");
    }

    /// <summary>Empty data returns empty arrays without throwing.</summary>
    [Fact]
    public void Evaluate_EmptyData_ReturnsEmpty()
    {
        var (xs, density) = GaussianKde.Evaluate([], 1.0);
        Assert.Empty(xs);
        Assert.Empty(density);
    }

    /// <summary>X range spans [min-3h, max+3h].</summary>
    [Fact]
    public void Evaluate_XRange_CoversDataPlusThreeBandwidths()
    {
        double[] sorted = [2.0, 3.0, 4.0];
        double bw = 0.5;
        var (xs, _) = GaussianKde.Evaluate(sorted, bw);
        Assert.Equal(sorted[0] - 3 * bw, xs[0], precision: 10);
        Assert.Equal(sorted[^1] + 3 * bw, xs[^1], precision: 10);
    }
}
