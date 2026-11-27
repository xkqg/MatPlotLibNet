// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="AdaptiveStochastic"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class AdaptiveStochasticTests
{
    // ── Branch 6 — Non-positive price is NOT a throw (range ratio doesn't need logs) ──
    [Fact]
    public void Constructor_NonPositivePrice_DoesNotThrow()
    {
        // Stochastic uses raw HLC ranges, not logs. Negative prices are weird but
        // don't crash the algorithm.
        var ex = Record.Exception(() =>
            new AdaptiveStochastic(OhlcvBars.Hlc([1.0, 2, 3, 4, 5, 6, 7], [-1.0, 0, 1, 2, 3, 4, 5], [0.0, 1, 2, 3, 4, 5, 6])));
        Assert.Null(ex);
    }

    // ── Branch 8 — Constant prices (H > L but close doesn't move within window) → near 50 ──
    [Fact]
    public void Compute_ConstantRanges_NearFifty()
    {
        // H = 101, L = 99, C = 100 constant. Range is fixed, close always at midpoint.
        var h = Enumerable.Repeat(101.0, 30).ToArray();
        var l = Enumerable.Repeat(99.0, 30).ToArray();
        var c = Enumerable.Repeat(100.0, 30).ToArray();
        var r = new AdaptiveStochastic(OhlcvBars.Hlc(h, l, c)).Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.InRange(v, 40.0, 60.0));
    }

    // ── Branch 9 & 10 — Adaptive lookback hit at lower / upper clamps ──
    [Fact]
    public void Compute_LongSeries_OutputBoundedZeroToHundred()
    {
        int n = 300;
        var rng = new Random(42);
        var c = new double[n]; c[0] = 100;
        for (int i = 1; i < n; i++)
            c[i] = c[i - 1] * (1 + (rng.NextDouble() - 0.5) * 0.02);
        var h = c.Select(v => v + 1).ToArray();
        var l = c.Select(v => v - 1).ToArray();

        var r = new AdaptiveStochastic(OhlcvBars.Hlc(h, l, c)).Compute().Values;
        Assert.All(r, v => Assert.InRange(v, 0.0, 100.0));
    }

    // ── Branch 11 — Normal multi-bar path — output at top of rising range = 100 ──
    [Fact]
    public void Compute_Uptrend_OutputNearHundredAtPeak()
    {
        // Strong monotonic uptrend — close always near the high of its window → %K near 100.
        var c = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        var r = new AdaptiveStochastic(OhlcvBars.Hlc(h, l, c), smoothingPeriod: 1).Compute().Values;
        Assert.True(r[^1] > 80, $"expected %K > 80 in strong uptrend, got {r[^1]}");
    }
}
