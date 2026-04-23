// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="CgOscillator"/>. Covers all 7 branches in
/// docs/contrib/indicator-tier-3b.md §2.</summary>
public class CgOscillatorTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var cg = new CgOscillator([]);
        Assert.Empty(cg.Compute().Values);
    }

    // ── Branch 2 — prices.Length < period → empty ──
    [Fact]
    public void Compute_LengthBelowPeriod_ReturnsEmpty()
    {
        var cg = new CgOscillator([1.0, 2, 3, 4], period: 10);
        Assert.Empty(cg.Compute().Values);
    }

    // ── Branch 3 — period < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_PeriodBelowTwo_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() => new CgOscillator([1.0, 2, 3, 4, 5], period));
    }

    // ── Branch 4 — Zero denominator (prices sum to zero over window) → zero output via guard ──
    [Fact]
    public void Compute_ZeroSumWindow_ReturnsZero()
    {
        // Symmetric negatives that sum to zero — triggers the sum==0 guard arm.
        var prices = new double[] { 1, -1, 2, -2, 3, -3, 4, -4, 5, -5 };
        var cg = new CgOscillator(prices, period: 10).Compute().Values;
        Assert.Single(cg);
        Assert.False(double.IsNaN(cg[0]));
        Assert.False(double.IsInfinity(cg[0]));
        Assert.Equal(0.0, cg[0], precision: 10);
    }

    // ── Branch 5 — Constant prices → cg = 0 (offset cancels) ──
    [Fact]
    public void Compute_ConstantPrices_ReturnsZero()
    {
        int n = 30;
        var cg = new CgOscillator(Enumerable.Repeat(100.0, n).ToArray(), period: 10).Compute().Values;
        Assert.Equal(n - 10 + 1, cg.Length);
        Assert.All(cg, v => Assert.Equal(0.0, v, precision: 9));
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
        Assert.All(cg, v =>
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
        new CgOscillator(Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray()).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_HasPeriod()
    {
        var cg = new CgOscillator([1.0, 2, 3]);
        Assert.Equal("CG(10)", cg.Label);
    }

    [Fact]
    public void InheritsPriceIndicator()
    {
        var cg = new CgOscillator([1.0, 2, 3]);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(cg);
        Assert.IsAssignableFrom<IIndicator>(cg);
    }
}
