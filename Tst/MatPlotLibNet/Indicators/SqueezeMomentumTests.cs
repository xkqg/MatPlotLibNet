// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="SqueezeMomentum"/>. Covers all 12 branches enumerated in
/// docs/contrib/indicator-tier-1d.md §1.</summary>
public class SqueezeMomentumTests
{
    private static double[] Flat(int count, double value) =>
        Enumerable.Repeat(value, count).ToArray();

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var sq = new SqueezeMomentum([], [], [], period: 20);
        var r = sq.Compute();
        Assert.Empty(r.SqueezeOn);
        Assert.Empty(r.Momentum);
    }

    // ── Branch 2 — HLC length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SqueezeMomentum([100.0, 101], [99.0], [100.0, 100.5], period: 2));
    }

    // ── Branch 3 — period <= 1 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_PeriodBelowTwo_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() =>
            new SqueezeMomentum([102.0, 103], [100.0, 101], [101.0, 102], period));
    }

    // ── Branch 4 — bbMult or kcMult <= 0 throws ──
    [Theory]
    [InlineData(0.0, 1.5)]
    [InlineData(-1.0, 1.5)]
    [InlineData(2.0, 0.0)]
    [InlineData(2.0, -0.5)]
    public void Constructor_NonPositiveMultiplier_Throws(double bbMult, double kcMult)
    {
        Assert.Throws<ArgumentException>(() =>
            new SqueezeMomentum(
                [102.0, 103, 104], [100.0, 101, 102], [101.0, 102, 103],
                period: 2, bbMult, kcMult));
    }

    // ── Branch 5 — Length <= period + 1 → empty ──
    [Fact]
    public void Compute_LengthAtPeriodPlusOne_ReturnsEmpty()
    {
        int p = 3;
        var h = new double[p + 1]; var l = new double[p + 1]; var c = new double[p + 1];
        for (int i = 0; i < p + 1; i++) { h[i] = 101 + i; l[i] = 99 + i; c[i] = 100 + i; }
        var sq = new SqueezeMomentum(h, l, c, period: p);
        var r = sq.Compute();
        Assert.Empty(r.Momentum);
    }

    // ── Branch 6 — Length == period + 2 → single output row ──
    [Fact]
    public void Compute_LengthAtPeriodPlusTwo_ReturnsSingleRow()
    {
        int p = 3;
        var h = new double[p + 2]; var l = new double[p + 2]; var c = new double[p + 2];
        for (int i = 0; i < p + 2; i++) { h[i] = 101 + i; l[i] = 99 + i; c[i] = 100 + i; }
        var sq = new SqueezeMomentum(h, l, c, period: p);
        var r = sq.Compute();
        Assert.Single(r.Momentum);
        Assert.Single(r.SqueezeOn);
    }

    // ── Branch 7 — H < L on any bar → throw ──
    [Fact]
    public void Constructor_HighBelowLow_Throws()
    {
        // Bar 1 has H=100 < L=101 → throw.
        Assert.Throws<ArgumentException>(() =>
            new SqueezeMomentum(
                [101.0, 100], [99.0, 101], [100.0, 100.5], period: 2));
    }

    // ── Branch 8 — Flat HLC → stddev=0, TR=0 → squeezeOn=false (strict <), momentum=0 ──
    [Fact]
    public void Compute_FlatPrices_SqueezeOffAndMomentumZero()
    {
        int p = 20;
        var flat = Flat(22, 100.0);
        var sq = new SqueezeMomentum(flat, flat, flat, period: p);
        var r = sq.Compute();
        Assert.NotEmpty(r.Momentum);
        Assert.All(r.SqueezeOn, v => Assert.False(v));
        Assert.All(r.Momentum, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(0.0, v, precision: 9);
        });
    }

    // ── Branch 9 — Narrow-range window → BB inside KC → squeezeOn=true ──
    [Fact]
    public void Compute_NarrowRange_SqueezeOn()
    {
        // Tiny symmetric bar-to-bar variation keeps stddev of close small (BB narrow);
        // HL range is also small but kcMult=1.5 makes KC wider than BB.
        int p = 5;
        // Alternate close between 100.0 and 100.1 — very low volatility.
        double[] c = [100, 100.1, 100, 100.1, 100, 100.1, 100];
        double[] h = [100.05, 100.15, 100.05, 100.15, 100.05, 100.15, 100.05];
        double[] l = [99.95, 100.05, 99.95, 100.05, 99.95, 100.05, 99.95];
        var sq = new SqueezeMomentum(h, l, c, period: p, bbMult: 2.0, kcMult: 3.0);
        var r = sq.Compute();
        Assert.NotEmpty(r.SqueezeOn);
        // With kcMult=3 (wide KC) and stable small oscillation → expect BB inside KC.
        Assert.Contains(r.SqueezeOn, v => v == true);
    }

    // ── Branch 10 — Transition: squeeze on → squeeze off ──
    [Fact]
    public void Compute_WideningVolatility_TransitionDetected()
    {
        int p = 4;
        // Tight, identical bars first (BB & KC both narrow; BB wider than KC thanks to bbMult=2
        // on stddev vs kcMult=1 on TR). Then a wide-close-swing block pushes BB outside KC.
        double[] h = [100.05, 100.05, 100.05, 100.05, 100.05, 100.05, 110, 115, 108, 113, 106, 112];
        double[] l = [99.95,  99.95,  99.95,  99.95,  99.95,  99.95,  90,  85,  92,  87,  94,  88];
        double[] c = [100,    100,    100,    100,    100,    100,    108, 88,  106, 89,  105, 90];
        var sq = new SqueezeMomentum(h, l, c, period: p, bbMult: 2.0, kcMult: 1.0);
        var r = sq.Compute();
        Assert.Contains(r.SqueezeOn, v => v == true);
        Assert.Contains(r.SqueezeOn, v => v == false);
    }

    // ── Branch 11 — Pure uptrend → momentum > 0 ──
    [Fact]
    public void Compute_Uptrend_MomentumPositive()
    {
        int p = 5;
        double[] c = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var sq = new SqueezeMomentum(h, l, c, period: p);
        var r = sq.Compute();
        Assert.All(r.Momentum, v => Assert.True(v > 0, $"expected momentum > 0, got {v}"));
    }

    // ── Branch 12 — Pure downtrend → momentum < 0 ──
    [Fact]
    public void Compute_Downtrend_MomentumNegative()
    {
        int p = 5;
        double[] c = Enumerable.Range(0, 20).Select(i => 120.0 - i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var sq = new SqueezeMomentum(h, l, c, period: p);
        var r = sq.Compute();
        Assert.All(r.Momentum, v => Assert.True(v < 0, $"expected momentum < 0, got {v}"));
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsAtLeastOneSeries()
    {
        var axes = new Axes();
        int p = 5;
        double[] c = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        new SqueezeMomentum(h, l, c, period: p).Apply(axes);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        int p = 5;
        double[] c = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        new SqueezeMomentum(h, l, c, period: p).Apply(axes);
        Assert.Equal("Squeeze(5)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_Is20()
    {
        double[] c = Enumerable.Range(0, 25).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var sq = new SqueezeMomentum(h, l, c);
        Assert.Equal("Squeeze(20)", sq.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsCandleIndicator()
    {
        int p = 5;
        double[] c = Enumerable.Range(0, 10).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var sq = new SqueezeMomentum(h, l, c, period: p);
        Assert.IsAssignableFrom<CandleIndicator<SqueezeResult>>(sq);
        Assert.IsAssignableFrom<IIndicator>(sq);
    }
}
