// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="RollSpread"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class RollSpreadTests
{
    // ── Branch 5 — Non-positive price NOT thrown (spec: raw differences, not logs) ──
    [Fact]
    public void Constructor_NonPositivePrice_DoesNotThrow()
    {
        // Raw diffs don't require positive prices; spec says just document.
        var ex = Record.Exception(() =>
            new RollSpread([-1.0, 0, 1, 2, 3, 4], period: 2));
        Assert.Null(ex);
    }

    // ── Branch 7 — cov >= 0 (positive autocorrelation, monotonic rise) → S = 0 ──
    [Fact]
    public void Compute_MonotonicRise_ReturnsZero()
    {
        var roll = new RollSpread(
            [100.0, 101, 102, 103, 104, 105, 106, 107], period: 4);
        var r = roll.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 8 — cov < 0 (bid-ask bounce) → S = 2√(−cov) ──
    [Fact]
    public void Compute_BidAskBounce_ReturnsTwiceSpread()
    {
        // Perfect bounce 100.0 / 100.2 gives pop-cov = −0.0384 → S ≈ 0.3919.
        // Expected Roll-spread value approaches 2·s = 0.4 as window size grows; for
        // period=4 (5 Δp values), finite-sample bias yields 0.3919.
        var roll = new RollSpread(
            [100.0, 100.2, 100.0, 100.2, 100.0, 100.2], period: 4);
        var r = roll.Compute().Values;
        Assert.Single(r);
        Assert.Equal(0.39192, r[0], precision: 4);
    }

    // ── Branch 9 — Normal multi-bar output ──
    [Fact]
    public void Compute_RollingOutput_ReturnsCorrectLength()
    {
        // n = 10, period = 3 → output length = 10 - 3 - 1 = 6
        var roll = new RollSpread(
            [100.0, 100.2, 100.0, 100.2, 100.0, 100.2, 100.0, 100.2, 100.0, 100.2],
            period: 3);
        var r = roll.Compute().Values;
        Assert.Equal(6, r.Length);
        r.AssertAllFinite();
    }
}
