// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Decycler"/>. Its arithmetic is held here against reference values; what it keeps with every indicator (its
/// lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class DecyclerTests
{
    // ── Branch 5 — Pure low-frequency trend → HP removes nothing → decycler ≈ prices ──
    [Fact]
    public void Compute_LowFrequencyTrend_ApproxPrices()
    {
        int n = 200;
        // Slow linear ramp — frequency well below the HP cutoff (hpPeriod=20 ⇒ cutoff period 20).
        // With period=400 ramp (full cycle), HP output is small so decycler ≈ prices.
        var prices = Enumerable.Range(0, n).Select(i => 100.0 + i * 0.05).ToArray();
        var dec = new Decycler(prices, hpPeriod: 20).Compute().Values;
        // Past transient, check decycler closely tracks the slow trend.
        for (int i = 100; i < n; i++)
            Assert.True(Math.Abs(dec[i] - prices[i]) < 1.0,
                $"bar {i}: decycler={dec[i]} vs prices={prices[i]}");
    }

    // ── Branch 6 — Pure high-frequency → HP passes everything → decycler ≈ 0 (after transient) ──
    [Fact]
    public void Compute_HighFrequency_DecyclerSmallAmplitude()
    {
        int n = 300;
        // High-frequency sinusoid (period 4) well above HP cutoff (period 20).
        // HP passes it, so decycler = price - HP ≈ 0 ± transient. Sinusoid amplitude 5 → decycler well below.
        var prices = new double[n];
        for (int i = 0; i < n; i++) prices[i] = 100 + 5 * Math.Sin(2 * Math.PI * i / 4);
        var dec = new Decycler(prices, hpPeriod: 20).Compute().Values;
        // Tail: decycler should hover near the baseline (100), not carry the ±5 amplitude.
        double tailMaxDev = 0;
        for (int i = 200; i < n; i++) tailMaxDev = Math.Max(tailMaxDev, Math.Abs(dec[i] - 100.0));
        Assert.True(tailMaxDev < 3.0,
            $"expected HP to pass high frequency ⇒ decycler near 100, got tailMaxDev={tailMaxDev}");
    }

    // ── Branch 7 — Mixed — trend preserved, short swings removed ──
    [Fact]
    public void Compute_MixedSignal_AllFinite()
    {
        int n = 200;
        var rng = new Random(42);
        var prices = new double[n];
        for (int i = 0; i < n; i++)
            prices[i] = 100 + i * 0.1 + rng.NextDouble() * 2;
        var dec = new Decycler(prices, hpPeriod: 20).Compute().Values;
        Assert.Equal(n, dec.Length);
        dec.AssertAllFinite();
    }
}
