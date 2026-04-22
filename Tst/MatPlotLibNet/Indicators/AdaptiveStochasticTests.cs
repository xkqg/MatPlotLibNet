// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="AdaptiveStochastic"/>. Covers all 11 branches in
/// docs/contrib/indicator-tier-2c.md §4.</summary>
public class AdaptiveStochasticTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new AdaptiveStochastic([], [], []);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 2 — HLC length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new AdaptiveStochastic([101.0, 102], [99.0], [100.0, 100.5]));
    }

    // ── Branch 3 — BarCount < 7 → empty ──
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    public void Compute_BarCountBelowSeven_ReturnsEmpty(int n)
    {
        var h = Enumerable.Range(0, n).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, n).Select(i => 99.0 + i).ToArray();
        var c = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var r = new AdaptiveStochastic(h, l, c);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 4 — smoothingPeriod < 1 throws ──
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveSmoothing_Throws(int sp)
    {
        Assert.Throws<ArgumentException>(() =>
            new AdaptiveStochastic([1.0, 2], [1.0, 2], [1.0, 2], smoothingPeriod: sp));
    }

    // ── Branch 5 — H < L throws ──
    [Fact]
    public void Constructor_HighBelowLow_Throws()
    {
        // H < L on bar 1 (H=99 < L=101).
        Assert.Throws<ArgumentException>(() =>
            new AdaptiveStochastic([101.0, 99], [99.0, 101], [100.0, 100.5]));
    }

    // ── Branch 6 — Non-positive price is NOT a throw (range ratio doesn't need logs) ──
    [Fact]
    public void Constructor_NonPositivePrice_DoesNotThrow()
    {
        // Stochastic uses raw HLC ranges, not logs. Negative prices are weird but
        // don't crash the algorithm.
        var ex = Record.Exception(() =>
            new AdaptiveStochastic([1.0, 2, 3, 4, 5, 6, 7], [-1.0, 0, 1, 2, 3, 4, 5], [0.0, 1, 2, 3, 4, 5, 6]));
        Assert.Null(ex);
    }

    // ── Branch 7 — Flat H==L zero-range guard → output 50 (neutral) ──
    [Fact]
    public void Compute_FlatHEqualsL_ReturnsFifty()
    {
        var prices = Enumerable.Repeat(100.0, 30).ToArray();
        var r = new AdaptiveStochastic(prices, prices, prices).Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(50.0, v, precision: 6);
        });
    }

    // ── Branch 8 — Constant prices (H > L but close doesn't move within window) → near 50 ──
    [Fact]
    public void Compute_ConstantRanges_NearFifty()
    {
        // H = 101, L = 99, C = 100 constant. Range is fixed, close always at midpoint.
        var h = Enumerable.Repeat(101.0, 30).ToArray();
        var l = Enumerable.Repeat(99.0, 30).ToArray();
        var c = Enumerable.Repeat(100.0, 30).ToArray();
        var r = new AdaptiveStochastic(h, l, c).Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 40.0, 60.0);
        });
    }

    // ── Branch 9 & 10 — Adaptive lookback hit at lower / upper clamps ──
    [Fact]
    public void Compute_LongSeries_OutputBoundedZeroToHundred()
    {
        int n = 300;
        var rng = new Random(42);
        var c = new double[n]; c[0] = 100;
        for (int i = 1; i < n; i++)
            c[i] = c[i - 1] * (1 + (rng.NextDouble() - 0.5) * 0.02);
        var h = c.Select(v => v + 1).ToArray();
        var l = c.Select(v => v - 1).ToArray();

        var r = new AdaptiveStochastic(h, l, c).Compute().Values;
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 0.0, 100.0);
        });
    }

    // ── Branch 11 — Normal multi-bar path — output at top of rising range = 100 ──
    [Fact]
    public void Compute_Uptrend_OutputNearHundredAtPeak()
    {
        // Strong monotonic uptrend — close always near the high of its window → %K near 100.
        var c = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        var r = new AdaptiveStochastic(h, l, c, smoothingPeriod: 1).Compute().Values;
        Assert.True(r[^1] > 80, $"expected %K > 80 in strong uptrend, got {r[^1]}");
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var c = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        new AdaptiveStochastic(h, l, c).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToHundred()
    {
        var axes = new Axes();
        var c = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        new AdaptiveStochastic(h, l, c).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(100, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var c = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        new AdaptiveStochastic(h, l, c, smoothingPeriod: 3).Apply(axes);
        Assert.Equal("AdaptStoch(3)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultSmoothingPeriod_IsThree()
    {
        var c = Enumerable.Range(0, 10).Select(i => 100.0 + i).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        var r = new AdaptiveStochastic(h, l, c);
        Assert.Equal("AdaptStoch(3)", r.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var c = Enumerable.Range(0, 10).Select(i => 100.0 + i).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        var r = new AdaptiveStochastic(h, l, c);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
