// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="EhlersSuperSmoother"/>. Covers all 4 branches in
/// docs/contrib/indicator-tier-3c.md §3 (the underlying helper is separately tested from
/// Tier 2c — this wrapper only exposes it as a first-class indicator).</summary>
public class EhlersSuperSmootherTests
{
    // ── Branch 1 — Null input → throw ──
    [Fact]
    public void Constructor_NullInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EhlersSuperSmoother(null!));
    }

    // ── Branch 2 — Empty input → empty output ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var ss = new EhlersSuperSmoother([]);
        Assert.Empty(ss.Compute().Values);
    }

    // ── Branch 3 — period < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_PeriodBelowTwo_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() => new EhlersSuperSmoother([1.0, 2, 3], period));
    }

    // ── Branch 4 — Delegate call — wrapper output matches the public recurrence ──
    // First two bars equal the input; subsequent bars follow the 2-pole Butterworth recurrence.
    [Fact]
    public void Compute_FlatInput_ConvergesToConstant()
    {
        var flat = Enumerable.Repeat(100.0, 50).ToArray();
        var ss = new EhlersSuperSmoother(flat, period: 10).Compute().Values;
        Assert.Equal(50, ss.Length);
        // First 2 bars are seeded with the input.
        Assert.Equal(100.0, ss[0], precision: 10);
        Assert.Equal(100.0, ss[1], precision: 10);
        // Recurrence preserves the constant.
        Assert.All(ss, v => Assert.Equal(100.0, v, precision: 8));
    }

    [Fact]
    public void Compute_RampInput_AllFinite()
    {
        var prices = Enumerable.Range(0, 40).Select(i => 100.0 + i).ToArray();
        var ss = new EhlersSuperSmoother(prices, period: 10).Compute().Values;
        Assert.Equal(40, ss.Length);
        Assert.All(ss, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.False(double.IsInfinity(v));
        });
        // SuperSmoother trails a ramp with a small lag — tail should be close to tail input.
        Assert.True(Math.Abs(ss[^1] - prices[^1]) < 5.0);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var prices = Enumerable.Repeat(100.0, 20).ToArray();
        new EhlersSuperSmoother(prices, period: 10).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_HasPeriod()
    {
        var ss = new EhlersSuperSmoother([1.0, 2, 3]);
        Assert.Equal("SS(10)", ss.Label);
    }

    [Fact]
    public void InheritsIndicator()
    {
        var ss = new EhlersSuperSmoother([1.0, 2, 3]);
        Assert.IsAssignableFrom<Indicator<SignalResult>>(ss);
        Assert.IsAssignableFrom<IIndicator>(ss);
    }
}
