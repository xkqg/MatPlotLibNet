// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="YangZhang"/> behavior. Covers all branches enumerated in
/// docs/contrib/indicator-tier-1a.md §2: empty, below-period, exact-period+1 boundary,
/// non-positive guard, all-flat, period precondition (≥2), normal multi-window path,
/// and the k-formula boundary.</summary>
public class YangZhangTests
{
    // Reference vector — 6 bars with O_t = C_{t-1} (no overnight jump), H = C+2, L = C-2.
    //   Bar 0: O=100, H=104, L=98,  C=102
    //   Bar 1: O=103, H=107, L=101, C=105
    //   Bar 2: O=106, H=110, L=104, C=108
    //   Bar 3: O=109, H=113, L=107, C=111
    //   Bar 4: O=112, H=116, L=110, C=114
    //   Bar 5: O=115, H=119, L=113, C=117
    //
    // With period=5 → single-window output. Hand-computed components:
    //   σ²_O (sample variance of ln(O_t/C_{t-1})) ≈ 1.654e-7
    //   σ²_C (sample variance of ln(C_t/O_t))     ≈ 6.220e-7
    //   σ²_RS (mean of per-bar RS component)      ≈ 1.329e-3
    //   k = 0.34 / (1.34 + 6/4) = 0.1197183
    //   σ²_YZ ≈ σ²_O + k·σ²_C + (1-k)·σ²_RS ≈ 1.1703e-3
    //   σ_YZ = √(σ²_YZ) ≈ 0.03421
    private static readonly double[] RefO = [100, 103, 106, 109, 112, 115];
    private static readonly double[] RefH = [104, 107, 110, 113, 116, 119];
    private static readonly double[] RefL = [98, 101, 104, 107, 110, 113];
    private static readonly double[] RefC = [102, 105, 108, 111, 114, 117];

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var yz = new YangZhang([], [], [], [], period: 5);
        Assert.Empty(yz.Compute().Values);
    }

    // ── Branch 2 — Length <= period (strict: needs prevClose) ──
    [Fact]
    public void Compute_LengthEqualsPeriod_ReturnsEmpty()
    {
        // Exactly period bars — no bar available as prevClose for the first window bar
        var yz = new YangZhang(
            [100.0, 101, 102, 103, 104], [101.0, 102, 103, 104, 105],
            [99.0, 100, 101, 102, 103], [100.5, 101.5, 102.5, 103.5, 104.5], period: 5);
        Assert.Empty(yz.Compute().Values);
    }

    [Fact]
    public void Compute_LengthBelowPeriod_ReturnsEmpty()
    {
        var yz = new YangZhang(
            [100.0, 101], [101.0, 102], [99.0, 100], [100.5, 101.5], period: 5);
        Assert.Empty(yz.Compute().Values);
    }

    // ── Branch 3 — Length == period + 1 (boundary, returns length-1 array) ──
    [Fact]
    public void Compute_LengthEqualsPeriodPlusOne_ReturnsSingleWindow()
    {
        var yz = new YangZhang(RefO, RefH, RefL, RefC, period: 5);
        Assert.Single(yz.Compute().Values);
    }

    // ── Branch 4 — Non-positive price guard ──
    [Theory]
    [InlineData(0.0, 1.0, 1.0, 1.0)]
    [InlineData(1.0, 0.0, 1.0, 1.0)]
    [InlineData(1.0, 1.0, 0.0, 1.0)]
    [InlineData(1.0, 1.0, 1.0, 0.0)]
    [InlineData(-0.1, 1.0, 1.0, 1.0)]
    public void Constructor_NonPositivePrice_Throws(double o, double h, double l, double c)
    {
        Assert.Throws<ArgumentException>(() =>
            new YangZhang([o, 1, 1, 1, 1, 1], [h, 1, 1, 1, 1, 1], [l, 1, 1, 1, 1, 1], [c, 1, 1, 1, 1, 1], period: 5));
    }

    // ── Branch 5 — All-flat window → 0.0, no NaN ──
    [Fact]
    public void Compute_AllFlatWindow_ReturnsZero()
    {
        double[] flat = [100, 100, 100, 100, 100, 100, 100];
        var yz = new YangZhang(flat, flat, flat, flat, period: 5);
        var result = yz.Compute().Values;
        Assert.NotEmpty(result);
        Assert.All(result, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(0.0, v, precision: 12);
        });
    }

    // ── Branch 6 — Period precondition (must be ≥ 2 for k formula) ──
    [Fact]
    public void Constructor_PeriodBelowTwo_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new YangZhang(RefO, RefH, RefL, RefC, period: 1));
        Assert.Throws<ArgumentException>(() =>
            new YangZhang(RefO, RefH, RefL, RefC, period: 0));
    }

    // ── Branch 7 — Normal multi-window path + known reference value ──
    [Fact]
    public void Compute_KnownVector_ApproxMatchesHandComputed()
    {
        var yz = new YangZhang(RefO, RefH, RefL, RefC, period: 5);
        var result = yz.Compute().Values;
        Assert.Single(result);
        // Hand-computed σ_YZ ≈ 0.03421 (see class header comment for derivation).
        Assert.Equal(0.03421, result[0], precision: 3);
    }

    [Fact]
    public void Compute_MultiWindow_ReturnsCorrectLength()
    {
        // 8 bars, period 5 → 3 window outputs (8 - 5 = 3)
        double[] o = [100, 103, 106, 109, 112, 115, 118, 121];
        double[] h = [104, 107, 110, 113, 116, 119, 122, 125];
        double[] l = [98, 101, 104, 107, 110, 113, 116, 119];
        double[] c = [102, 105, 108, 111, 114, 117, 120, 123];
        var yz = new YangZhang(o, h, l, c, period: 5);
        var result = yz.Compute().Values;
        Assert.Equal(3, result.Length);
        Assert.All(result, v => Assert.False(double.IsNaN(v)));
    }

    // ── Branch 8 — k boundary at period = 2 ──
    [Fact]
    public void Compute_PeriodTwo_ComputesWithoutError()
    {
        // Period 2: k = 0.34 / (1.34 + 3) = 0.0783410
        double[] o = [100, 101, 102];
        double[] h = [101, 102, 103];
        double[] l = [99, 100, 101];
        double[] c = [100.5, 101.5, 102.5];
        var yz = new YangZhang(o, h, l, c, period: 2);
        var result = yz.Compute().Values;
        Assert.Single(result);
        Assert.False(double.IsNaN(result[0]));
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new YangZhang(RefO, RefH, RefL, RefC, period: 5).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new YangZhang(RefO, RefH, RefL, RefC, period: 5).Apply(axes);
        Assert.Equal("YZ(5)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_Is20()
    {
        var o = new double[25]; var h = new double[25]; var l = new double[25]; var c = new double[25];
        for (int i = 0; i < 25; i++) { o[i] = 100; h[i] = 101; l[i] = 99; c[i] = 100; }
        var yz = new YangZhang(o, h, l, c);
        Assert.Equal("YZ(20)", yz.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsCandleIndicator()
    {
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(
            new YangZhang(RefO, RefH, RefL, RefC));
        Assert.IsAssignableFrom<IIndicator>(
            new YangZhang(RefO, RefH, RefL, RefC));
    }
}
