// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="EhlersITrend"/>. Covers all 7 branches in
/// docs/contrib/indicator-tier-3c.md §1.</summary>
public class EhlersITrendTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var it = new EhlersITrend([]);
        Assert.Empty(it.Compute().Values);
    }

    // ── Branch 2 — Length < 7 → empty (Hilbert warmup) ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    public void Compute_LengthBelowSeven_ReturnsEmpty(int n)
    {
        var prices = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var it = new EhlersITrend(prices);
        Assert.Empty(it.Compute().Values);
    }

    // ── Branch 3 — Length == 7 → boundary, single output ──
    [Fact]
    public void Compute_LengthSeven_ReturnsSingleValue()
    {
        var prices = new[] { 100.0, 101, 102, 103, 104, 105, 106 };
        var it = new EhlersITrend(prices).Compute().Values;
        Assert.Single(it);
        Assert.False(double.IsNaN(it[0]));
        Assert.False(double.IsInfinity(it[0]));
    }

    // ── Branch 4 — Flat prices → trend = flat value (weighted avg of constant = constant) ──
    [Fact]
    public void Compute_FlatPrices_ReturnsFlat()
    {
        var flat = Enumerable.Repeat(100.0, 50).ToArray();
        var it = new EhlersITrend(flat).Compute().Values;
        Assert.Equal(44, it.Length); // 50 - 6
        Assert.All(it, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(100.0, v, precision: 6);
        });
    }

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
        Assert.All(it, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
        });
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
        Assert.All(it, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new EhlersITrend(Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray()).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_IsITrend()
    {
        var it = new EhlersITrend([1.0, 2, 3, 4, 5, 6, 7]);
        Assert.Equal("iTrend", it.Label);
    }

    [Fact]
    public void InheritsPriceIndicator()
    {
        var it = new EhlersITrend([1.0, 2, 3, 4, 5, 6, 7]);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(it);
        Assert.IsAssignableFrom<IIndicator>(it);
    }
}
