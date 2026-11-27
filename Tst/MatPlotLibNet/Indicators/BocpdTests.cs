// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Bocpd"/>. Its arithmetic is held here against reference values; what it keeps with every indicator (its
/// lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class BocpdTests
{
    // ── Branch 9 — Strong breakpoint → spike above 0.5 ──
    [Fact]
    public void Compute_StrongBreak_ProducesSpike()
    {
        var prices = new double[40];
        for (int i = 0; i < 20; i++) prices[i] = 100.0;
        for (int i = 20; i < 40; i++) prices[i] = 200.0;

        var r = new Bocpd(prices, hazard: 0.01).Compute().Values;
        Assert.Equal(39, r.Length);
        // The break is at bar 20 → output index 19 (P(r_t=0) at t=20).
        Assert.True(r[19] > 0.5, $"expected changepoint spike > 0.5 at index 19, got {r[19]}");
    }

    // ── Branch 11 — Run-length truncation at maxRunLength ──
    [Fact]
    public void Compute_LongSeries_WithSmallMaxRunLength_DoesNotThrow()
    {
        var prices = Enumerable.Repeat(100.0, 100).ToArray();
        // maxRunLength = 5 → distribution is truncated; no NaN / no overflow.
        var r = new Bocpd(prices, hazard: 0.01, maxRunLength: 5).Compute().Values;
        Assert.Equal(99, r.Length);
        r.AssertAllFinite();
    }
}
