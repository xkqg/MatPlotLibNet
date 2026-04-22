// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="WaveletEntropy"/>. Covers all 9 branches in
/// docs/contrib/indicator-tier-2b.md §3.</summary>
public class WaveletEntropyTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var w = new WaveletEntropy([]);
        Assert.Empty(w.Compute().Values);
    }

    // ── Branch 2 — window not power of 2 throws ──
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(9)]
    public void Constructor_NonPowerOfTwoWindow_Throws(int window)
    {
        Assert.Throws<ArgumentException>(() =>
            new WaveletEntropy([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10], window));
    }

    // ── Branch 3 — window < 4 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_WindowBelowFour_Throws(int window)
    {
        Assert.Throws<ArgumentException>(() => new WaveletEntropy([1.0, 2, 3, 4], window));
    }

    // ── Branch 4 — prices.Length < window → empty ──
    [Fact]
    public void Compute_LengthBelowWindow_ReturnsEmpty()
    {
        var w = new WaveletEntropy([1.0, 2, 3], window: 4);
        Assert.Empty(w.Compute().Values);
    }

    // ── Branch 5 — prices.Length == window → single output ──
    [Fact]
    public void Compute_LengthEqualsWindow_ReturnsSingleValue()
    {
        var w = new WaveletEntropy([1.0, 2, 3, 4], window: 4);
        Assert.Single(w.Compute().Values);
    }

    // ── Branch 6 — Total energy == 0 (constant signal=0) → output 0 (guard) ──
    [Fact]
    public void Compute_AllZeroSignal_ReturnsZero()
    {
        var prices = new double[20];
        var w = new WaveletEntropy(prices, window: 16).Compute().Values;
        Assert.NotEmpty(w);
        Assert.All(w, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 7 — Constant non-zero signal → one band (approx) dominates → entropy near 0 ──
    [Fact]
    public void Compute_ConstantNonZeroSignal_EntropyNearZero()
    {
        var prices = Enumerable.Repeat(100.0, 100).ToArray();
        var w = new WaveletEntropy(prices, window: 64).Compute().Values;
        Assert.NotEmpty(w);
        // Details all zero → approx holds all energy → p = [0, 0, 0, 0, 0, 0, 1] → H = 0.
        Assert.All(w, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 7b — Alternating ±1 → one detail band dominates → entropy near 0 ──
    [Fact]
    public void Compute_AlternatingSignal_EntropyNearZero()
    {
        var alt = new double[100];
        for (int i = 0; i < 100; i++) alt[i] = (i % 2 == 0) ? 1.0 : -1.0;
        var w = new WaveletEntropy(alt, window: 64).Compute().Values;
        Assert.True(w[^1] < 0.1, $"expected entropy < 0.1 for alternating signal, got {w[^1]}");
    }

    // ── Branch 8 — Uniform energy across bands → entropy near 1 ──
    //
    // Construct a signal whose DWT energy is spread across all bands. Superpose
    // oscillations at each scale: 1/2 the bars swing at scale 1, 1/4 at scale 2, etc.
    // For a window of 16 (4 levels), a zero-mean noisy signal should distribute
    // energy roughly across all bands.
    [Fact]
    public void Compute_NoisySignal_HasModerateEntropy()
    {
        var rng = new Random(42);
        var prices = new double[200];
        for (int i = 0; i < 200; i++) prices[i] = rng.NextDouble() - 0.5;
        var w = new WaveletEntropy(prices, window: 16).Compute().Values;
        Assert.All(w, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, 0.0, 1.0);
        });
        // Random noise → energy roughly spread → entropy > 0.5 typically.
        Assert.True(w[^1] > 0.5, $"expected noisy entropy > 0.5, got {w[^1]}");
    }

    // ── Branch 9 — Normal multi-window path — output range bounded ──
    [Fact]
    public void Compute_VariedSignal_OutputBounded()
    {
        var prices = new double[100];
        for (int i = 0; i < 100; i++) prices[i] = Math.Sin(i * 0.2) + 0.1 * Math.Sin(i * 1.5);
        var w = new WaveletEntropy(prices, window: 16).Compute().Values;
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
        new WaveletEntropy(prices, window: 16).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsYAxisZeroToOne()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        new WaveletEntropy(prices, window: 16).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        new WaveletEntropy(prices, window: 32).Apply(axes);
        Assert.Equal("WEnt(W=32)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultWindow()
    {
        var prices = new double[100];
        for (int i = 0; i < 100; i++) prices[i] = i;
        var w = new WaveletEntropy(prices);
        Assert.Equal("WEnt(W=64)", w.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var w = new WaveletEntropy([1.0, 2, 3, 4], window: 4);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(w);
        Assert.IsAssignableFrom<IIndicator>(w);
    }
}
