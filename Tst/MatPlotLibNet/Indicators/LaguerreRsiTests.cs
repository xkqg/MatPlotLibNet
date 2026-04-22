// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="LaguerreRsi"/>. Covers all 10 branches in
/// docs/contrib/indicator-tier-1d.md §3.</summary>
public class LaguerreRsiTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new LaguerreRsi([], alpha: 0.2);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 2 — Length == 1 → empty (need prev for recurrence) ──
    [Fact]
    public void Compute_SingleBar_ReturnsEmpty()
    {
        var r = new LaguerreRsi([100.0], alpha: 0.2);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 3 — Length == 2 boundary → single output ──
    [Fact]
    public void Compute_TwoBars_ReturnsSingleValue()
    {
        var r = new LaguerreRsi([100.0, 101], alpha: 0.2);
        Assert.Single(r.Compute().Values);
    }

    // ── Branch 4 — alpha <= 0 throws ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    public void Constructor_NonPositiveAlpha_Throws(double alpha)
    {
        Assert.Throws<ArgumentException>(() => new LaguerreRsi([100.0, 101], alpha));
    }

    // ── Branch 5 — alpha >= 1 throws ──
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Constructor_AlphaAtOrAboveOne_Throws(double alpha)
    {
        Assert.Throws<ArgumentException>(() => new LaguerreRsi([100.0, 101], alpha));
    }

    // ── Branch 6 — alpha = 0.5 edge (no special handling, sanity check) ──
    [Fact]
    public void Compute_AlphaHalf_ProducesValidOutput()
    {
        var r = new LaguerreRsi(
            Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray(),
            alpha: 0.5);
        var vals = r.Compute().Values;
        Assert.All(vals, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 0.0, 1.0);
        });
    }

    // ── Branch 7 — Flat prices → CU = CD = 0 → output 0 (guard branch) ──
    [Fact]
    public void Compute_FlatPrices_ReturnsZero()
    {
        var r = new LaguerreRsi(
            Enumerable.Repeat(100.0, 10).ToArray(), alpha: 0.2);
        var vals = r.Compute().Values;
        Assert.All(vals, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 8 — Monotonic rising → output approaches 1.0 ──
    [Fact]
    public void Compute_MonotonicRising_ApproachesOne()
    {
        var r = new LaguerreRsi(
            Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray(),
            alpha: 0.2);
        var vals = r.Compute().Values;
        // After ≥20 bars of sustained rise, Laguerre RSI should be very close to 1.
        Assert.InRange(vals[25], 0.95, 1.0001);
    }

    // ── Branch 9 — Monotonic falling → output approaches 0.0 ──
    [Fact]
    public void Compute_MonotonicFalling_ApproachesZero()
    {
        var r = new LaguerreRsi(
            Enumerable.Range(0, 30).Select(i => 130.0 - i).ToArray(),
            alpha: 0.2);
        var vals = r.Compute().Values;
        Assert.InRange(vals[25], 0.0, 0.05);
    }

    // ── Branch 10 — Zigzag → output stays in [0, 1] ──
    [Fact]
    public void Compute_Zigzag_ProducesBoundedOutput()
    {
        double[] prices = new double[30];
        for (int i = 0; i < 30; i++) prices[i] = 100 + (i % 2);
        var r = new LaguerreRsi(prices, alpha: 0.2);
        var vals = r.Compute().Values;
        Assert.All(vals, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 0.0, 1.0);
        });
    }

    // Known vector — Python reference (see spec §3):
    //   prices = [100, 101, 100, 101, 100], alpha=0.2
    // Step-by-step (all L_k init to 100):
    //   i=1: L0=100.2, L1=99.84, L2=100.128, L3=99.8976
    //        CU = 0.36 + 0 + 0.2304 = 0.5904
    //        CD = 0 + 0.288 + 0     = 0.288
    //        RSI = 0.5904 / 0.8784 ≈ 0.67213
    [Fact]
    public void Compute_KnownVector_MatchesReference()
    {
        var r = new LaguerreRsi([100.0, 101, 100, 101, 100], alpha: 0.2);
        var vals = r.Compute().Values;
        Assert.Equal(4, vals.Length);
        Assert.Equal(0.67213, vals[0], precision: 4);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new LaguerreRsi([100.0, 101, 102, 103, 104], alpha: 0.2).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToOne()
    {
        var axes = new Axes();
        new LaguerreRsi([100.0, 101, 102, 103, 104], alpha: 0.2).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new LaguerreRsi([100.0, 101, 102, 103, 104], alpha: 0.2).Apply(axes);
        Assert.Equal("LagRSI(0.20)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultAlpha_IsZeroPointTwo()
    {
        var r = new LaguerreRsi([100.0, 101]);
        Assert.Equal("LagRSI(0.20)", r.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var r = new LaguerreRsi([100.0, 101]);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
