// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="GarmanKlass"/> behavior. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
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

    // ── Branch 5 — H == L (zero range) contributes 0, no NaN ──
    [Fact]
    public void Compute_ZeroRangeBar_NoNaN()
    {
        // Bar 0: H==L so ln(H/L)==0; full window has 5 bars but bar 0 is H==L
        double[] o = [100, 100, 100, 100, 100];
        double[] h = [100, 101, 102, 103, 104];   // bar 0: H == L
        double[] l = [100, 99, 98, 97, 96];       // bar 0: H == L == 100
        double[] c = [100, 100, 100, 100, 100];
        var result = new GarmanKlass(OhlcvBars.Ohlc(o, h, l, c), period: 5).Compute().Values;
        Assert.Single(result);
        result[0].AssertFinite();
    }

    // ── Branch 6 — O == C (doji) contributes 0, no NaN ──
    [Fact]
    public void Compute_DojiBar_NoNaN()
    {
        double[] o = [100, 100, 100, 100, 100];
        double[] h = [101, 102, 103, 104, 105];
        double[] l = [99, 98, 97, 96, 95];
        double[] c = [100, 100, 100, 100, 100]; // O == C every bar
        var result = new GarmanKlass(OhlcvBars.Ohlc(o, h, l, c), period: 5).Compute().Values;
        Assert.Single(result);
        result[0].AssertFinite();
        Assert.True(result[0] > 0); // HL range non-zero → σ > 0
    }

    // ── Branch 7 — All-flat window (H=L=O=C) → 0.0, no NaN: the Garman-Klass and the Yang-Zhang estimator alike ──
    public static TheoryData<Func<OhlcvBars, double[]>> Estimators => new()
    {
        bars => new GarmanKlass(bars, period: 5).Compute().Values,
        bars => new YangZhang(bars, period: 5).Compute().Values,
    };

    [Theory]
    [MemberData(nameof(Estimators))]
    public void Compute_AllFlatWindow_ReturnsZero(Func<OhlcvBars, double[]> estimate) =>
        EdgeCaseData.AssertFlatBarsHaveNoVolatility(estimate);

    // ── Branch 8 — Normal single-window path ──
    /// <summary>The two volatility estimators over the same kind of reference vector (GarmanKlass: the canonical 5-bar vector, checked
    /// against numpy; YangZhang: 6 bars with O_t = C_(t-1), hand-computed, see its class header).</summary>
    public static TheoryData<Func<OhlcvBars, double[]>, OhlcvBars, double, int> KnownVectors => new()
    {
        { bars => new GarmanKlass(bars, period: 5).Compute().Values, OhlcvBars.Ohlc(RefO, RefH, RefL, RefC), 0.0277119, 5 },
        { bars => new YangZhang(bars, period: 5).Compute().Values, OhlcvBars.Ohlc(YangZhangTests.RefO, YangZhangTests.RefH, YangZhangTests.RefL, YangZhangTests.RefC), 0.03421, 3 },
    };

    [Theory]
    [MemberData(nameof(KnownVectors))]
    public void Compute_KnownVector_MatchesTheReference(Func<OhlcvBars, double[]> estimate, OhlcvBars bars, double expected, int precision) =>
        EdgeCaseData.AssertOneValue(estimate(bars), expected, precision);

    // ── Branch 9 — Rolling path (length > period → multiple window outputs) ──
    [Fact]
    public void Compute_RollingWindow_ReturnsMultipleValues()
    {
        // 7 bars, period 5 → 3 window outputs
        double[] o = [100, 102, 103, 105, 107, 108, 110];
        double[] h = [105, 104, 106, 108, 109, 111, 112];
        double[] l = [99, 100, 102, 104, 106, 107, 109];
        double[] c = [102, 103, 105, 107, 108, 110, 111];
        var result = new GarmanKlass(OhlcvBars.Ohlc(o, h, l, c), period: 5).Compute().Values;
        Assert.Equal(3, result.Length);
        result.AssertAllFinite();
        Assert.All(result, v => Assert.True(v > 0));
    }
}
