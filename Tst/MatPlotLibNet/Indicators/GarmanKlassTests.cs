// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="GarmanKlass"/> behavior. Covers all branches enumerated in
/// docs/contrib/indicator-tier-1a.md §1: empty, below-period, exact-period boundary,
/// non-positive guard, H==L zero-range, doji O==C, all-flat, single-window, rolling.</summary>
public class GarmanKlassTests
{
    // Canonical 5-bar vector from spec §1 test vectors:
    private static readonly double[] RefO = [100, 102, 103, 105, 107];
    private static readonly double[] RefH = [105, 104, 106, 108, 109];
    private static readonly double[] RefL = [99, 100, 102, 104, 106];
    private static readonly double[] RefC = [102, 103, 105, 107, 108];
    //
    // Python reference:
    //   import numpy as np
    //   np.sqrt(np.mean(0.5*np.log(h/l)**2 - (2*np.log(2)-1)*np.log(c/o)**2)) ≈ 0.0277119
    // Hand-verified: Σ σ²_GK = 0.00383972, mean = 0.000767943, √ = 0.0277119

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var gk = new GarmanKlass([], [], [], [], period: 5);
        Assert.Empty(gk.Compute().Values);
    }

    // ── Branch 2 — Length < period ──
    [Fact]
    public void Compute_LengthBelowPeriod_ReturnsEmpty()
    {
        var gk = new GarmanKlass(
            [100.0, 101.0, 102.0], [101.0, 102.0, 103.0],
            [99.0, 100.0, 101.0], [100.5, 101.5, 102.5], period: 5);
        Assert.Empty(gk.Compute().Values);
    }

    // ── Branch 3 — Length == period (boundary, returns length-1 array) ──
    [Fact]
    public void Compute_LengthEqualsPeriod_ReturnsSingleWindow()
    {
        var gk = new GarmanKlass(RefO, RefH, RefL, RefC, period: 5);
        Assert.Single(gk.Compute().Values);
    }

    // ── Branch 4 — Non-positive price guard ──
    [Theory]
    [InlineData(0.0, 1.0, 1.0, 1.0)]    // O <= 0
    [InlineData(1.0, 0.0, 1.0, 1.0)]    // H <= 0
    [InlineData(1.0, 1.0, 0.0, 1.0)]    // L <= 0
    [InlineData(1.0, 1.0, 1.0, 0.0)]    // C <= 0
    [InlineData(-1.0, 1.0, 1.0, 1.0)]   // O negative
    public void Constructor_NonPositivePrice_Throws(double o, double h, double l, double c)
    {
        Assert.Throws<ArgumentException>(() =>
            new GarmanKlass([o, 1, 1, 1, 1], [h, 1, 1, 1, 1], [l, 1, 1, 1, 1], [c, 1, 1, 1, 1], period: 5));
    }

    // ── Branch 5 — H == L (zero range) contributes 0, no NaN ──
    [Fact]
    public void Compute_ZeroRangeBar_NoNaN()
    {
        // Bar 0: H==L so ln(H/L)==0; full window has 5 bars but bar 0 is H==L
        double[] o = [100, 100, 100, 100, 100];
        double[] h = [100, 101, 102, 103, 104];   // bar 0: H == L
        double[] l = [100, 99, 98, 97, 96];       // bar 0: H == L == 100
        double[] c = [100, 100, 100, 100, 100];
        var result = new GarmanKlass(o, h, l, c, period: 5).Compute().Values;
        Assert.Single(result);
        Assert.False(double.IsNaN(result[0]));
    }

    // ── Branch 6 — O == C (doji) contributes 0, no NaN ──
    [Fact]
    public void Compute_DojiBar_NoNaN()
    {
        double[] o = [100, 100, 100, 100, 100];
        double[] h = [101, 102, 103, 104, 105];
        double[] l = [99, 98, 97, 96, 95];
        double[] c = [100, 100, 100, 100, 100]; // O == C every bar
        var result = new GarmanKlass(o, h, l, c, period: 5).Compute().Values;
        Assert.Single(result);
        Assert.False(double.IsNaN(result[0]));
        Assert.True(result[0] > 0); // HL range non-zero → σ > 0
    }

    // ── Branch 7 — All-flat window (H=L=O=C) → 0.0, no NaN ──
    [Fact]
    public void Compute_AllFlatWindow_ReturnsZero()
    {
        double[] flat = [100, 100, 100, 100, 100, 100, 100];
        var gk = new GarmanKlass(flat, flat, flat, flat, period: 5);
        var result = gk.Compute().Values;
        Assert.All(result, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(0.0, v, precision: 12);
        });
    }

    // ── Branch 8 — Normal single-window path ──
    [Fact]
    public void Compute_KnownVector_MatchesPythonReference()
    {
        var gk = new GarmanKlass(RefO, RefH, RefL, RefC, period: 5);
        var result = gk.Compute().Values;
        Assert.Single(result);
        Assert.Equal(0.0277119, result[0], precision: 5);
    }

    // ── Branch 9 — Rolling path (length > period → multiple window outputs) ──
    [Fact]
    public void Compute_RollingWindow_ReturnsMultipleValues()
    {
        // 7 bars, period 5 → 3 window outputs
        double[] o = [100, 102, 103, 105, 107, 108, 110];
        double[] h = [105, 104, 106, 108, 109, 111, 112];
        double[] l = [99, 100, 102, 104, 106, 107, 109];
        double[] c = [102, 103, 105, 107, 108, 110, 111];
        var result = new GarmanKlass(o, h, l, c, period: 5).Compute().Values;
        Assert.Equal(3, result.Length);
        Assert.All(result, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.True(v > 0);
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new GarmanKlass(RefO, RefH, RefL, RefC, period: 5).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new GarmanKlass(RefO, RefH, RefL, RefC, period: 7).Apply(axes);
        Assert.Equal("GK(7)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_Is20()
    {
        // Constructor default applied via label
        var o = new double[25]; var h = new double[25]; var l = new double[25]; var c = new double[25];
        for (int i = 0; i < 25; i++) { o[i] = 100; h[i] = 101; l[i] = 99; c[i] = 100; }
        var gk = new GarmanKlass(o, h, l, c);
        Assert.Equal("GK(20)", gk.Label);
    }

    // ── Type-system: inherits CandleIndicator<SignalResult> ──
    [Fact]
    public void InheritsCandleIndicator()
    {
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(
            new GarmanKlass(RefO, RefH, RefL, RefC));
        Assert.IsAssignableFrom<IIndicator>(
            new GarmanKlass(RefO, RefH, RefL, RefC));
    }
}
