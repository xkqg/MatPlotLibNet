// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Cusum"/> behavior. Covers all 11 branches enumerated in
/// docs/contrib/indicator-tier-1b.md §1.</summary>
public class CusumTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var c = new Cusum([], threshold: 0.01);
        var r = c.Compute();
        Assert.Empty(r.Signal);
        Assert.Empty(r.SPos);
        Assert.Empty(r.SNeg);
    }

    // ── Branch 2 — Length == 1 → empty (need prev-close for log-return) ──
    [Fact]
    public void Compute_SingleBar_ReturnsEmpty()
    {
        var c = new Cusum([100.0], threshold: 0.01);
        var r = c.Compute();
        Assert.Empty(r.Signal);
        Assert.Empty(r.SPos);
        Assert.Empty(r.SNeg);
    }

    // ── Branch 3 — Length == 2 boundary (one output) ──
    [Fact]
    public void Compute_TwoBars_ReturnsSingleRow()
    {
        var c = new Cusum([100.0, 101.0], threshold: 0.05);
        var r = c.Compute();
        Assert.Single(r.Signal);
        Assert.Single(r.SPos);
        Assert.Single(r.SNeg);
    }

    // ── Branch 4 — Non-positive price throws ──
    [Theory]
    [InlineData(new double[] { 100.0, 0.0, 101.0 })]
    [InlineData(new double[] { 100.0, -1.0, 101.0 })]
    [InlineData(new double[] { 0.0, 101.0 })]
    public void Constructor_NonPositivePrice_Throws(double[] prices)
    {
        Assert.Throws<ArgumentException>(() => new Cusum(prices, threshold: 0.01));
    }

    // ── Branch 5 — Non-positive threshold throws ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.01)]
    public void Constructor_NonPositiveThreshold_Throws(double threshold)
    {
        Assert.Throws<ArgumentException>(() => new Cusum([100.0, 101.0], threshold));
    }

    // ── Branch 6 — Flat prices → all zeros, no NaN ──
    [Fact]
    public void Compute_FlatPrices_AllZero()
    {
        var c = new Cusum([100.0, 100, 100, 100, 100], threshold: 0.01);
        var r = c.Compute();
        Assert.All(r.Signal, v => Assert.Equal(0, v));
        Assert.All(r.SPos, v => { Assert.Equal(0.0, v, precision: 12); Assert.False(double.IsNaN(v)); });
        Assert.All(r.SNeg, v => { Assert.Equal(0.0, v, precision: 12); Assert.False(double.IsNaN(v)); });
    }

    // ── Branch 7 — Monotonic rise above threshold triggers +1 then resets ──
    [Fact]
    public void Compute_MonotonicRise_TriggersPositiveSignalThenResets()
    {
        // Log-returns ln(101/100)=0.00995, ln(102/101)=0.00985, ln(103/102)=0.00976
        // Cumulative S_pos: 0.00995 → 0.01980 → would breach 0.015 at bar 1 → reset
        // Next bar: S_pos grows from 0 + 0.00976 = 0.00976 (below threshold).
        var c = new Cusum([100.0, 101, 102, 103], threshold: 0.015);
        var r = c.Compute();
        // r[0] is from bar 1 — return only one return accumulated, below threshold → 0
        Assert.Equal(0, r.Signal[0]);
        // r[1] is from bar 2 — cumulative crossed threshold → +1
        Assert.Equal(1, r.Signal[1]);
        // r[2] after reset, only one return below threshold → 0
        Assert.Equal(0, r.Signal[2]);
    }

    // ── Branch 8 — Monotonic fall triggers -1 then resets ──
    [Fact]
    public void Compute_MonotonicFall_TriggersNegativeSignalThenResets()
    {
        var c = new Cusum([100.0, 99, 98, 97], threshold: 0.015);
        var r = c.Compute();
        Assert.Equal(0, r.Signal[0]);
        Assert.Equal(-1, r.Signal[1]);
        Assert.Equal(0, r.Signal[2]);
    }

    // ── Branch 9 — Drift compensation: positive drift absorbs steady rise → no breach ──
    [Fact]
    public void Compute_DriftAbsorbsSteadyTrend_NoBreach()
    {
        // Each bar rises exactly 1% — drift=log(1.01) cancels → y_t - θ = 0 → S_pos stays at 0.
        double drift = Math.Log(1.01);
        var c = new Cusum([100.0, 101, 102.01, 103.0301], threshold: 0.01, drift);
        var r = c.Compute();
        Assert.All(r.Signal, v => Assert.Equal(0, v));
    }

    // ── Branch 10 — Both-sides pathological (large gap): +1 wins ──
    [Fact]
    public void Compute_LargeGap_PositiveSideWins()
    {
        // Big upward gap — S_pos crosses threshold; S_neg would not trigger either way
        // because return is positive. Verify +1 emitted, not -1.
        var c = new Cusum([100.0, 110], threshold: 0.05);
        var r = c.Compute();
        Assert.Equal(1, r.Signal[0]);
    }

    // ── Branch 11 — Normal multi-bar output ──
    [Fact]
    public void Compute_ReturnsLengthMinusOne()
    {
        // 6 prices → 5 outputs
        var c = new Cusum([100.0, 101, 102, 101, 99, 98], threshold: 0.05);
        var r = c.Compute();
        Assert.Equal(5, r.Signal.Length);
        Assert.Equal(5, r.SPos.Length);
        Assert.Equal(5, r.SNeg.Length);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsAtLeastOneSeries()
    {
        var axes = new Axes();
        new Cusum([100.0, 101, 102, 103, 100, 97, 96], threshold: 0.02).Apply(axes);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var c = new Cusum([100.0, 101], threshold: 0.02);
        Assert.Equal("CUSUM(h=0.02)", c.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        Assert.IsAssignableFrom<PriceIndicator<CusumResult>>(new Cusum([100.0, 101], 0.01));
        Assert.IsAssignableFrom<IIndicator>(new Cusum([100.0, 101], 0.01));
    }
}
