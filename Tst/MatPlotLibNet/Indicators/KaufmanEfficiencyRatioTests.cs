// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="KaufmanEfficiencyRatio"/> behavior. Covers all branches
/// enumerated in docs/contrib/indicator-tier-1a.md §3: empty, below-period, exact boundary,
/// flat (zero-volatility guard), monotonic, zigzag, normal multi-bar, and period precondition.</summary>
public class KaufmanEfficiencyRatioTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var er = new KaufmanEfficiencyRatio([], period: 5);
        Assert.Empty(er.Compute().Values);
    }

    // ── Branch 2 — Length <= period ──
    [Fact]
    public void Compute_LengthEqualsPeriod_ReturnsEmpty()
    {
        // 5 prices, period 5 — not enough to compute |C_t − C_{t-N}|
        var er = new KaufmanEfficiencyRatio([100.0, 101, 102, 103, 104], period: 5);
        Assert.Empty(er.Compute().Values);
    }

    [Fact]
    public void Compute_LengthBelowPeriod_ReturnsEmpty()
    {
        var er = new KaufmanEfficiencyRatio([100.0, 101, 102], period: 5);
        Assert.Empty(er.Compute().Values);
    }

    // ── Branch 3 — Length == period + 1 (boundary, single output) ──
    [Fact]
    public void Compute_LengthEqualsPeriodPlusOne_ReturnsSingleValue()
    {
        var er = new KaufmanEfficiencyRatio([100.0, 101, 102, 103, 104, 105], period: 5);
        Assert.Single(er.Compute().Values);
    }

    // ── Branch 4 — Flat prices (volatility == 0 guard, explicit division-by-zero path) ──
    [Fact]
    public void Compute_FlatPrices_ReturnsZero()
    {
        double[] prices = [100, 100, 100, 100, 100, 100];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Single(result);
        Assert.Equal(0.0, result[0], precision: 12);
        Assert.False(double.IsNaN(result[0]));
    }

    // ── Branch 5 — Monotonic rising → ER = 1.0 ──
    [Fact]
    public void Compute_MonotonicRising_ReturnsOne()
    {
        double[] prices = [100, 101, 102, 103, 104, 105];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Single(result);
        Assert.Equal(1.0, result[0], precision: 10);
    }

    [Fact]
    public void Compute_MonotonicFalling_ReturnsOne()
    {
        // |change| = net displacement; all bars contribute in same direction
        double[] prices = [105, 104, 103, 102, 101, 100];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Single(result);
        Assert.Equal(1.0, result[0], precision: 10);
    }

    // ── Branch 6 — Zigzag (|ΔC| constant) → ER is small ──
    [Fact]
    public void Compute_Zigzag_ReturnsFifthOfTotal()
    {
        // [100,101,100,101,100,101], period=5
        //   change = |101 − 100| = 1
        //   volatility = 1 + 1 + 1 + 1 + 1 = 5
        //   ER = 0.2
        double[] prices = [100, 101, 100, 101, 100, 101];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Single(result);
        Assert.Equal(0.2, result[0], precision: 10);
    }

    [Fact]
    public void Compute_PureZigzag_NetZero_ReturnsZero()
    {
        // [100,101,100,101,100,101,100], period=6: net change = 0, volatility = 6
        // → ER = 0
        double[] prices = [100, 101, 100, 101, 100, 101, 100];
        var er = new KaufmanEfficiencyRatio(prices, period: 6);
        var result = er.Compute().Values;
        Assert.Single(result);
        Assert.Equal(0.0, result[0], precision: 10);
    }

    // ── Branch 7 — Normal multi-bar output ──
    [Fact]
    public void Compute_MultiBarOutput_ReturnsCorrectLength()
    {
        // 10 prices, period 5 → 10 - 5 = 5 outputs
        double[] prices = [100, 101, 102, 103, 104, 105, 106, 107, 108, 109];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Equal(5, result.Length);
        Assert.All(result, v =>
        {
            Assert.InRange(v, 0.0, 1.0);
            Assert.False(double.IsNaN(v));
        });
    }

    [Fact]
    public void Compute_KnownVector_RollingMixedSignal()
    {
        // Build a known sequence: 5 up, 1 reversal, 4 up.
        // [100, 101, 102, 103, 104, 105, 100, 101, 102, 103, 104]
        // period = 5.
        // Output at t=5: window = idx 1..5 = [101,102,103,104,105]; change = |105-100|=5;
        //   vol sums diffs over idx 1..5 = |1|+|1|+|1|+|1|+|1| = 5 → ER = 1.0
        // Output at t=6: window idx 2..6 = [102..100]; change = |100-101|=1;
        //   vol = |1|+|1|+|1|+|1|+|5| = 9 → ER ≈ 0.111
        double[] prices = [100, 101, 102, 103, 104, 105, 100, 101, 102, 103, 104];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Equal(6, result.Length); // 11 - 5 = 6
        Assert.Equal(1.0, result[0], precision: 10);
        Assert.Equal(1.0 / 9.0, result[1], precision: 10);
    }

    // ── Branch 8 — Period < 1 throws ──
    [Fact]
    public void Constructor_PeriodBelowOne_Throws()
    {
        double[] prices = [100, 101, 102];
        Assert.Throws<ArgumentException>(() => new KaufmanEfficiencyRatio(prices, period: 0));
        Assert.Throws<ArgumentException>(() => new KaufmanEfficiencyRatio(prices, period: -5));
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new KaufmanEfficiencyRatio([100.0, 101, 102, 103, 104, 105, 106], period: 5).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new KaufmanEfficiencyRatio([100.0, 101, 102, 103, 104, 105, 106], period: 5).Apply(axes);
        Assert.Equal("ER(5)", axes.Series[0].Label);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToOne()
    {
        var axes = new Axes();
        new KaufmanEfficiencyRatio([100.0, 101, 102, 103, 104, 105, 106], period: 5).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void DefaultPeriod_IsTen()
    {
        var prices = new double[15];
        for (int i = 0; i < 15; i++) prices[i] = 100 + i;
        var er = new KaufmanEfficiencyRatio(prices);
        Assert.Equal("ER(10)", er.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(
            new KaufmanEfficiencyRatio([100.0, 101, 102, 103, 104, 105, 106]));
        Assert.IsAssignableFrom<IIndicator>(
            new KaufmanEfficiencyRatio([100.0, 101, 102, 103, 104, 105, 106]));
    }
}
