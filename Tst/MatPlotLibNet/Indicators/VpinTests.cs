// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Vpin"/> (BVC approximation). Covers all 9 branches enumerated
/// in docs/contrib/indicator-tier-1c.md §3.</summary>
public class VpinTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var vpin = new Vpin([], [], bucketPeriod: 3, sigmaPeriod: 3);
        Assert.Empty(vpin.Compute().Values);
    }

    // ── Branch 2 — close/volume length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vpin([100.0, 101, 102], [1000.0, 1000], bucketPeriod: 3, sigmaPeriod: 3));
    }

    // ── Branch 3 — bucketPeriod or sigmaPeriod <= 0 throws ──
    [Theory]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    [InlineData(3, 0)]
    [InlineData(3, -1)]
    public void Constructor_NonPositivePeriod_Throws(int bucketPeriod, int sigmaPeriod)
    {
        Assert.Throws<ArgumentException>(() =>
            new Vpin([100.0, 101, 102, 103], [1000.0, 1000, 1000, 1000], bucketPeriod, sigmaPeriod));
    }

    // ── Branch 4 — Length <= max(bucketPeriod, sigmaPeriod) → empty ──
    [Fact]
    public void Compute_LengthBelowMaxPeriod_ReturnsEmpty()
    {
        // max(3,5)=5. 4 bars → empty.
        var vpin = new Vpin(
            [100.0, 101, 102, 103], [1000.0, 1000, 1000, 1000],
            bucketPeriod: 3, sigmaPeriod: 5);
        Assert.Empty(vpin.Compute().Values);
    }

    // ── Branch 5 — Non-positive close throws ──
    [Theory]
    [InlineData(new double[] { 100.0, 0.0, 101.0, 102.0 })]
    [InlineData(new double[] { -1.0, 101.0, 102.0, 103.0 })]
    public void Constructor_NonPositiveClose_Throws(double[] close)
    {
        var vol = new double[close.Length];
        for (int i = 0; i < vol.Length; i++) vol[i] = 1000;
        Assert.Throws<ArgumentException>(() =>
            new Vpin(close, vol, bucketPeriod: 2, sigmaPeriod: 2));
    }

    // ── Branch 6 — Zero total bucket volume → 0 (division-by-zero guard) ──
    [Fact]
    public void Compute_ZeroBucketVolume_ReturnsZero()
    {
        // All volumes 0 → total bucket volume = 0 → explicit guard returns 0.
        // Close must vary so σ_r is defined (non-zero), so we exercise the
        // bucket-volume guard distinct from the σ=0 branch.
        var vpin = new Vpin(
            [100.0, 101, 102, 103, 104, 105, 106],
            [0.0, 0, 0, 0, 0, 0, 0],
            bucketPeriod: 3, sigmaPeriod: 3);
        var r = vpin.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 7 — σ_r == 0 → 50/50 split convention → VPIN = 0 for that bucket ──
    [Fact]
    public void Compute_IdenticalReturnsInSigmaWindow_ReturnsZero()
    {
        // Powers of 2 produce EXACTLY-equal log-returns in IEEE-754 (ratio = 2 is exact).
        // Therefore σ_r = 0 in every non-trivial sigma window → branch 7 is exercised.
        var vpin = new Vpin(
            [100.0, 200, 400, 800, 1600, 3200, 6400],
            [1000.0, 1000, 1000, 1000, 1000, 1000, 1000],
            bucketPeriod: 3, sigmaPeriod: 3);
        var r = vpin.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 8 — Flat prices → all returns 0 → σ_r = 0 → VPIN = 0 throughout ──
    [Fact]
    public void Compute_FlatPrices_ReturnsZero()
    {
        var vpin = new Vpin(
            [100.0, 100, 100, 100, 100, 100, 100, 100],
            [1000.0, 1000, 1000, 1000, 1000, 1000, 1000, 1000],
            bucketPeriod: 3, sigmaPeriod: 3);
        var r = vpin.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 9 — Normal multi-bar path ──
    [Fact]
    public void Compute_VariablePrices_ProducesFiniteOutputInZeroOne()
    {
        // Mixed-direction moves produce non-degenerate BVC classification.
        var close = new double[20];
        var vol = new double[20];
        var rng = new Random(42);
        close[0] = 100;
        for (int i = 1; i < 20; i++)
            close[i] = close[i - 1] * (1 + (rng.NextDouble() - 0.5) * 0.02);
        for (int i = 0; i < 20; i++) vol[i] = 1000 + rng.NextDouble() * 500;

        var vpin = new Vpin(close, vol, bucketPeriod: 5, sigmaPeriod: 5);
        var r = vpin.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 0.0, 1.0);
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var close = new double[15]; var vol = new double[15];
        for (int i = 0; i < 15; i++) { close[i] = 100 + i; vol[i] = 1000; }
        new Vpin(close, vol, bucketPeriod: 5, sigmaPeriod: 5).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToOne()
    {
        var axes = new Axes();
        var close = new double[15]; var vol = new double[15];
        for (int i = 0; i < 15; i++) { close[i] = 100 + i; vol[i] = 1000; }
        new Vpin(close, vol, bucketPeriod: 5, sigmaPeriod: 5).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var close = new double[15]; var vol = new double[15];
        for (int i = 0; i < 15; i++) { close[i] = 100 + i; vol[i] = 1000; }
        new Vpin(close, vol, bucketPeriod: 5, sigmaPeriod: 5).Apply(axes);
        Assert.Equal("VPIN(5)", axes.Series[0].Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsCandleIndicator()
    {
        var close = new double[10]; var vol = new double[10];
        for (int i = 0; i < 10; i++) { close[i] = 100 + i; vol[i] = 1000; }
        var v = new Vpin(close, vol, bucketPeriod: 3, sigmaPeriod: 3);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(v);
        Assert.IsAssignableFrom<IIndicator>(v);
    }
}
