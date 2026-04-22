// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="RelativeVigorIndex"/>. Covers all 10 branches in
/// docs/contrib/indicator-tier-2d.md §3.</summary>
public class RelativeVigorIndexTests
{
    // ── Branch 1 — Empty input → both arrays empty ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new RelativeVigorIndex([], [], [], [], period: 5).Compute();
        Assert.Empty(r.Rvi);
        Assert.Empty(r.Signal);
    }

    // ── Branch 2 — OHLC length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new RelativeVigorIndex([100.0, 101], [102.0], [99.0, 100], [100.5, 100.8], period: 2));
    }

    // ── Branch 3 — period < 2 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_PeriodBelowTwo_Throws(int p)
    {
        var a = Enumerable.Repeat(100.0, 10).ToArray();
        Assert.Throws<ArgumentException>(() =>
            new RelativeVigorIndex(a, a, a, a, p));
    }

    // ── Branch 4 — BarCount <= period + 3 → empty (insufficient for weighted-smooth + SMA) ──
    [Fact]
    public void Compute_InsufficientBars_ReturnsEmpty()
    {
        // period=5 → need > period + 5 = 10 bars for full RVI+Signal output; 7 < 8 = period+3.
        var a = Enumerable.Repeat(100.0, 7).ToArray();
        var r = new RelativeVigorIndex(a, a, a, a, period: 5).Compute();
        Assert.Empty(r.Rvi);
        Assert.Empty(r.Signal);
    }

    // ── Branch 5 — Flat OHLC — Value=0, Range=0 → Den=0 guard fires → RVI=0, Signal=0 ──
    [Fact]
    public void Compute_FlatOhlc_RviAndSignalAllZero()
    {
        var a = Enumerable.Repeat(100.0, 30).ToArray();
        var r = new RelativeVigorIndex(a, a, a, a, period: 5).Compute();
        Assert.NotEmpty(r.Rvi);
        Assert.NotEmpty(r.Signal);
        Assert.All(r.Rvi, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
        Assert.All(r.Signal, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 6 — Ranging market: Close == Open every bar, but H > L → RVI = 0 ──
    [Fact]
    public void Compute_PureRangingMarket_RviZero()
    {
        int n = 30;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            o[i] = 100; c[i] = 100; // Close == Open
            h[i] = 101; l[i] = 99;  // H > L
        }
        var r = new RelativeVigorIndex(o, h, l, c, period: 5).Compute();
        Assert.All(r.Rvi, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 7 — Sustained uptrend (Close > Open always) → RVI positive ──
    [Fact]
    public void Compute_Uptrend_RviPositive()
    {
        int n = 40;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            o[i] = 100 + i;
            c[i] = o[i] + 0.8;   // strong close above open
            h[i] = o[i] + 1.0;
            l[i] = o[i] - 0.2;
        }
        var r = new RelativeVigorIndex(o, h, l, c, period: 5).Compute();
        Assert.NotEmpty(r.Rvi);
        // Tail values should all be > 0 after smoothing settles.
        for (int i = r.Rvi.Length - 5; i < r.Rvi.Length; i++)
            Assert.True(r.Rvi[i] > 0, $"bar {i} RVI = {r.Rvi[i]}, expected > 0");
    }

    // ── Branch 8 — Sustained downtrend (Close < Open always) → RVI negative ──
    [Fact]
    public void Compute_Downtrend_RviNegative()
    {
        int n = 40;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            o[i] = 100 - i;
            c[i] = o[i] - 0.8;
            h[i] = o[i] + 0.2;
            l[i] = o[i] - 1.0;
        }
        var r = new RelativeVigorIndex(o, h, l, c, period: 5).Compute();
        Assert.NotEmpty(r.Rvi);
        for (int i = r.Rvi.Length - 5; i < r.Rvi.Length; i++)
            Assert.True(r.Rvi[i] < 0, $"bar {i} RVI = {r.Rvi[i]}, expected < 0");
    }

    // ── Branch 9 — Den == 0 guard: tested via Branch 5 (flat OHLC) + this targeted case ──
    [Fact]
    public void Compute_ZeroRangeWindow_GuardFiresNoNaN()
    {
        // Constant HL (zero range) everywhere, but Close drifts. DenSmooth stays 0.
        int n = 30;
        var h = Enumerable.Repeat(100.0, n).ToArray();
        var l = Enumerable.Repeat(100.0, n).ToArray();
        var o = Enumerable.Repeat(100.0, n).ToArray();
        var c = Enumerable.Range(0, n).Select(i => 100.0 + 0.01 * i).ToArray();
        var r = new RelativeVigorIndex(o, h, l, c, period: 5).Compute();
        // All Den = 0 → RVI forced to 0 by guard → Signal = 0.
        Assert.All(r.Rvi, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 10 — Normal multi-bar path — output bounded ──
    [Fact]
    public void Compute_RandomSeries_StaysFinite()
    {
        var rng = new Random(42);
        int n = 60;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + rng.NextDouble() * 10;
            o[i] = mid;
            c[i] = mid + (rng.NextDouble() - 0.5) * 2;
            h[i] = Math.Max(o[i], c[i]) + rng.NextDouble();
            l[i] = Math.Min(o[i], c[i]) - rng.NextDouble();
        }
        var r = new RelativeVigorIndex(o, h, l, c, period: 10).Compute();
        Assert.All(r.Rvi, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
        });
        Assert.All(r.Signal, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
        });
    }

    // ── Apply / label ──
    [Fact]
    public void Apply_AddsTwoSeries()
    {
        var axes = new Axes();
        var a = Enumerable.Repeat(100.0, 30).ToArray();
        new RelativeVigorIndex(a, a, a, a, period: 5).Apply(axes);
        Assert.Equal(2, axes.Series.Count);
    }

    [Fact]
    public void Apply_SetsExpectedLabels()
    {
        var axes = new Axes();
        var a = Enumerable.Repeat(100.0, 30).ToArray();
        new RelativeVigorIndex(a, a, a, a, period: 5).Apply(axes);
        Assert.Equal("RVI", axes.Series[0].Label);
        Assert.Equal("Signal", axes.Series[1].Label);
    }

    [Fact]
    public void ConstructorLabel_IncludesPeriod()
    {
        var a = Enumerable.Repeat(100.0, 10).ToArray();
        var r = new RelativeVigorIndex(a, a, a, a, period: 10);
        Assert.Equal("RVI(10)", r.Label);
    }

    [Fact]
    public void DefaultPeriod_IsTen()
    {
        var a = Enumerable.Repeat(100.0, 10).ToArray();
        var r = new RelativeVigorIndex(a, a, a, a);
        Assert.Equal("RVI(10)", r.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var a = Enumerable.Repeat(100.0, 10).ToArray();
        var r = new RelativeVigorIndex(a, a, a, a, period: 5);
        Assert.IsAssignableFrom<CandleIndicator<RviResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }

    // ── RviResult record struct — deconstruction works ──
    [Fact]
    public void RviResult_Deconstructs()
    {
        double[] rvi = [0.1, 0.2];
        double[] sig = [0.05, 0.15];
        var result = new RviResult(rvi, sig);
        var (a, b) = result;
        Assert.Same(rvi, a);
        Assert.Same(sig, b);
    }
}
