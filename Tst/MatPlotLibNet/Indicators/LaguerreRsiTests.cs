// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="LaguerreRsi"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class LaguerreRsiTests
{
    // ── Branch 6 — alpha = 0.5 edge (no special handling, sanity check) ──
    [Fact]
    public void Compute_AlphaHalf_ProducesValidOutput()
    {
        var r = new LaguerreRsi(
            Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray(),
            alpha: 0.5);
        var vals = r.Compute().Values;
        Assert.All(vals, v => Assert.InRange(v, 0.0, 1.0));
    }

    // ── Branch 8 — Monotonic rising → output approaches 1.0 ──
    [Fact]
    public void Compute_MonotonicRising_ApproachesOne()
    {
        var r = new LaguerreRsi(
            Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray(),
            alpha: 0.2);
        var vals = r.Compute().Values;
        // After ≥20 bars of sustained rise, Laguerre RSI should be very close to 1.
        Assert.InRange(vals[25], 0.95, 1.0001);
    }

    // ── Branch 9 — Monotonic falling → output approaches 0.0 ──
    [Fact]
    public void Compute_MonotonicFalling_ApproachesZero()
    {
        var r = new LaguerreRsi(
            Enumerable.Range(0, 30).Select(i => 130.0 - i).ToArray(),
            alpha: 0.2);
        var vals = r.Compute().Values;
        Assert.InRange(vals[25], 0.0, 0.05);
    }

    // Known vector — Python reference (see spec §3):
    //   prices = [100, 101, 100, 101, 100], alpha=0.2
    // Step-by-step (all L_k init to 100):
    //   i=1: L0=100.2, L1=99.84, L2=100.128, L3=99.8976
    //        CU = 0.36 + 0 + 0.2304 = 0.5904
    //        CD = 0 + 0.288 + 0     = 0.288
    //        RSI = 0.5904 / 0.8784 ≈ 0.67213
    [Fact]
    public void Compute_KnownVector_MatchesReference()
    {
        var r = new LaguerreRsi([100.0, 101, 100, 101, 100], alpha: 0.2);
        var vals = r.Compute().Values;
        Assert.Equal(4, vals.Length);
        Assert.Equal(0.67213, vals[0], precision: 4);
    }
}
