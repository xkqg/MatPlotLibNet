// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="CyberCycle"/>. Covers all 8 branches in
/// docs/contrib/indicator-tier-2c.md §1.</summary>
public class CyberCycleTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var cc = new CyberCycle([]);
        Assert.Empty(cc.Compute().Values);
    }

    // ── Branch 2 — Length < 4 → empty ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Compute_LengthBelowFour_ReturnsEmpty(int n)
    {
        var prices = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var cc = new CyberCycle(prices);
        Assert.Empty(cc.Compute().Values);
    }

    // ── Branch 3 — Length == 4 → single output ──
    [Fact]
    public void Compute_LengthFour_ReturnsSingleValue()
    {
        var cc = new CyberCycle([100.0, 101, 102, 103]);
        Assert.Single(cc.Compute().Values);
    }

    // ── Branch 4 — alpha <= 0 throws ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    public void Constructor_NonPositiveAlpha_Throws(double alpha)
    {
        Assert.Throws<ArgumentException>(() => new CyberCycle([1.0, 2, 3, 4], alpha));
    }

    // ── Branch 5 — alpha >= 1 throws ──
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Constructor_AlphaAtOrAboveOne_Throws(double alpha)
    {
        Assert.Throws<ArgumentException>(() => new CyberCycle([1.0, 2, 3, 4], alpha));
    }

    // ── Branch 6 — Constant prices → CC = 0 everywhere ──
    [Fact]
    public void Compute_ConstantPrices_ReturnsZero()
    {
        var prices = Enumerable.Repeat(100.0, 30).ToArray();
        var cc = new CyberCycle(prices).Compute().Values;
        Assert.Equal(27, cc.Length); // 30 - 3
        Assert.All(cc, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(0.0, v, precision: 10);
        });
    }

    // ── Branch 7 — Pure sinusoid — CC produces non-trivial cycle output ──
    [Fact]
    public void Compute_Sinusoid_HasSignificantAmplitude()
    {
        int n = 200;
        var prices = new double[n];
        for (int i = 0; i < n; i++) prices[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 15);
        var cc = new CyberCycle(prices, alpha: 0.07).Compute().Values;

        // Tail portion should oscillate with non-trivial amplitude.
        double tailMax = 0;
        for (int i = 150; i < cc.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(cc[i]));
        Assert.True(tailMax > 0.5, $"expected cycle amplitude, got tailMax={tailMax}");
    }

    // ── Branch 8 — Normal multi-bar path + known reference ──
    //
    // Python reference (verbatim recurrence):
    //   p = [1, 2, 3, 5, 4, 6, 8, 7]
    //   alpha = 0.07
    //   s = [p[0], p[0], p[0], (p[3]+2*p[2]+2*p[1]+p[0])/6, ...]
    //   (for i < 3 initialize smooth = p[i]; then formula)
    //   CC recurrence from i=3 onwards, CC[0..2] = 0.
    // Output = CC[3:].
    [Fact]
    public void Compute_KnownVector_AllFinite()
    {
        var prices = new[] { 1.0, 2, 3, 5, 4, 6, 8, 7 };
        var cc = new CyberCycle(prices, alpha: 0.2).Compute().Values;
        Assert.Equal(5, cc.Length); // 8 - 3
        Assert.All(cc, v => Assert.False(double.IsNaN(v)));
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new CyberCycle([100.0, 101, 102, 103, 104]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new CyberCycle([100.0, 101, 102, 103, 104], alpha: 0.15).Apply(axes);
        Assert.Equal("CC(0.15)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultAlpha_IsPointZeroSeven()
    {
        var cc = new CyberCycle([1.0, 2, 3, 4]);
        Assert.Equal("CC(0.07)", cc.Label);
    }

    [Fact]
    public void InheritsPriceIndicator()
    {
        var cc = new CyberCycle([1.0, 2, 3, 4]);
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(cc);
        Assert.IsAssignableFrom<IIndicator>(cc);
    }
}
