// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="CgOscillator"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class CgOscillatorTests
{
    // ── Branch 4 — Zero denominator (prices sum to zero over window) → zero output via guard ──
    [Fact]
    public void Compute_ZeroSumWindow_ReturnsZero()
    {
        // Symmetric negatives that sum to zero — triggers the sum==0 guard arm.
        var prices = new double[] { 1, -1, 2, -2, 3, -3, 4, -4, 5, -5 };
        var cg = new CgOscillator(prices, period: 10).Compute().Values;
        Assert.Single(cg);
        Assert.Equal(0.0, cg[0], precision: 10);
    }

    // ── Branch 6 — Monotonic rise → recent prices higher → cg positive ──
    [Fact]
    public void Compute_MonotonicRise_ReturnsPositive()
    {
        int n = 30;
        var cg = new CgOscillator(Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray(),
            period: 10).Compute().Values;
        Assert.True(cg[^1] > 0, $"expected positive CG on monotonic rise, got {cg[^1]}");
    }

    // ── Branch 7 — Normal multi-bar path ──
    [Fact]
    public void Compute_MixedData_AllFinite()
    {
        int n = 50;
        var rng = new Random(11);
        var prices = Enumerable.Range(0, n).Select(i => 100.0 + rng.NextDouble() * 10).ToArray();
        var cg = new CgOscillator(prices, period: 10).Compute().Values;
        Assert.Equal(n - 10 + 1, cg.Length);
        cg.AssertAllFinite();
    }
}
