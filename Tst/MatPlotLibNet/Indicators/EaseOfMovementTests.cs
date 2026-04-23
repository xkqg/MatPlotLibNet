// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="EaseOfMovement"/>. Covers all 9 branches in
/// docs/contrib/indicator-tier-3a.md §3.</summary>
public class EaseOfMovementTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var emv = new EaseOfMovement([], [], []);
        Assert.Empty(emv.Compute().Values);
    }

    // ── Branch 2 — HLV length mismatch → throw ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new EaseOfMovement([1.0, 2], [1.0, 2, 3], [1000.0, 1000]));
        Assert.Throws<ArgumentException>(() =>
            new EaseOfMovement([1.0, 2], [1.0, 2], [1000.0, 1000, 1000]));
    }

    // ── Branch 3 — BarCount <= period → empty ──
    [Fact]
    public void Compute_BarCountAtOrBelowPeriod_ReturnsEmpty()
    {
        // period=14 default, 14 bars → BarCount == period → empty
        var emv = new EaseOfMovement(Flat(100, 14), Flat(99, 14), Flat(1000, 14));
        Assert.Empty(emv.Compute().Values);
    }

    // ── Branch 4 — period < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_PeriodBelowTwo_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() =>
            new EaseOfMovement(Flat(100, 20), Flat(99, 20), Flat(1000, 20), period));
    }

    // ── Branch 5 — scale <= 0 → throw ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_NonPositiveScale_Throws(double scale)
    {
        Assert.Throws<ArgumentException>(() =>
            new EaseOfMovement(Flat(100, 20), Flat(99, 20), Flat(1000, 20), scale: scale));
    }

    // ── Branch 6 — Zero volume → BoxRatio = 0 → EMV_1 = 0 (division guard) ──
    [Fact]
    public void Compute_ZeroVolume_NoDivisionByZero()
    {
        int n = 30;
        // Non-flat H/L so MidpointMove is non-zero, but zero volume → BoxRatio = 0.
        var h = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var l = Enumerable.Range(0, n).Select(i => 99.0 + i).ToArray();
        var v = Flat(0, n);
        var emv = new EaseOfMovement(h, l, v, period: 10).Compute().Values;
        Assert.All(emv, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
            Assert.Equal(0.0, x, precision: 10);
        });
    }

    // ── Branch 7 — Zero range (H == L) → BoxRatio = ∞ → EMV_1 = 0 (guard) ──
    [Fact]
    public void Compute_ZeroRange_NoInfinity()
    {
        int n = 30;
        // H == L on every bar but midpoint moves (via increasing common value).
        var same = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var v = Flat(1000, n);
        var emv = new EaseOfMovement(same, same, v, period: 10).Compute().Values;
        Assert.All(emv, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
            Assert.Equal(0.0, x, precision: 10);
        });
    }

    // ── Branch 8 — Flat midpoints → MidpointMove = 0 → EMV = 0 ──
    [Fact]
    public void Compute_FlatMidpoints_ReturnsZero()
    {
        int n = 30;
        var emv = new EaseOfMovement(Flat(100, n), Flat(99, n), Flat(1000, n), period: 10)
            .Compute().Values;
        Assert.Equal(n - 10, emv.Length);
        Assert.All(emv, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 9 — Normal multi-bar path ──
    [Fact]
    public void Compute_RisingMidpoint_AllFinite()
    {
        int n = 40;
        var h = Enumerable.Range(0, n).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, n).Select(i => 99.0 + i).ToArray();
        var v = Flat(1000, n);
        var emv = new EaseOfMovement(h, l, v, period: 14, scale: 1e6).Compute().Values;
        Assert.Equal(n - 14, emv.Length);
        Assert.All(emv, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
        });
        // Rising midpoints + constant volume + constant range → positive EMV.
        Assert.True(emv[^1] > 0, $"expected positive EMV on rising midpoint, got {emv[^1]}");
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        int n = 30;
        var h = Enumerable.Range(0, n).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, n).Select(i => 99.0 + i).ToArray();
        new EaseOfMovement(h, l, Flat(1000, n)).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_HasPeriod()
    {
        var emv = new EaseOfMovement([1.0, 2], [1.0, 2], [1000.0, 1000]);
        Assert.Equal("EMV(14)", emv.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var emv = new EaseOfMovement([1.0, 2], [1.0, 2], [1000.0, 1000]);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(emv);
        Assert.IsAssignableFrom<IIndicator>(emv);
    }
}
