// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="FractionalDifferentiation"/> + its internal <c>ComputeWeights</c> helper. Its arithmetic is held here
/// against reference values; what it keeps with every indicator (its lines, its pane, its first value, the arguments it refuses)
/// is held by <see cref="IndicatorContractTests"/>.</summary>
public class FractionalDifferentiationTests
{
    // ── Branch 1-3 — constructor guards ──

    // ── Branch 7 — Normal multi-bar output ──
    [Fact]
    public void Compute_NormalPath_ReturnsExpectedLength()
    {
        // 50 bars, d=0.4 tol=1e-2 → weights ≈ small, output length = 50 - m
        var prices = new double[50];
        for (int i = 0; i < 50; i++) prices[i] = 100 + 0.5 * i;
        var ffd = new FractionalDifferentiation(prices, d: 0.4, tolerance: 1e-2);
        var weights = FractionalDifferentiation.ComputeWeights(0.4, tolerance: 1e-2);
        var result = ffd.Compute().Values;
        Assert.Equal(50 - weights.Length + 1, result.Length);
        result.AssertAllFinite();
    }

    // ── ComputeWeights — Branch 1: d-boundary guard (defensive) ──

    // ── ComputeWeights — Branch 2: very small d, verify cap kicks in ──

    [Fact]
    public void ComputeWeights_VerySmallD_TerminatesAtHardCap()
    {
        // d=0.01 + tol=1e-12 would never terminate naturally in a reasonable lifetime.
        // The hard cap must prevent an infinite loop and throw.
        Assert.Throws<InvalidOperationException>(() =>
            FractionalDifferentiation.ComputeWeights(0.01, 1e-12));
    }

    // ── ComputeWeights — Branch 3: near-integer d → short weight series ──

    [Fact]
    public void ComputeWeights_NearIntegerD_ReturnsFewWeights()
    {
        var w = FractionalDifferentiation.ComputeWeights(0.99, 1e-3);
        // For d=0.99, weights decay fast (close to integer differencing)
        Assert.InRange(w.Length, 2, 20);
    }

    // ── ComputeWeights — Branch 4: tolerance boundary ──

    [Fact]
    public void ComputeWeights_LastKeptWeight_SatisfiesTolerance()
    {
        double tol = 1e-4;
        var w = FractionalDifferentiation.ComputeWeights(0.4, tol);
        // Last kept weight must have |w| >= tol (otherwise it wouldn't have been kept)
        Assert.True(Math.Abs(w[^1]) >= tol);
    }

    // ── ComputeWeights — Branch 5: first two weights match closed form ──

    [Theory]
    [InlineData(0.5, 1e-3)]
    [InlineData(0.4, 1e-3)]
    [InlineData(0.25, 1e-2)]
    public void ComputeWeights_FirstTwoWeights_MatchClosedForm(double d, double tolerance)
    {
        var w = FractionalDifferentiation.ComputeWeights(d, tolerance);
        Assert.Equal(1.0, w[0], precision: 12);
        Assert.Equal(-d, w[1], precision: 12);
    }

    // Known vector for d=0.5, tol=1e-3:
    //   w[0] = 1, w[1] = -0.5, w[2] = -0.125, w[3] = -0.0625, w[4] = -0.0390625
    // Derived via recurrence w_k = w_{k-1} * (k - 1 - d) / k.
    [Fact]
    public void ComputeWeights_KnownVector_DHalf()
    {
        var w = FractionalDifferentiation.ComputeWeights(0.5, 1e-3);
        Assert.Equal(1.0,        w[0], precision: 10);
        Assert.Equal(-0.5,       w[1], precision: 10);
        Assert.Equal(-0.125,     w[2], precision: 10);
        Assert.Equal(-0.0625,    w[3], precision: 10);
        Assert.Equal(-0.0390625, w[4], precision: 10);
    }

    // ── ComputeWeights — Branch 6: signs alternate after w_1 ──
    [Fact]
    public void ComputeWeights_SignsAlternateAfterW1_False_AllNegativeForDInZeroOne()
    {
        // Note: for d ∈ (0, 1), the recurrence w_k = w_{k-1}*(k-1-d)/k keeps (k-1-d) positive
        // for k >= 2 (since d < 1, so k-1-d >= 1-1-0.99 = -0.99 only at k=1, then >= 0 at k=2,
        // strictly positive at k >= 2). So weights DO NOT alternate — all are negative after w_0.
        // This test pins the actual behaviour: w_0 > 0, w_k <= 0 for k >= 1.
        var w = FractionalDifferentiation.ComputeWeights(0.4, 1e-6);
        Assert.True(w[0] > 0);
        for (int k = 1; k < w.Length; k++)
            Assert.True(w[k] <= 0, $"expected w[{k}] <= 0, got {w[k]}");
    }
}
