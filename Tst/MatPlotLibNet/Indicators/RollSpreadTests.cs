// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="RollSpread"/>. Covers all 9 branches enumerated in
/// docs/contrib/indicator-tier-1c.md §4.</summary>
public class RollSpreadTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var roll = new RollSpread([], period: 4);
        Assert.Empty(roll.Compute().Values);
    }

    // ── Branch 2 — Length <= period + 1 → empty ──
    [Fact]
    public void Compute_LengthEqualsPeriodPlusOne_ReturnsEmpty()
    {
        var roll = new RollSpread([100.0, 101, 102, 103, 104], period: 4);
        Assert.Empty(roll.Compute().Values);
    }

    [Fact]
    public void Compute_LengthBelowPeriodPlusOne_ReturnsEmpty()
    {
        var roll = new RollSpread([100.0, 101, 102], period: 4);
        Assert.Empty(roll.Compute().Values);
    }

    // ── Branch 3 — Length == period + 2 → single output ──
    [Fact]
    public void Compute_LengthEqualsPeriodPlusTwo_ReturnsSingleValue()
    {
        var roll = new RollSpread([100.0, 101, 102, 103, 104, 105], period: 4);
        Assert.Single(roll.Compute().Values);
    }

    // ── Branch 4 — period < 2 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_PeriodBelowTwo_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() => new RollSpread([100.0, 101, 102], period));
    }

    // ── Branch 5 — Non-positive price NOT thrown (spec: raw differences, not logs) ──
    [Fact]
    public void Constructor_NonPositivePrice_DoesNotThrow()
    {
        // Raw diffs don't require positive prices; spec says just document.
        var ex = Record.Exception(() =>
            new RollSpread([-1.0, 0, 1, 2, 3, 4], period: 2));
        Assert.Null(ex);
    }

    // ── Branch 6 — Flat prices → all Δp = 0 → cov = 0 → S = 0, no NaN ──
    [Fact]
    public void Compute_FlatPrices_ReturnsZero()
    {
        var roll = new RollSpread([100.0, 100, 100, 100, 100, 100], period: 3);
        var r = roll.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 7 — cov >= 0 (positive autocorrelation, monotonic rise) → S = 0 ──
    [Fact]
    public void Compute_MonotonicRise_ReturnsZero()
    {
        var roll = new RollSpread(
            [100.0, 101, 102, 103, 104, 105, 106, 107], period: 4);
        var r = roll.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 8 — cov < 0 (bid-ask bounce) → S = 2√(−cov) ──
    [Fact]
    public void Compute_BidAskBounce_ReturnsTwiceSpread()
    {
        // Perfect bounce 100.0 / 100.2 gives pop-cov = −0.0384 → S ≈ 0.3919.
        // Expected Roll-spread value approaches 2·s = 0.4 as window size grows; for
        // period=4 (5 Δp values), finite-sample bias yields 0.3919.
        var roll = new RollSpread(
            [100.0, 100.2, 100.0, 100.2, 100.0, 100.2], period: 4);
        var r = roll.Compute().Values;
        Assert.Single(r);
        Assert.Equal(0.39192, r[0], precision: 4);
    }

    // ── Branch 9 — Normal multi-bar output ──
    [Fact]
    public void Compute_RollingOutput_ReturnsCorrectLength()
    {
        // n = 10, period = 3 → output length = 10 - 3 - 1 = 6
        var roll = new RollSpread(
            [100.0, 100.2, 100.0, 100.2, 100.0, 100.2, 100.0, 100.2, 100.0, 100.2],
            period: 3);
        var r = roll.Compute().Values;
        Assert.Equal(6, r.Length);
        Assert.All(r, v => Assert.False(double.IsNaN(v)));
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var prices = new double[10];
        for (int i = 0; i < 10; i++) prices[i] = 100 + i;
        new RollSpread(prices, period: 3).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var prices = new double[10];
        for (int i = 0; i < 10; i++) prices[i] = 100 + i;
        new RollSpread(prices, period: 3).Apply(axes);
        Assert.Equal("Roll(3)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_Is20()
    {
        var prices = new double[25];
        for (int i = 0; i < 25; i++) prices[i] = 100 + i;
        var r = new RollSpread(prices);
        Assert.Equal("Roll(20)", r.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var prices = new double[10];
        for (int i = 0; i < 10; i++) prices[i] = 100 + i;
        var r = new RollSpread(prices, period: 3);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
