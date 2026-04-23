// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Decycler"/>. Covers all 7 branches in
/// docs/contrib/indicator-tier-3c.md §2.</summary>
public class DecyclerTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var dec = new Decycler([]);
        Assert.Empty(dec.Compute().Values);
    }

    // ── Branch 2 — Length < 3 → empty (filter needs prev values) ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Compute_LengthBelowThree_ReturnsEmpty(int n)
    {
        var prices = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var dec = new Decycler(prices, hpPeriod: 20);
        Assert.Empty(dec.Compute().Values);
    }

    // ── Branch 3 — hpPeriod < 4 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Constructor_HpPeriodBelowFour_Throws(int hp)
    {
        Assert.Throws<ArgumentException>(() => new Decycler([100.0, 101, 102, 103, 104], hp));
    }

    // ── Branch 4 — Flat prices → HP settles to 0 → decycler = prices ──
    [Fact]
    public void Compute_FlatPrices_EqualsPrices()
    {
        int n = 100;
        var flat = Enumerable.Repeat(100.0, n).ToArray();
        var dec = new Decycler(flat, hpPeriod: 20).Compute().Values;
        Assert.Equal(n, dec.Length);
        // After the HP transient settles, decycler should track the flat input.
        for (int i = 40; i < n; i++)
            Assert.Equal(100.0, dec[i], precision: 6);
    }

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
        Assert.All(dec, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var flat = Enumerable.Repeat(100.0, 40).ToArray();
        new Decycler(flat, hpPeriod: 20).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_HasHpPeriod()
    {
        var dec = new Decycler([1.0, 2, 3]);
        Assert.Equal("Decycler(60)", dec.Label);
    }

    [Fact]
    public void InheritsPriceIndicator()
    {
        var dec = new Decycler([1.0, 2, 3]);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(dec);
        Assert.IsAssignableFrom<IIndicator>(dec);
    }
}
