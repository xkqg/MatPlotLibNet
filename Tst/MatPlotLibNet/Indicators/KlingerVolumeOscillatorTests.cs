// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="KlingerVolumeOscillator"/>. Covers all 11 branches in
/// docs/contrib/indicator-tier-3a.md §1.</summary>
public class KlingerVolumeOscillatorTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Ramp(double start, double step, int n)
        => Enumerable.Range(0, n).Select(i => start + step * i).ToArray();

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var k = new KlingerVolumeOscillator([], [], [], []);
        var r = k.Compute();
        Assert.Empty(r.Kvo);
        Assert.Empty(r.Signal);
    }

    // ── Branch 2 — HLCV length mismatch → throw ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new KlingerVolumeOscillator([1.0, 2, 3], [1, 2], [1.0, 2, 3], [1.0, 2, 3]));
        Assert.Throws<ArgumentException>(() =>
            new KlingerVolumeOscillator([1.0, 2, 3], [1.0, 2, 3], [1.0, 2], [1.0, 2, 3]));
        Assert.Throws<ArgumentException>(() =>
            new KlingerVolumeOscillator([1.0, 2, 3], [1.0, 2, 3], [1.0, 2, 3], [1.0, 2]));
    }

    // ── Branch 3 — BarCount < 2 → empty ──
    [Fact]
    public void Compute_BarCountBelowTwo_ReturnsEmpty()
    {
        var k = new KlingerVolumeOscillator([100.0], [99.0], [99.5], [1000.0]);
        var r = k.Compute();
        Assert.Empty(r.Kvo);
        Assert.Empty(r.Signal);
    }

    // ── Branch 4 — fastPeriod < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_FastPeriodBelowTwo_Throws(int fast)
    {
        Assert.Throws<ArgumentException>(() =>
            new KlingerVolumeOscillator(Ramp(100, 1, 5), Ramp(99, 1, 5), Ramp(99.5, 1, 5), Flat(1000, 5),
                fastPeriod: fast));
    }

    // ── Branch 5 — slowPeriod <= fastPeriod → throw ──
    [Theory]
    [InlineData(34, 34)]
    [InlineData(55, 34)]
    public void Constructor_SlowNotGreaterThanFast_Throws(int fast, int slow)
    {
        Assert.Throws<ArgumentException>(() =>
            new KlingerVolumeOscillator(Ramp(100, 1, 5), Ramp(99, 1, 5), Ramp(99.5, 1, 5), Flat(1000, 5),
                fastPeriod: fast, slowPeriod: slow));
    }

    // ── Branch 6 — signalPeriod < 1 → throw ──
    [Fact]
    public void Constructor_SignalPeriodBelowOne_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new KlingerVolumeOscillator(Ramp(100, 1, 5), Ramp(99, 1, 5), Ramp(99.5, 1, 5), Flat(1000, 5),
                fastPeriod: 2, slowPeriod: 3, signalPeriod: 0));
    }

    // ── Branch 7 — Flat HLC → trend = 0 → VF = 0 → KVO & Signal = 0 ──
    [Fact]
    public void Compute_FlatHlc_ReturnsZero()
    {
        int n = 100;
        var r = new KlingerVolumeOscillator(
            Flat(100, n), Flat(100, n), Flat(100, n), Flat(1000, n)).Compute();
        Assert.Equal(n - 1, r.Kvo.Length);
        Assert.Equal(n - 1, r.Signal.Length);
        Assert.All(r.Kvo, v => Assert.Equal(0.0, v, precision: 10));
        Assert.All(r.Signal, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 8 — CM == 0 (flat H == L on consecutive same-trend bars) → VF = 0 guard ──
    [Fact]
    public void Compute_HEqualsL_NoDivisionByZero()
    {
        // H == L on every bar → (H - L) = 0 everywhere, CM = 0, VF must be 0 (guard arm).
        // Typical price still moves since C varies, so trend is non-zero — but VF must still be 0.
        var h = new double[] { 100, 101, 102, 103, 104, 105, 106, 107 };
        var l = new double[] { 100, 101, 102, 103, 104, 105, 106, 107 };
        var c = new double[] { 100, 101, 102, 103, 104, 105, 106, 107 };
        var v = Flat(1000, 8);
        var r = new KlingerVolumeOscillator(h, l, c, v, fastPeriod: 2, slowPeriod: 3, signalPeriod: 2).Compute();
        Assert.Equal(7, r.Kvo.Length);
        Assert.All(r.Kvo, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.Equal(0.0, x, precision: 10);
        });
    }

    // ── Branch 9 — Trend reversal → CM resets with prev+current range ──
    // ── Branch 10 — Trend sustains → CM accumulates ──
    // ── Branch 11 — Normal multi-bar path ──
    [Fact]
    public void Compute_AlternatingTrend_ProducesFiniteOutput()
    {
        // Saw-tooth HLC that flips trend every bar — exercises the reset arm repeatedly.
        int n = 40;
        var h = new double[n];
        var l = new double[n];
        var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + (i % 2 == 0 ? 1 : -1);
            h[i] = mid + 0.5;
            l[i] = mid - 0.5;
            c[i] = mid;
        }
        var vol = Flat(1000, n);
        var r = new KlingerVolumeOscillator(h, l, c, vol,
            fastPeriod: 3, slowPeriod: 5, signalPeriod: 3).Compute();

        Assert.Equal(n - 1, r.Kvo.Length);
        Assert.Equal(n - 1, r.Signal.Length);
        Assert.All(r.Kvo, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
        });
        Assert.All(r.Signal, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
        });
    }

    [Fact]
    public void Compute_SustainedUptrend_AccumulatesCm()
    {
        // Monotone rising HLC+positive volume → trend = +1 every bar → CM accumulates,
        // VF > 0, KVO settles positive. Strictly checking >0 on tail is sufficient
        // regression against the "CM reset incorrectly" bug class.
        int n = 80;
        var h = Ramp(101, 1, n);
        var l = Ramp(99, 1, n);
        var c = Ramp(100, 1, n);
        var v = Flat(1000, n);
        var r = new KlingerVolumeOscillator(h, l, c, v,
            fastPeriod: 5, slowPeriod: 13, signalPeriod: 5).Compute();

        // Tail portion should have positive KVO (sustained up-trend → positive VF → positive EMAs diff).
        // With fast=5, slow=13, the sign of KVO depends on whether fast EMA of VF exceeds slow EMA.
        // For a strictly-increasing input, the *levels* settle but fast EMA tracks closer — should be > 0.
        Assert.True(r.Kvo[^1] > 0,
            $"expected positive KVO tail on sustained uptrend, got {r.Kvo[^1]}");
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsTwoLineSeries()
    {
        var axes = new Axes();
        int n = 30;
        new KlingerVolumeOscillator(Ramp(101, 1, n), Ramp(99, 1, n), Ramp(100, 1, n), Flat(1000, n))
            .Apply(axes);
        Assert.Equal(2, axes.Series.Count);
        Assert.All(axes.Series, s => Assert.IsType<LineSeries>(s));
    }

    [Fact]
    public void Apply_SetsExpectedLabels()
    {
        var axes = new Axes();
        int n = 30;
        new KlingerVolumeOscillator(Ramp(101, 1, n), Ramp(99, 1, n), Ramp(100, 1, n), Flat(1000, n))
            .Apply(axes);
        Assert.Equal("KVO", axes.Series[0].Label);
        Assert.Equal("Signal", axes.Series[1].Label);
    }

    [Fact]
    public void DefaultLabel_HasPeriodSignature()
    {
        var k = new KlingerVolumeOscillator([1.0, 2], [1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.Equal("KVO(34/55/13)", k.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var k = new KlingerVolumeOscillator([1.0, 2], [1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.IsAssignableFrom<CandleIndicator<KlingerResult>>(k);
        Assert.IsAssignableFrom<IIndicator>(k);
    }
}
