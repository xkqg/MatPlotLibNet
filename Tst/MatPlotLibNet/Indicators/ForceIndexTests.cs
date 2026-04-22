// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="ForceIndex"/>. Covers all 10 branches in
/// docs/contrib/indicator-tier-2d.md §1.</summary>
public class ForceIndexTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new ForceIndex([], [], period: 1);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 2 — Length == 1 → empty ──
    [Fact]
    public void Compute_SingleBar_ReturnsEmpty()
    {
        var r = new ForceIndex([100.0], [1000.0], period: 1);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 3 — Length == 2 → single output ──
    [Fact]
    public void Compute_TwoBars_ReturnsSingleValue()
    {
        var r = new ForceIndex([100.0, 102.0], [500.0, 800.0], period: 1);
        var v = r.Compute().Values;
        Assert.Single(v);
        // Raw force at bar 1: 800 × (102 - 100) = 1600.
        Assert.Equal(1600.0, v[0], precision: 9);
    }

    // ── Branch 4 — Length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ForceIndex([100.0, 101, 102], [1000.0, 1000], period: 1));
    }

    // ── Branch 5 — period < 1 throws ──
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositivePeriod_Throws(int p)
    {
        Assert.Throws<ArgumentException>(() =>
            new ForceIndex([100.0, 101], [1000.0, 1000], p));
    }

    // ── Branch 6 — period == 1 → no smoothing, raw force only ──
    [Fact]
    public void Compute_PeriodOne_KnownVector_MatchesRawForce()
    {
        // close: [100, 102, 101, 103], volume: [500, 800, 400, 600]
        // raw: [800 × 2, 400 × -1, 600 × 2] = [1600, -400, 1200]
        var close = new[] { 100.0, 102.0, 101.0, 103.0 };
        var volume = new[] { 500.0, 800.0, 400.0, 600.0 };
        var r = new ForceIndex(close, volume, period: 1).Compute().Values;
        Assert.Equal(3, r.Length);
        Assert.Equal(1600.0, r[0], precision: 9);
        Assert.Equal(-400.0, r[1], precision: 9);
        Assert.Equal(1200.0, r[2], precision: 9);
    }

    // ── Branch 7 — Flat prices → force = 0 ──
    [Fact]
    public void Compute_FlatPrices_ReturnsZero()
    {
        var r = new ForceIndex(
            [100.0, 100, 100, 100, 100],
            [1000.0, 1000, 1000, 1000, 1000],
            period: 1).Compute().Values;
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 8 — Zero volume on a bar → that bar's force = 0 ──
    [Fact]
    public void Compute_ZeroVolume_ContributesZero()
    {
        // close [100, 105, 110], volume [500, 0, 1000]
        // raw: [0 × 5, 1000 × 5] = [0, 5000]
        var r = new ForceIndex([100.0, 105, 110], [500.0, 0, 1000], period: 1).Compute().Values;
        Assert.Equal(2, r.Length);
        Assert.Equal(0.0, r[0], precision: 12);
        Assert.Equal(5000.0, r[1], precision: 12);
    }

    // ── Branch 9 — Negative volume accepted (no throw) — math still defines something ──
    [Fact]
    public void Constructor_NegativeVolume_DoesNotThrow()
    {
        // Negative volume is semantically invalid but we don't reject it at ctor.
        var ex = Record.Exception(() =>
            new ForceIndex([100.0, 101], [-500.0, -500.0], period: 1));
        Assert.Null(ex);
    }

    // ── Branch 10 — EMA smoothing (period > 1) ──
    //
    // With period=3, raw = [1600, -400, 1200]. EMA with adjust=false seeds at raw[0]:
    //   k = 2/(3+1) = 0.5
    //   e[0] = 1600
    //   e[1] = 0.5 × (-400) + 0.5 × 1600 = -200 + 800 = 600
    //   e[2] = 0.5 × 1200 + 0.5 × 600 = 600 + 300 = 900
    [Fact]
    public void Compute_EmaSmoothing_MatchesKnownReference()
    {
        var close = new[] { 100.0, 102.0, 101.0, 103.0 };
        var volume = new[] { 500.0, 800.0, 400.0, 600.0 };
        var r = new ForceIndex(close, volume, period: 3).Compute().Values;
        Assert.Equal(3, r.Length);
        Assert.Equal(1600.0, r[0], precision: 9);
        Assert.Equal(600.0, r[1], precision: 9);
        Assert.Equal(900.0, r[2], precision: 9);
    }

    // ── Apply / label ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new ForceIndex([100.0, 101, 102, 103], [1000.0, 1000, 1000, 1000]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new ForceIndex([100.0, 101], [1000.0, 1000], period: 13).Apply(axes);
        Assert.Equal("Force(13)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_IsThirteen()
    {
        var r = new ForceIndex([100.0, 101], [1000.0, 1000]);
        Assert.Equal("Force(13)", r.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var r = new ForceIndex([100.0, 101], [1000.0, 1000]);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
