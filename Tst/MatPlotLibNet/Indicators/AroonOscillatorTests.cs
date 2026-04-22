// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="AroonOscillator"/>. Covers all 12 branches in
/// docs/contrib/indicator-tier-2d.md §2.</summary>
public class AroonOscillatorTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(new AroonOscillator([], [], period: 5).Compute().Values);
    }

    // ── Branch 2 — HL length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new AroonOscillator([101.0, 102], [99.0], period: 2));
    }

    // ── Branch 3 — period < 2 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_PeriodBelowTwo_Throws(int p)
    {
        Assert.Throws<ArgumentException>(() =>
            new AroonOscillator([101.0, 102, 103], [99.0, 100, 101], p));
    }

    // ── Branch 4 — BarCount <= period → empty ──
    [Fact]
    public void Compute_BarCountEqualsPeriod_ReturnsEmpty()
    {
        var r = new AroonOscillator(
            [101.0, 102, 103, 104, 105], [99.0, 100, 101, 102, 103], period: 5);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 5 — BarCount == period + 1 → single output ──
    [Fact]
    public void Compute_BarCountEqualsPeriodPlusOne_ReturnsSingleValue()
    {
        var r = new AroonOscillator(
            [101.0, 102, 103, 104, 105, 106], [99.0, 100, 101, 102, 103, 104], period: 5);
        Assert.Single(r.Compute().Values);
    }

    // ── Branch 6 — H < L throws ──
    [Fact]
    public void Constructor_HighBelowLow_Throws()
    {
        // Bar 1 has H=99 < L=100 — invalid.
        Assert.Throws<ArgumentException>(() =>
            new AroonOscillator([101.0, 99], [99.0, 100], period: 2));
    }

    // ── Branch 7 — Ties on highest high: most recent wins ──
    //   h = [100, 100, 100], l = [99, 99, 99], period = 2
    //   At output index 0 (bar t=2): window = bars [0..2], all H=100 → tie → most recent (bar 2).
    //   bars_since_high = 0 → Up = 100.
    //   L window: all L=99 → tie → most recent (bar 2). bars_since_low = 0 → Down = 100.
    //   Osc = 0.
    [Fact]
    public void Compute_HighTies_MostRecentWins()
    {
        double[] h = [100.0, 100, 100];
        double[] l = [99.0, 99, 99];
        var r = new AroonOscillator(h, l, period: 2).Compute().Values;
        Assert.Single(r);
        Assert.Equal(0.0, r[0], precision: 10);
    }

    // ── Branch 8 — Ties on lowest low: most recent wins ──
    //   Exercised together with Branch 7 in the flat-prices test above. Add a targeted
    //   case where H varies but L ties: low never changes, Up fluctuates, Down stays 100.
    [Fact]
    public void Compute_LowTies_DownAlwaysHundred()
    {
        // H rises, L stays flat → Down always 100 (most recent low = today).
        double[] h = [100.0, 101, 102, 103];
        double[] l = [99.0, 99, 99, 99];
        var r = new AroonOscillator(h, l, period: 3).Compute().Values;
        Assert.Single(r);
        // Bar t=3, window [0..3]:
        //   Highs = 100, 101, 102, 103 — newest is bar 3 → bars_since_high = 0 → Up = 100.
        //   Lows = all 99 — tie → most recent → bars_since_low = 0 → Down = 100.
        //   Osc = 0.
        Assert.Equal(0.0, r[0], precision: 10);
    }

    // ── Branch 9 — Flat prices → ties → Osc = 0 ──
    [Fact]
    public void Compute_FlatPrices_ReturnsZero()
    {
        var h = Enumerable.Repeat(100.0, 30).ToArray();
        var l = Enumerable.Repeat(100.0, 30).ToArray();
        var r = new AroonOscillator(h, l, period: 10).Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 10 — Monotonic rise → Osc near +100 ──
    [Fact]
    public void Compute_MonotonicRise_OscillatorNearHundred()
    {
        var rising = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        var r = new AroonOscillator(rising, rising, period: 5).Compute().Values;
        Assert.NotEmpty(r);
        // At first output (bar t=5), window [0..5]: high = bar 5, low = bar 0.
        // Up = 100*(5 − 0)/5 = 100; Down = 100*(5 − 5)/5 = 0; Osc = 100.
        Assert.Equal(100.0, r[0], precision: 10);
        Assert.All(r, v => Assert.Equal(100.0, v, precision: 10));
    }

    // ── Branch 11 — Monotonic fall → Osc near -100 ──
    [Fact]
    public void Compute_MonotonicFall_OscillatorNearMinusHundred()
    {
        var falling = Enumerable.Range(0, 20).Select(i => 120.0 - i).ToArray();
        var r = new AroonOscillator(falling, falling, period: 5).Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.Equal(-100.0, v, precision: 10));
    }

    // ── Branch 12 — Normal multi-bar path ──
    [Fact]
    public void Compute_MixedSignal_OutputBounded()
    {
        var rng = new Random(42);
        var h = new double[100];
        var l = new double[100];
        for (int i = 0; i < 100; i++)
        {
            double mid = 100 + rng.NextDouble() * 10;
            h[i] = mid + 0.5;
            l[i] = mid - 0.5;
        }
        var r = new AroonOscillator(h, l, period: 25).Compute().Values;
        Assert.Equal(75, r.Length); // 100 - 25
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, -100.0, 100.0);
        });
    }

    // ── Apply / label ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var h = Enumerable.Range(0, 30).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, 30).Select(i => 99.0 + i).ToArray();
        new AroonOscillator(h, l, period: 10).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisRange()
    {
        var axes = new Axes();
        var h = Enumerable.Range(0, 30).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, 30).Select(i => 99.0 + i).ToArray();
        new AroonOscillator(h, l, period: 10).Apply(axes);
        Assert.Equal(-100, axes.YAxis.Min);
        Assert.Equal(100, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var h = Enumerable.Range(0, 30).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, 30).Select(i => 99.0 + i).ToArray();
        new AroonOscillator(h, l, period: 25).Apply(axes);
        Assert.Equal("Aroon(25)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_IsTwentyFive()
    {
        var h = Enumerable.Range(0, 30).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, 30).Select(i => 99.0 + i).ToArray();
        var r = new AroonOscillator(h, l);
        Assert.Equal("Aroon(25)", r.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var r = new AroonOscillator([101.0, 102], [99.0, 100], period: 2);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
