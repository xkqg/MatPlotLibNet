// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="WaveletEnergyRatio"/>. Covers all 10 branches in
/// docs/contrib/indicator-tier-2b.md §2.</summary>
public class WaveletEnergyRatioTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var w = new WaveletEnergyRatio([], window: 4, level: 0);
        Assert.Empty(w.Compute().Values);
    }

    // ── Branch 2 — window not power of 2 throws ──
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(9)]
    public void Constructor_NonPowerOfTwoWindow_Throws(int window)
    {
        Assert.Throws<ArgumentException>(() =>
            new WaveletEnergyRatio([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10], window, level: 0));
    }

    // ── Branch 3 — window < 4 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_WindowBelowFour_Throws(int window)
    {
        Assert.Throws<ArgumentException>(() =>
            new WaveletEnergyRatio([1.0, 2, 3, 4], window, level: 0));
    }

    // ── Branch 4 — level < 0 throws ──
    [Fact]
    public void Constructor_NegativeLevel_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new WaveletEnergyRatio([1.0, 2, 3, 4], window: 4, level: -1));
    }

    // ── Branch 5 — level >= log2(window) throws ──
    [Theory]
    [InlineData(4, 2)]
    [InlineData(8, 3)]
    [InlineData(64, 6)]
    public void Constructor_LevelAtOrAboveLog2Window_Throws(int window, int level)
    {
        Assert.Throws<ArgumentException>(() =>
            new WaveletEnergyRatio(new double[64], window, level));
    }

    // ── Branch 6 — prices.Length < window → empty ──
    [Fact]
    public void Compute_LengthBelowWindow_ReturnsEmpty()
    {
        var w = new WaveletEnergyRatio([1.0, 2, 3], window: 4, level: 0);
        Assert.Empty(w.Compute().Values);
    }

    // ── Branch 7 — prices.Length == window → single output ──
    [Fact]
    public void Compute_LengthEqualsWindow_ReturnsSingleValue()
    {
        var w = new WaveletEnergyRatio([1.0, 2, 3, 4], window: 4, level: 0);
        Assert.Single(w.Compute().Values);
    }

    // ── Branch 8 — Constant signal → 0 (all detail energies 0; approx energy excluded from output level) ──
    [Fact]
    public void Compute_ConstantSignal_ReturnsZero()
    {
        var prices = Enumerable.Repeat(100.0, 100).ToArray();
        var w = new WaveletEnergyRatio(prices, window: 64, level: 0).Compute().Values;
        Assert.NotEmpty(w);
        Assert.All(w, v =>
        {
            Assert.Equal(0.0, v, precision: 10);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 8b — All-zero signal → total energy = 0 → guard returns 0 (no NaN) ──
    [Fact]
    public void Compute_AllZeroSignal_ReturnsZero()
    {
        var prices = new double[100]; // all zeros
        var w = new WaveletEnergyRatio(prices, window: 64, level: 0).Compute().Values;
        Assert.NotEmpty(w);
        Assert.All(w, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 9 — Alternating ±1 concentrates energy at level 0; other levels near 0 ──
    [Fact]
    public void Compute_AlternatingSignal_LevelZeroDominates()
    {
        var alt = new double[100];
        for (int i = 0; i < 100; i++) alt[i] = (i % 2 == 0) ? 1.0 : -1.0;

        var w0 = new WaveletEnergyRatio(alt, window: 64, level: 0).Compute().Values;
        Assert.True(w0[^1] > 0.95, $"expected level-0 ratio > 0.95, got {w0[^1]}");

        var w1 = new WaveletEnergyRatio(alt, window: 64, level: 1).Compute().Values;
        Assert.True(w1[^1] < 0.05, $"expected level-1 ratio < 0.05, got {w1[^1]}");
    }

    // ── Branch 10 — Normal multi-window path ──
    [Fact]
    public void Compute_NoisyWindow_StaysInZeroOneRange()
    {
        var rng = new Random(42);
        var prices = new double[100];
        for (int i = 0; i < 100; i++) prices[i] = rng.NextDouble();
        var w = new WaveletEnergyRatio(prices, window: 16, level: 0).Compute().Values;
        Assert.Equal(85, w.Length); // 100 - 16 + 1
        Assert.All(w, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 0.0, 1.0);
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        new WaveletEnergyRatio(prices, window: 16, level: 0).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToOne()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        new WaveletEnergyRatio(prices, window: 16, level: 0).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        new WaveletEnergyRatio(prices, window: 16, level: 2).Apply(axes);
        Assert.Equal("WER(W=16,L=2)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultWindowAndLevel()
    {
        var prices = new double[100];
        for (int i = 0; i < 100; i++) prices[i] = i;
        var w = new WaveletEnergyRatio(prices);
        Assert.Equal("WER(W=64,L=0)", w.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var w = new WaveletEnergyRatio([1.0, 2, 3, 4], window: 4, level: 0);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(w);
        Assert.IsAssignableFrom<IIndicator>(w);
    }
}
