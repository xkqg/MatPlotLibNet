// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="RoofingFilter"/>. Covers all 10 branches enumerated in
/// docs/contrib/indicator-tier-2c.md §2.</summary>
public class RoofingFilterTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(new RoofingFilter([]).Compute().Values);
    }

    // ── Branch 2 — Length < 3 → empty ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Compute_LengthBelowThree_ReturnsEmpty(int n)
    {
        var prices = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        Assert.Empty(new RoofingFilter(prices).Compute().Values);
    }

    // ── Branch 3 — Length == 3 → 3 outputs (filters produce full-length output) ──
    [Fact]
    public void Compute_LengthThree_ReturnsThreeValues()
    {
        var r = new RoofingFilter([100.0, 101, 102]).Compute().Values;
        Assert.Equal(3, r.Length);
        Assert.All(r, v => Assert.False(double.IsNaN(v)));
    }

    // ── Branch 4 — hpPeriod < 4 throws ──
    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    [InlineData(0)]
    public void Constructor_HpPeriodBelowFour_Throws(int hp)
    {
        Assert.Throws<ArgumentException>(() =>
            new RoofingFilter([1.0, 2, 3, 4], hpPeriod: hp));
    }

    // ── Branch 5 — lpPeriod < 2 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void Constructor_LpPeriodBelowTwo_Throws(int lp)
    {
        Assert.Throws<ArgumentException>(() =>
            new RoofingFilter([1.0, 2, 3, 4], hpPeriod: 48, lpPeriod: lp));
    }

    // ── Branch 6 — lpPeriod >= hpPeriod throws ──
    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 20)]
    public void Constructor_LpNotStrictlyBelowHp_Throws(int hp, int lp)
    {
        Assert.Throws<ArgumentException>(() =>
            new RoofingFilter([1.0, 2, 3, 4], hpPeriod: hp, lpPeriod: lp));
    }

    // ── Branch 7 — Constant input → settled tail near zero ──
    [Fact]
    public void Compute_ConstantInput_SettledTailNearZero()
    {
        var prices = Enumerable.Repeat(100.0, 200).ToArray();
        var r = new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Compute().Values;
        // Transient dies out by bar 2·hpPeriod; settled portion should be ≈ 0.
        for (int i = 100; i < r.Length; i++)
            Assert.True(Math.Abs(r[i]) < 1e-4, $"bar {i} = {r[i]}");
    }

    // ── Branch 8 — Pure HF noise — HP passes it, LP attenuates it → small output ──
    [Fact]
    public void Compute_HighFrequencyNoise_StrongAttenuation()
    {
        var prices = new double[200];
        for (int i = 0; i < 200; i++) prices[i] = 100 + (i % 2 == 0 ? 1.0 : -1.0);
        var r = new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Compute().Values;
        double tailMax = 0;
        for (int i = 100; i < r.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        // Alternating ±1 is well above the 10-period LP cutoff → strongly attenuated.
        Assert.True(tailMax < 0.5, $"tailMax={tailMax}; HF should attenuate");
    }

    // ── Branch 9 — Pure LF trend — HP removes it → output near 0 ──
    [Fact]
    public void Compute_LowFrequencyTrend_StrongAttenuation()
    {
        var prices = new double[300];
        for (int i = 0; i < 300; i++) prices[i] = 100 + 0.05 * i;
        var r = new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Compute().Values;
        double tailMax = 0;
        for (int i = 200; i < r.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax < 0.1, $"tailMax={tailMax}; slow trend should attenuate");
    }

    // ── Branch 10 — Target-frequency sinusoid — output has non-trivial amplitude ──
    [Fact]
    public void Compute_TargetFrequencySinusoid_AmplitudePreserved()
    {
        // Period between lpPeriod (10) and hpPeriod (48) — e.g., 20 — sits in the passband.
        var prices = new double[300];
        for (int i = 0; i < 300; i++) prices[i] = 100 + 5 * Math.Sin(2 * Math.PI * i / 20);
        var r = new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Compute().Values;
        double tailMax = 0;
        for (int i = 200; i < r.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax > 0.5, $"tailMax={tailMax}; target-band signal should pass");
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        new RoofingFilter(prices).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Apply(axes);
        Assert.Equal("Roof(48/10)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultHpAndLpPeriods()
    {
        var prices = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        var r = new RoofingFilter(prices);
        Assert.Equal("Roof(48/10)", r.Label);
    }

    [Fact]
    public void InheritsPriceIndicator()
    {
        var r = new RoofingFilter([1.0, 2, 3, 4]);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
