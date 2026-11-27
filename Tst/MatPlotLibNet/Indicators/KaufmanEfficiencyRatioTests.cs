// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="KaufmanEfficiencyRatio"/> behavior. Its arithmetic is held here against reference values; what it keeps
/// with every indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see
/// cref="IndicatorContractTests"/>.</summary>
public class KaufmanEfficiencyRatioTests
{
    // ── Branch 5 — A monotonic move, rising or falling → ER = 1.0: every bar moves the same way ──
    [Theory]
    [InlineData(new double[] { 100, 101, 102, 103, 104, 105 })]
    [InlineData(new double[] { 105, 104, 103, 102, 101, 100 })]
    public void Compute_MonotonicMove_ReturnsOne(double[] prices)
    {
        var result = new KaufmanEfficiencyRatio(prices, period: 5).Compute().Values;
        Assert.Single(result);
        Assert.Equal(1.0, result[0], precision: 10);
    }

    // ── Branch 6 — Zigzag (|ΔC| constant) → ER is small ──
    [Fact]
    public void Compute_Zigzag_ReturnsFifthOfTotal()
    {
        // [100,101,100,101,100,101], period=5
        //   change = |101 − 100| = 1
        //   volatility = 1 + 1 + 1 + 1 + 1 = 5
        //   ER = 0.2
        double[] prices = [100, 101, 100, 101, 100, 101];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Single(result);
        Assert.Equal(0.2, result[0], precision: 10);
    }

    [Fact]
    public void Compute_PureZigzag_NetZero_ReturnsZero()
    {
        // [100,101,100,101,100,101,100], period=6: net change = 0, volatility = 6
        // → ER = 0
        double[] prices = [100, 101, 100, 101, 100, 101, 100];
        var er = new KaufmanEfficiencyRatio(prices, period: 6);
        var result = er.Compute().Values;
        Assert.Single(result);
        Assert.Equal(0.0, result[0], precision: 10);
    }

    // ── Branch 7 — Normal multi-bar output ──
    [Fact]
    public void Compute_MultiBarOutput_ReturnsCorrectLength()
    {
        // 10 prices, period 5 → 10 - 5 = 5 outputs
        double[] prices = [100, 101, 102, 103, 104, 105, 106, 107, 108, 109];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Equal(5, result.Length);
        Assert.All(result, v => Assert.InRange(v, 0.0, 1.0));
    }

    [Fact]
    public void Compute_KnownVector_RollingMixedSignal()
    {
        // Build a known sequence: 5 up, 1 reversal, 4 up.
        // [100, 101, 102, 103, 104, 105, 100, 101, 102, 103, 104]
        // period = 5.
        // Output at t=5: window = idx 1..5 = [101,102,103,104,105]; change = |105-100|=5;
        //   vol sums diffs over idx 1..5 = |1|+|1|+|1|+|1|+|1| = 5 → ER = 1.0
        // Output at t=6: window idx 2..6 = [102..100]; change = |100-101|=1;
        //   vol = |1|+|1|+|1|+|1|+|5| = 9 → ER ≈ 0.111
        double[] prices = [100, 101, 102, 103, 104, 105, 100, 101, 102, 103, 104];
        var er = new KaufmanEfficiencyRatio(prices, period: 5);
        var result = er.Compute().Values;
        Assert.Equal(6, result.Length); // 11 - 5 = 6
        Assert.Equal(1.0, result[0], precision: 10);
        Assert.Equal(1.0 / 9.0, result[1], precision: 10);
    }
}
