// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="EhlersITrend"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class EhlersITrendTests
{
    // ── Branch 5 — Pure sinusoid → trend smooths toward centre line ──
    [Fact]
    public void Compute_Sinusoid_SmoothsTowardCentre()
    {
        int n = 300;
        var prices = new double[n];
        for (int i = 0; i < n; i++) prices[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 15);
        var it = new EhlersITrend(prices).Compute().Values;

        // Well into the series, the adaptive trend should hover near 100 — the cycle centre.
        double maxDeviation = 0;
        for (int i = 200; i < it.Length; i++)
            maxDeviation = Math.Max(maxDeviation, Math.Abs(it[i] - 100.0));
        // Well under the 10-unit sinusoid amplitude.
        Assert.True(maxDeviation < 5.0, $"expected smoothing near 100, got maxDeviation={maxDeviation}");
    }

    // ── Branch 6 — Very short post-warmup path (period not yet climbed) → fallback keeps finite output ──
    [Fact]
    public void Compute_EarlyBars_FallbackPreventsNaN()
    {
        // Length 8 triggers the i==6 and i==7 loop iterations where period is still settling
        // toward the >=6 floor. The < 2 fallback arm must keep output finite.
        var prices = new[] { 1.0, 2, 3, 4, 5, 6, 7, 8 };
        var it = new EhlersITrend(prices).Compute().Values;
        Assert.Equal(2, it.Length);
        it.AssertAllFinite();
    }

    // ── Branch 7 — Normal multi-bar path, known vector → all finite ──
    [Fact]
    public void Compute_KnownVector_AllFinite()
    {
        var prices = new[] {
            1.0, 2, 3, 5, 4, 6, 8, 7, 9, 11, 10, 12, 14, 13, 15, 17, 16, 18, 20, 19
        };
        var it = new EhlersITrend(prices).Compute().Values;
        Assert.Equal(14, it.Length); // 20 - 6
        it.AssertAllFinite();
    }
}
