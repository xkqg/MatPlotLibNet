// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Bocpd"/>. Covers all 11 branches enumerated in
/// docs/contrib/indicator-tier-2a.md §1.</summary>
public class BocpdTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new Bocpd([]);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 2 — Length == 1 → empty (need prev observation) ──
    [Fact]
    public void Compute_SingleBar_ReturnsEmpty()
    {
        var r = new Bocpd([100.0]);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 3 — Length == 2 boundary → single output ──
    [Fact]
    public void Compute_TwoBars_ReturnsSingleValue()
    {
        var r = new Bocpd([100.0, 101.0]);
        var v = r.Compute().Values;
        Assert.Single(v);
        Assert.False(double.IsNaN(v[0]));
        Assert.InRange(v[0], 0.0, 1.0);
    }

    // ── Branch 4 — hazard <= 0 throws ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.01)]
    public void Constructor_NonPositiveHazard_Throws(double hazard)
    {
        Assert.Throws<ArgumentException>(() => new Bocpd([100.0, 101], hazard));
    }

    // ── Branch 5 — hazard >= 1 throws ──
    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void Constructor_HazardAtOrAboveOne_Throws(double hazard)
    {
        Assert.Throws<ArgumentException>(() => new Bocpd([100.0, 101], hazard));
    }

    // ── Branch 6 — priorVariance <= 0 throws ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_NonPositivePriorVariance_Throws(double priorVar)
    {
        Assert.Throws<ArgumentException>(() => new Bocpd([100.0, 101], priorVariance: priorVar));
    }

    // ── Branch 7 — maxRunLength < 1 throws ──
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_MaxRunLengthBelowOne_Throws(int maxRun)
    {
        Assert.Throws<ArgumentException>(() => new Bocpd([100.0, 101], maxRunLength: maxRun));
    }

    // ── Branch 8 — Flat prices → low changepoint signal ──
    [Fact]
    public void Compute_FlatPrices_ChangepointProbStaysLow()
    {
        var prices = Enumerable.Repeat(100.0, 50).ToArray();
        var r = new Bocpd(prices, hazard: 0.01).Compute().Values;
        Assert.Equal(49, r.Length);
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 0.0, 0.05);
        });
    }

    // ── Branch 9 — Strong breakpoint → spike above 0.5 ──
    [Fact]
    public void Compute_StrongBreak_ProducesSpike()
    {
        var prices = new double[40];
        for (int i = 0; i < 20; i++) prices[i] = 100.0;
        for (int i = 20; i < 40; i++) prices[i] = 200.0;

        var r = new Bocpd(prices, hazard: 0.01).Compute().Values;
        Assert.Equal(39, r.Length);
        // The break is at bar 20 → output index 19 (P(r_t=0) at t=20).
        Assert.True(r[19] > 0.5, $"expected changepoint spike > 0.5 at index 19, got {r[19]}");
    }

    // ── Branch 10 — Normalization stays finite (no NaN, all in [0,1]) ──
    [Fact]
    public void Compute_NoisySeries_AllOutputsFiniteAndBounded()
    {
        var rng = new Random(123);
        var prices = new double[200];
        prices[0] = 100;
        for (int i = 1; i < 200; i++)
            prices[i] = prices[i - 1] + (rng.NextDouble() - 0.5) * 2;

        var r = new Bocpd(prices, hazard: 0.01, priorVariance: 1.0).Compute().Values;
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
            Assert.InRange(v, 0.0, 1.0);
        });
    }

    // ── Branch 11 — Run-length truncation at maxRunLength ──
    [Fact]
    public void Compute_LongSeries_WithSmallMaxRunLength_DoesNotThrow()
    {
        var prices = Enumerable.Repeat(100.0, 100).ToArray();
        // maxRunLength = 5 → distribution is truncated; no NaN / no overflow.
        var r = new Bocpd(prices, hazard: 0.01, maxRunLength: 5).Compute().Values;
        Assert.Equal(99, r.Length);
        Assert.All(r, v => Assert.False(double.IsNaN(v)));
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new Bocpd([100.0, 101, 102, 103, 104]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToOne()
    {
        var axes = new Axes();
        new Bocpd([100.0, 101, 102, 103, 104]).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new Bocpd([100.0, 101, 102, 103, 104], hazard: 0.02).Apply(axes);
        Assert.Equal("BOCPD(h=0.02)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultHazard_IsOneInHundred()
    {
        var r = new Bocpd([100.0, 101]);
        Assert.Equal("BOCPD(h=0.01)", r.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var r = new Bocpd([100.0, 101]);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
