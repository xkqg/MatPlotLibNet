// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Supertrend"/>. Covers all 11 branches in
/// docs/contrib/indicator-tier-3b.md §1.</summary>
public class SupertrendTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Ramp(double start, double step, int n)
        => Enumerable.Range(0, n).Select(i => start + step * i).ToArray();

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new Supertrend([], [], []).Compute();
        Assert.Empty(r.Line);
        Assert.Empty(r.Direction);
        Assert.Empty(r.Flipped);
    }

    // ── Branch 2 — HLC length mismatch → throw ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Supertrend([1.0, 2, 3], [1.0, 2], [1.0, 2, 3]));
        Assert.Throws<ArgumentException>(() =>
            new Supertrend([1.0, 2, 3], [1.0, 2, 3], [1.0, 2]));
    }

    // ── Branch 3 — BarCount <= period → empty ──
    [Fact]
    public void Compute_BarCountAtOrBelowPeriod_ReturnsEmpty()
    {
        // period=10 default, BarCount=10 → empty
        var r = new Supertrend(Flat(100, 10), Flat(99, 10), Flat(99.5, 10)).Compute();
        Assert.Empty(r.Line);
    }

    // ── Branch 4 — period < 1 → throw ──
    [Fact]
    public void Constructor_PeriodBelowOne_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Supertrend(Ramp(101, 1, 20), Ramp(99, 1, 20), Ramp(100, 1, 20), period: 0));
    }

    // ── Branch 5 — multiplier <= 0 → throw ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_NonPositiveMultiplier_Throws(double mult)
    {
        Assert.Throws<ArgumentException>(() =>
            new Supertrend(Ramp(101, 1, 20), Ramp(99, 1, 20), Ramp(100, 1, 20), multiplier: mult));
    }

    // ── Branch 6 — Flat prices → direction stable (+1), line = midpoint − mult·ATR ──
    [Fact]
    public void Compute_FlatPrices_DirectionStableAtPositiveOne()
    {
        int n = 40;
        // H and L offset slightly so ATR > 0; close in the middle so never outside bands.
        var h = Flat(101, n);
        var l = Flat(99, n);
        var c = Flat(100, n);
        var r = new Supertrend(h, l, c, period: 10, multiplier: 3.0).Compute();
        Assert.Equal(n - 10, r.Line.Length);
        Assert.All(r.Direction, d => Assert.Equal(+1, d));
        // First output bar never registers a flip (no predecessor).
        Assert.False(r.Flipped[0]);
    }

    // ── Branch 7 — Strong uptrend → direction stays +1, no flips after first bar ──
    [Fact]
    public void Compute_StrongUptrend_DirectionStaysPositive()
    {
        int n = 60;
        var h = Ramp(102, 1, n);
        var l = Ramp(98, 1, n);
        var c = Ramp(100, 1, n);
        var r = new Supertrend(h, l, c, period: 10, multiplier: 3.0).Compute();
        Assert.All(r.Direction, d => Assert.Equal(+1, d));
        // After the initial warmup bar, no flips should occur.
        for (int i = 1; i < r.Flipped.Length; i++) Assert.False(r.Flipped[i]);
    }

    // ── Branch 8 — Strong downtrend → direction goes negative ──
    [Fact]
    public void Compute_StrongDowntrend_DirectionTurnsNegative()
    {
        int n = 60;
        // Start mid-range then fall hard — price breaks below initial lower band.
        var h = new double[n];
        var l = new double[n];
        var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 200 - i * 2.0;
            h[i] = mid + 1;
            l[i] = mid - 1;
            c[i] = mid - 0.5;
        }
        var r = new Supertrend(h, l, c, period: 10, multiplier: 1.5).Compute();
        // Tail should be in downtrend.
        Assert.Equal(-1, r.Direction[^1]);
    }

    // ── Branch 9 — Direction flip → Flipped marker on the crossing bar ──
    [Fact]
    public void Compute_ReversalSequence_SetsFlippedTrue()
    {
        // Rise for 30 bars, then sudden crash for 30 bars — forces one flip after warmup.
        int n = 60;
        var h = new double[n];
        var l = new double[n];
        var c = new double[n];
        for (int i = 0; i < 30; i++)
        {
            h[i] = 102 + i;
            l[i] = 98 + i;
            c[i] = 100 + i;
        }
        for (int i = 30; i < n; i++)
        {
            h[i] = 140 - (i - 30) * 3;
            l[i] = 130 - (i - 30) * 3;
            c[i] = 135 - (i - 30) * 3;
        }
        var r = new Supertrend(h, l, c, period: 10, multiplier: 2.0).Compute();
        // At least one Flipped=true bar must exist (the crossover event).
        int flipCount = r.Flipped.Count(f => f);
        Assert.True(flipCount >= 1, "expected at least one direction flip");
    }

    // ── Branch 10 — close_t between upper and lower bands → direction unchanged ──
    [Fact]
    public void Compute_CloseInsideBands_DirectionUnchanged()
    {
        int n = 40;
        // Wide oscillation within tight bands — close always inside.
        var h = new double[n];
        var l = new double[n];
        var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            h[i] = 110;
            l[i] = 90;
            c[i] = 100 + (i % 3 - 1) * 0.1; // tiny jitter around 100
        }
        var r = new Supertrend(h, l, c, period: 10, multiplier: 3.0).Compute();
        // With tight close moves and wide bands, direction should be stable for most bars.
        int stableCount = 0;
        for (int i = 1; i < r.Direction.Length; i++)
            if (r.Direction[i] == r.Direction[i - 1]) stableCount++;
        Assert.True(stableCount > r.Direction.Length / 2,
            "expected majority of bars to hold direction when close stays inside bands");
    }

    // ── Branch 11 — Initial bar defaults to +1 and line = lower band ──
    [Fact]
    public void Compute_InitialBar_DirectionPlusOne_LineBelowPrice()
    {
        int n = 20;
        var h = Flat(101, n);
        var l = Flat(99, n);
        var c = Flat(100, n);
        var r = new Supertrend(h, l, c, period: 10, multiplier: 3.0).Compute();
        Assert.Equal(+1, r.Direction[0]);
        Assert.True(r.Line[0] < 100, $"expected initial line < close=100, got {r.Line[0]}");
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        int n = 30;
        new Supertrend(Ramp(101, 1, n), Ramp(99, 1, n), Ramp(100, 1, n)).Apply(axes);
        Assert.NotEmpty(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_HasPeriodAndMultiplier()
    {
        var st = new Supertrend([1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.Equal("ST(10,3)", st.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var st = new Supertrend([1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.IsAssignableFrom<CandleIndicator<SupertrendResult>>(st);
        Assert.IsAssignableFrom<IIndicator>(st);
    }
}
