// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="YangZhangVolRatio"/>. Covers all 9 branches in
/// docs/contrib/indicator-tier-3b.md §4.</summary>
public class YangZhangVolRatioTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new YangZhangVolRatio([], [], [], []);
        Assert.Empty(r.Compute().Values);
    }

    // ── Branch 2 — OHLC length mismatch → throw ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new YangZhangVolRatio([1.0, 2, 3], [1.0, 2], [1.0, 2, 3], [1.0, 2, 3]));
        Assert.Throws<ArgumentException>(() =>
            new YangZhangVolRatio([1.0, 2, 3], [1.0, 2, 3], [1.0, 2], [1.0, 2, 3]));
        Assert.Throws<ArgumentException>(() =>
            new YangZhangVolRatio([1.0, 2, 3], [1.0, 2, 3], [1.0, 2, 3], [1.0, 2]));
    }

    // ── Branch 3 — BarCount <= longWindow → empty ──
    [Fact]
    public void Compute_BarCountBelowLongWindow_ReturnsEmpty()
    {
        var flat = Flat(100, 30);
        var r = new YangZhangVolRatio(flat, flat, flat, flat, shortWindow: 10, longWindow: 40)
            .Compute().Values;
        Assert.Empty(r);
    }

    // ── Branch 4 — shortWindow < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_ShortWindowBelowTwo_Throws(int shortW)
    {
        var flat = Flat(100, 100);
        Assert.Throws<ArgumentException>(() =>
            new YangZhangVolRatio(flat, flat, flat, flat, shortWindow: shortW, longWindow: 20));
    }

    // ── Branch 5 — longWindow <= shortWindow → throw ──
    [Theory]
    [InlineData(20, 20)]
    [InlineData(20, 10)]
    public void Constructor_LongNotGreaterThanShort_Throws(int shortW, int longW)
    {
        var flat = Flat(100, 100);
        Assert.Throws<ArgumentException>(() =>
            new YangZhangVolRatio(flat, flat, flat, flat, shortWindow: shortW, longWindow: longW));
    }

    // ── Branch 6 — Constant prices → both YZ vols = 0 → ratio = 1 (guard) ──
    [Fact]
    public void Compute_ConstantPrices_RatioEqualsOne()
    {
        int n = 100;
        var flat = Flat(100, n);
        var r = new YangZhangVolRatio(flat, flat, flat, flat, shortWindow: 20, longWindow: 60)
            .Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.Equal(1.0, v, precision: 9));
    }

    // ── Branch 7 — Vol expansion → tail ratio > 1 ──
    // Transition at bar 110 so the 60-bar long window (ending at 149, covering bars 90..149)
    // straddles the regime shift (20 flat + 40 noisy), while the 20-bar short window is
    // entirely in the noisy tail. Short > long drives the ratio above 1.
    [Fact]
    public void Compute_VolExpansion_TailRatioAboveOne()
    {
        int n = 150;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        var rng = new Random(101);
        for (int i = 0; i < n; i++)
        {
            double baseP = 100.0;
            double noise = i < 110 ? 1e-4 : 3.0;
            double oPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double cPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double hi = Math.Max(oPx, cPx) + rng.NextDouble() * noise;
            double lo = Math.Min(oPx, cPx) - rng.NextDouble() * noise;
            o[i] = oPx; h[i] = hi; l[i] = lo; c[i] = cPx;
        }
        var r = new YangZhangVolRatio(o, h, l, c, shortWindow: 20, longWindow: 60).Compute().Values;
        Assert.True(r[^1] > 1.0, $"expected expansion tail > 1, got {r[^1]}");
    }

    // ── Branch 8 — Vol contraction → tail ratio < 1 ──
    // Mirror of the expansion scenario: the last 40 bars are flat so the short window
    // captures only the quiet tail while the long window still includes 20 noisy bars.
    [Fact]
    public void Compute_VolContraction_TailRatioBelowOne()
    {
        int n = 150;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        var rng = new Random(77);
        for (int i = 0; i < n; i++)
        {
            double baseP = 100.0;
            double noise = i < 110 ? 3.0 : 1e-4;
            double oPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double cPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double hi = Math.Max(oPx, cPx) + rng.NextDouble() * noise;
            double lo = Math.Min(oPx, cPx) - rng.NextDouble() * noise;
            o[i] = oPx; h[i] = hi; l[i] = lo; c[i] = cPx;
        }
        var r = new YangZhangVolRatio(o, h, l, c, shortWindow: 20, longWindow: 60).Compute().Values;
        Assert.True(r[^1] < 1.0, $"expected contraction tail < 1, got {r[^1]}");
    }

    // ── Branch 9 — Normal multi-bar path ──
    [Fact]
    public void Compute_MixedData_AllFinite()
    {
        int n = 120;
        var rng = new Random(9);
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + rng.NextDouble() * 2;
            o[i] = mid;
            c[i] = mid + (rng.NextDouble() - 0.5);
            h[i] = Math.Max(o[i], c[i]) + rng.NextDouble();
            l[i] = Math.Min(o[i], c[i]) - rng.NextDouble();
        }
        var r = new YangZhangVolRatio(o, h, l, c, shortWindow: 20, longWindow: 60).Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
            Assert.True(v > 0);
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        int n = 100;
        var flat = Flat(100.0, n);
        var axes = new Axes();
        new YangZhangVolRatio(flat, flat, flat, flat, shortWindow: 10, longWindow: 30).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_HasWindows()
    {
        var r = new YangZhangVolRatio([1.0, 2], [1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.Equal("YZRatio(20/60)", r.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var r = new YangZhangVolRatio([1.0, 2], [1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
