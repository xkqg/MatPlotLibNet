// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="AmihudIlliquidity"/>. Covers all 9 branches enumerated in
/// docs/contrib/indicator-tier-1c.md §1.</summary>
public class AmihudIlliquidityTests
{
    // Canonical vector (spec §1):
    //   close:  [100, 101, 103, 102, 105]
    //   volume: [1000, 1000, 500, 1500, 1000]
    //   period: 3
    // Python reference:
    //   import numpy as np
    //   c = np.array([100,101,103,102,105]); v = np.array([1000,1000,500,1500,1000])
    //   r = np.log(c[1:]/c[:-1]); illiq = np.abs(r)/(c[1:]*v[1:])
    //   out = [illiq[i:i+3].mean() for i in range(len(illiq)-2)]
    //   → out = [1.81010331e-07, 2.40194780e-07]
    private static readonly double[] RefClose = [100, 101, 103, 102, 105];
    private static readonly double[] RefVolume = [1000, 1000, 500, 1500, 1000];

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var illiq = new AmihudIlliquidity([], [], period: 5);
        Assert.Empty(illiq.Compute().Values);
    }

    // ── Branch 2 — close/volume length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new AmihudIlliquidity([100.0, 101], [1000.0], period: 1));
    }

    // ── Branch 3 — Length <= period → empty ──
    [Fact]
    public void Compute_LengthEqualsPeriod_ReturnsEmpty()
    {
        var illiq = new AmihudIlliquidity(
            [100.0, 101, 102], [1000.0, 1000, 1000], period: 3);
        Assert.Empty(illiq.Compute().Values);
    }

    [Fact]
    public void Compute_LengthBelowPeriod_ReturnsEmpty()
    {
        var illiq = new AmihudIlliquidity(
            [100.0, 101], [1000.0, 1000], period: 5);
        Assert.Empty(illiq.Compute().Values);
    }

    // ── Branch 4 — Length == period + 1 → single output ──
    [Fact]
    public void Compute_LengthEqualsPeriodPlusOne_ReturnsSingleValue()
    {
        var illiq = new AmihudIlliquidity(
            [100.0, 101, 102, 103], [1000.0, 1000, 1000, 1000], period: 3);
        Assert.Single(illiq.Compute().Values);
    }

    // ── Branch 5 — period <= 0 throws ──
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositivePeriod_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() =>
            new AmihudIlliquidity([100.0, 101, 102], [1.0, 1, 1], period));
    }

    // ── Branch 6 — Zero volume on a bar → PositiveInfinity in that bar's contribution ──
    [Fact]
    public void Compute_ZeroVolume_ReturnsPositiveInfinity()
    {
        // Period=1, close=[100,101], volume=[1000,0]
        // Bar 1: |r|=0.00995, dollar_vol = 101*0 = 0 → infinity policy.
        var illiq = new AmihudIlliquidity([100.0, 101], [1000.0, 0], period: 1);
        Assert.Equal(double.PositiveInfinity, illiq.Compute().Values[0]);
    }

    // ── Branch 7 — Non-positive close throws ──
    [Theory]
    [InlineData(new double[] { 100.0, 0.0, 101.0 })]
    [InlineData(new double[] { -1.0, 101.0, 102.0 })]
    [InlineData(new double[] { 100.0, 101.0, -5.0 })]
    public void Constructor_NonPositiveClose_Throws(double[] close)
    {
        var volume = new double[close.Length];
        for (int i = 0; i < volume.Length; i++) volume[i] = 1000;
        Assert.Throws<ArgumentException>(() =>
            new AmihudIlliquidity(close, volume, period: 1));
    }

    // ── Branch 8 — Flat prices → all zeros, no NaN ──
    [Fact]
    public void Compute_FlatPrices_ReturnsZero()
    {
        var illiq = new AmihudIlliquidity(
            [100.0, 100, 100, 100, 100], [1000.0, 1000, 1000, 1000, 1000], period: 3);
        var r = illiq.Compute().Values;
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 9 — Normal multi-bar path (reference vector) ──
    [Fact]
    public void Compute_KnownVector_MatchesReference()
    {
        var illiq = new AmihudIlliquidity(RefClose, RefVolume, period: 3);
        var r = illiq.Compute().Values;
        Assert.Equal(2, r.Length);
        Assert.Equal(1.81010331e-7, r[0], precision: 12);
        Assert.Equal(2.40194780e-7, r[1], precision: 12);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new AmihudIlliquidity(RefClose, RefVolume, period: 3).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new AmihudIlliquidity(RefClose, RefVolume, period: 3).Apply(axes);
        Assert.Equal("ILLIQ(3)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_Is20()
    {
        var close = new double[25]; var vol = new double[25];
        for (int i = 0; i < 25; i++) { close[i] = 100 + i; vol[i] = 1000; }
        var illiq = new AmihudIlliquidity(close, vol);
        Assert.Equal("ILLIQ(20)", illiq.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsCandleIndicator()
    {
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(
            new AmihudIlliquidity(RefClose, RefVolume, period: 3));
        Assert.IsAssignableFrom<IIndicator>(
            new AmihudIlliquidity(RefClose, RefVolume, period: 3));
    }
}
