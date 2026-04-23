// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="TwiggsMoneyFlow"/>. Covers all 9 branches in
/// docs/contrib/indicator-tier-3a.md §2.</summary>
public class TwiggsMoneyFlowTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Ramp(double start, double step, int n)
        => Enumerable.Range(0, n).Select(i => start + step * i).ToArray();

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var tmf = new TwiggsMoneyFlow([], [], [], []);
        Assert.Empty(tmf.Compute().Values);
    }

    // ── Branch 2 — HLCV length mismatch → throw ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TwiggsMoneyFlow([1.0, 2], [1.0, 2, 3], [1.0, 2], [1.0, 2]));
    }

    // ── Branch 3 — BarCount < 2 → empty ──
    [Fact]
    public void Compute_SingleBar_ReturnsEmpty()
    {
        var tmf = new TwiggsMoneyFlow([100.0], [99.0], [99.5], [1000.0]);
        Assert.Empty(tmf.Compute().Values);
    }

    // ── Branch 4 — period < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_PeriodBelowTwo_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() =>
            new TwiggsMoneyFlow(Flat(100, 5), Flat(100, 5), Flat(100, 5), Flat(1000, 5), period));
    }

    // ── Branch 5 — Flat prices + positive volume → TR = 0 → AD = 0 → TMF = 0 ──
    [Fact]
    public void Compute_FlatPricesPositiveVolume_ReturnsZero()
    {
        int n = 50;
        var tmf = new TwiggsMoneyFlow(Flat(100, n), Flat(100, n), Flat(100, n), Flat(1000, n), period: 10)
            .Compute().Values;
        Assert.Equal(n - 1, tmf.Length);
        Assert.All(tmf, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 6 — Flat prices + zero volume → EMAs both 0 → TMF = 0 via guard ──
    [Fact]
    public void Compute_ZeroVolume_NoDivisionByZero()
    {
        int n = 30;
        var tmf = new TwiggsMoneyFlow(Flat(100, n), Flat(100, n), Flat(100, n), Flat(0, n), period: 10)
            .Compute().Values;
        Assert.Equal(n - 1, tmf.Length);
        Assert.All(tmf, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
            Assert.Equal(0.0, v, precision: 10);
        });
    }

    // ── Branch 7 — Pure uptrend (C == TH each bar) → TMF → +1 ──
    [Fact]
    public void Compute_CloseAtTrueHigh_ConvergesTowardPlusOne()
    {
        int n = 100;
        var high = Ramp(102, 1, n);
        var low = Ramp(98, 1, n);
        var close = Ramp(102, 1, n); // C == H → (2C - TH - TL) / TR = (2C - max(H,pC) - min(L,pC)) / TR → (C - pC) / TR-ish
        // After warmup, TMF should trend strongly positive.
        var tmf = new TwiggsMoneyFlow(high, low, close, Flat(1000, n), period: 10).Compute().Values;
        Assert.True(tmf[^1] > 0.5, $"expected tail TMF > 0.5 on close-at-TH, got {tmf[^1]}");
    }

    // ── Branch 8 — Pure downtrend (C == TL each bar) → TMF → -1 ──
    [Fact]
    public void Compute_CloseAtTrueLow_ConvergesTowardMinusOne()
    {
        int n = 100;
        // Downtrend: each bar's HLC lower than the previous bar's close.
        var high = Enumerable.Range(0, n).Select(i => 200.0 - i).ToArray();
        var low = Enumerable.Range(0, n).Select(i => 196.0 - i).ToArray();
        var close = low;                                                       // C at low extreme
        var tmf = new TwiggsMoneyFlow(high, low, close, Flat(1000, n), period: 10).Compute().Values;
        Assert.True(tmf[^1] < -0.5, $"expected tail TMF < -0.5 on close-at-TL, got {tmf[^1]}");
    }

    // ── Branch 9 — Normal multi-bar path, finite output ──
    [Fact]
    public void Compute_MixedData_AllFinite()
    {
        int n = 50;
        var rng = new Random(42);
        var h = new double[n]; var l = new double[n]; var c = new double[n]; var v = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + i + rng.NextDouble();
            h[i] = mid + 1;
            l[i] = mid - 1;
            c[i] = mid + rng.NextDouble() - 0.5;
            v[i] = 1000 + rng.NextDouble() * 500;
        }
        var tmf = new TwiggsMoneyFlow(h, l, c, v, period: 10).Compute().Values;
        Assert.Equal(n - 1, tmf.Length);
        Assert.All(tmf, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
            Assert.InRange(x, -1.0, 1.0);
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeriesAndSetsYRange()
    {
        var axes = new Axes();
        int n = 30;
        new TwiggsMoneyFlow(Ramp(101, 1, n), Ramp(99, 1, n), Ramp(100, 1, n), Flat(1000, n)).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
        Assert.Equal(-1, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void DefaultLabel_HasPeriod()
    {
        var t = new TwiggsMoneyFlow([1.0, 2], [1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.Equal("TMF(21)", t.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var t = new TwiggsMoneyFlow([1.0, 2], [1.0, 2], [1.0, 2], [1.0, 2]);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(t);
        Assert.IsAssignableFrom<IIndicator>(t);
    }
}
