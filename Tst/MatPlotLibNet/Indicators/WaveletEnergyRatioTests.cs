// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="WaveletEnergyRatio"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class WaveletEnergyRatioTests
{
    // ── Branch 8b — All-zero signal → total energy = 0 → guard returns 0 (no NaN) ──
    [Fact]
    public void Compute_AllZeroSignal_ReturnsZero()
    {
        var prices = new double[100]; // all zeros
        var w = new WaveletEnergyRatio(prices, window: 64, level: 0).Compute().Values;
        Assert.NotEmpty(w);
        Assert.All(w, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 9 — Alternating ±1 concentrates energy at level 0; other levels near 0 ──
    [Fact]
    public void Compute_AlternatingSignal_LevelZeroDominates()
    {
        var alt = new double[100];
        for (int i = 0; i < 100; i++) alt[i] = (i % 2 == 0) ? 1.0 : -1.0;

        var w0 = new WaveletEnergyRatio(alt, window: 64, level: 0).Compute().Values;
        Assert.True(w0[^1] > 0.95, $"expected level-0 ratio > 0.95, got {w0[^1]}");

        var w1 = new WaveletEnergyRatio(alt, window: 64, level: 1).Compute().Values;
        Assert.True(w1[^1] < 0.05, $"expected level-1 ratio < 0.05, got {w1[^1]}");
    }

    // ── Branch 10 — Normal multi-window path ──
    [Fact]
    public void Compute_NoisyWindow_StaysInZeroOneRange()
    {
        var rng = new Random(42);
        var prices = new double[100];
        for (int i = 0; i < 100; i++) prices[i] = rng.NextDouble();
        var w = new WaveletEnergyRatio(prices, window: 16, level: 0).Compute().Values;
        Assert.Equal(85, w.Length); // 100 - 16 + 1
        Assert.All(w, v => Assert.InRange(v, 0.0, 1.0));
    }
}
