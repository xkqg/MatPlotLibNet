// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="PermutationEntropy"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class PermutationEntropyTests
{
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
}
