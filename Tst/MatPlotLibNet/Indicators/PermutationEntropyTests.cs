// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="PermutationEntropy"/>. Covers all 10 branches in
/// docs/contrib/indicator-tier-2b.md §1.</summary>
public class PermutationEntropyTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var pe = new PermutationEntropy([], order: 3, window: 10);
        Assert.Empty(pe.Compute().Values);
    }

    // ── Branch 2 — prices.Length < window → empty ──
    [Fact]
    public void Compute_LengthBelowWindow_ReturnsEmpty()
    {
        var pe = new PermutationEntropy([1.0, 2, 3, 4, 5], order: 3, window: 10);
        Assert.Empty(pe.Compute().Values);
    }

    // ── Branch 3 — prices.Length == window → single output ──
    [Fact]
    public void Compute_LengthEqualsWindow_ReturnsSingleValue()
    {
        var pe = new PermutationEntropy([1.0, 2, 3, 4, 5, 4, 3, 2, 1, 2], order: 3, window: 10);
        Assert.Single(pe.Compute().Values);
    }

    // ── Branch 4 — order < 2 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_OrderBelowTwo_Throws(int order)
    {
        Assert.Throws<ArgumentException>(() =>
            new PermutationEntropy([1.0, 2, 3, 4, 5], order, window: 4));
    }

    // ── Branch 5 — order > 7 throws ──
    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public void Constructor_OrderAboveSeven_Throws(int order)
    {
        Assert.Throws<ArgumentException>(() =>
            new PermutationEntropy([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10], order, window: 20));
    }

    // ── Branch 6 — window <= order throws ──
    [Theory]
    [InlineData(3, 3)]
    [InlineData(3, 2)]
    public void Constructor_WindowNotGreaterThanOrder_Throws(int order, int window)
    {
        Assert.Throws<ArgumentException>(() =>
            new PermutationEntropy([1.0, 2, 3, 4, 5], order, window));
    }

    // ── Branch 7 — Constant signal → entropy 0 ──
    [Fact]
    public void Compute_ConstantSignal_ReturnsZero()
    {
        var flat = Enumerable.Repeat(100.0, 50).ToArray();
        var pe = new PermutationEntropy(flat, order: 3, window: 20).Compute().Values;
        Assert.NotEmpty(pe);
        Assert.All(pe, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 8 — Strictly monotonic rising → entropy 0 ──
    [Fact]
    public void Compute_MonotonicRising_ReturnsZero()
    {
        var rising = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var pe = new PermutationEntropy(rising, order: 3, window: 20).Compute().Values;
        Assert.NotEmpty(pe);
        Assert.All(pe, v => Assert.Equal(0.0, v, precision: 12));
    }

    [Fact]
    public void Compute_MonotonicFalling_ReturnsZero()
    {
        var falling = Enumerable.Range(0, 50).Select(i => 100.0 - i).ToArray();
        var pe = new PermutationEntropy(falling, order: 3, window: 20).Compute().Values;
        Assert.All(pe, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 9 — Perfect noise → entropy approaches 1 ──
    [Fact]
    public void Compute_NoisySeries_EntropyNearOne()
    {
        var rng = new Random(42);
        var noise = new double[500];
        for (int i = 0; i < 500; i++) noise[i] = rng.NextDouble();

        var pe = new PermutationEntropy(noise, order: 3, window: 100).Compute().Values;
        // For d=3, 3!=6 permutations; uniform → entropy 1 after normalization.
        // For random uniform noise with window=100, expect entropy ~0.99.
        double last = pe[^1];
        Assert.True(last > 0.95 && last <= 1.0, $"expected PE near 1.0, got {last}");
    }

    // ── Branch 10 — Normal multi-window path ──
    //
    // Known vector: the 5-bar window [1, 2, 3, 2, 1] with order=3 has 3 subwindows:
    //   [1,2,3] → ranks (0,1,2)
    //   [2,3,2] → stable argsort of [2,3,2]: indices sorted by value are (0,2,1) (1 stable-first).
    //             So rank of positions: pos 0 → rank 0, pos 1 → rank 2, pos 2 → rank 1 → perm (0,2,1).
    //   [3,2,1] → ranks (2,1,0).
    // Three distinct perms with counts (1,1,1) → H = log(3). Normalized by log(3!) = log(6):
    //   entropy = log(3) / log(6) ≈ 0.6131.
    [Fact]
    public void Compute_KnownVector_MatchesReference()
    {
        var prices = new[] { 1.0, 2, 3, 2, 1 };
        var pe = new PermutationEntropy(prices, order: 3, window: 5).Compute().Values;
        Assert.Single(pe);
        Assert.Equal(Math.Log(3) / Math.Log(6), pe[0], precision: 10);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        new PermutationEntropy(prices, order: 3, window: 20).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToOne()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        new PermutationEntropy(prices, order: 3, window: 20).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        new PermutationEntropy(prices, order: 4, window: 25).Apply(axes);
        Assert.Equal("PE(d=4,W=25)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultOrderAndWindow()
    {
        var prices = new double[120];
        for (int i = 0; i < 120; i++) prices[i] = i;
        var pe = new PermutationEntropy(prices);
        Assert.Equal("PE(d=4,W=100)", pe.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var pe = new PermutationEntropy([1.0, 2, 3, 4, 5], order: 3, window: 5);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(pe);
        Assert.IsAssignableFrom<IIndicator>(pe);
    }
}
