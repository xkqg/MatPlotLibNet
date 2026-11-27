// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="WaveletEntropy"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class WaveletEntropyTests
{
    // ── Branch 6 — Total energy == 0 (constant signal=0) → output 0 (guard) ──
    [Fact]
    public void Compute_AllZeroSignal_ReturnsZero()
    {
        var prices = new double[20];
        var w = new WaveletEntropy(prices, window: 16).Compute().Values;
        Assert.NotEmpty(w);
        Assert.All(w, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 7b — Alternating ±1 → one detail band dominates → entropy near 0 ──
    [Fact]
    public void Compute_AlternatingSignal_EntropyNearZero()
    {
        var alt = new double[100];
        for (int i = 0; i < 100; i++) alt[i] = (i % 2 == 0) ? 1.0 : -1.0;
        var w = new WaveletEntropy(alt, window: 64).Compute().Values;
        Assert.True(w[^1] < 0.1, $"expected entropy < 0.1 for alternating signal, got {w[^1]}");
    }

    // ── Branch 8 — Uniform energy across bands → entropy near 1 ──
    //
    // Construct a signal whose DWT energy is spread across all bands. Superpose
    // oscillations at each scale: 1/2 the bars swing at scale 1, 1/4 at scale 2, etc.
    // For a window of 16 (4 levels), a zero-mean noisy signal should distribute
    // energy roughly across all bands.
    [Fact]
    public void Compute_NoisySignal_HasModerateEntropy()
    {
        var rng = new Random(42);
        var prices = new double[200];
        for (int i = 0; i < 200; i++) prices[i] = rng.NextDouble() - 0.5;
        var w = new WaveletEntropy(prices, window: 16).Compute().Values;
        Assert.All(w, v => Assert.InRange(v, 0.0, 1.0));
        // Random noise → energy roughly spread → entropy > 0.5 typically.
        Assert.True(w[^1] > 0.5, $"expected noisy entropy > 0.5, got {w[^1]}");
    }
}
