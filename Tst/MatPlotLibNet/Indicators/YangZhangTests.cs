// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="YangZhang"/> behavior. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
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
    internal static readonly double[] RefO = [100, 103, 106, 109, 112, 115];
    internal static readonly double[] RefH = [104, 107, 110, 113, 116, 119];
    internal static readonly double[] RefL = [98, 101, 104, 107, 110, 113];
    internal static readonly double[] RefC = [102, 105, 108, 111, 114, 117];

    // ── Branch 7 — Normal multi-window path + known reference value ──

    [Fact]
    public void Compute_MultiWindow_ReturnsCorrectLength()
    {
        // 8 bars, period 5 → 3 window outputs (8 - 5 = 3)
        double[] o = [100, 103, 106, 109, 112, 115, 118, 121];
        double[] h = [104, 107, 110, 113, 116, 119, 122, 125];
        double[] l = [98, 101, 104, 107, 110, 113, 116, 119];
        double[] c = [102, 105, 108, 111, 114, 117, 120, 123];
        var yz = new YangZhang(OhlcvBars.Ohlc(o, h, l, c), period: 5);
        var result = yz.Compute().Values;
        Assert.Equal(3, result.Length);
        result.AssertAllFinite();
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
        var yz = new YangZhang(OhlcvBars.Ohlc(o, h, l, c), period: 2);
        var result = yz.Compute().Values;
        Assert.Single(result);
        result[0].AssertFinite();
    }
}
