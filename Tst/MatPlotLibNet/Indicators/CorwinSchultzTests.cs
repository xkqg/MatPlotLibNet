// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="CorwinSchultz"/>. Covers all 10 branches enumerated in
/// docs/contrib/indicator-tier-1c.md §2.</summary>
public class CorwinSchultzTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var cs = new CorwinSchultz([], [], period: 2);
        Assert.Empty(cs.Compute().Values);
    }

    // ── Branch 2 — high/low length mismatch throws ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new CorwinSchultz([100.0, 101], [99.0], period: 2));
    }

    // ── Branch 3 — Length <= period → empty ──
    [Fact]
    public void Compute_LengthEqualsPeriod_ReturnsEmpty()
    {
        var cs = new CorwinSchultz(
            [101.0, 102], [99.0, 100], period: 2);
        Assert.Empty(cs.Compute().Values);
    }

    // ── Branch 4 — Length == period + 1 → single output ──
    [Fact]
    public void Compute_LengthEqualsPeriodPlusOne_ReturnsSingleValue()
    {
        var cs = new CorwinSchultz(
            [100.5, 100.5, 100.5], [99.5, 99.5, 99.5], period: 2);
        Assert.Single(cs.Compute().Values);
    }

    // ── Branch 5 — period < 2 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_PeriodBelowTwo_Throws(int period)
    {
        Assert.Throws<ArgumentException>(() =>
            new CorwinSchultz([101.0, 102, 103], [99.0, 100, 101], period));
    }

    // ── Branch 6 — Non-positive H or L throws ──
    [Theory]
    [InlineData(new double[] { 0.0, 101 }, new double[] { 99.0, 100 })]
    [InlineData(new double[] { 101.0, -5 }, new double[] { 99.0, 100 })]
    [InlineData(new double[] { 101.0, 102 }, new double[] { 0.0, 100 })]
    [InlineData(new double[] { 101.0, 102 }, new double[] { 99.0, -5 })]
    public void Constructor_NonPositiveHighOrLow_Throws(double[] h, double[] l)
    {
        Assert.Throws<ArgumentException>(() =>
            new CorwinSchultz(h, l, period: 2));
    }

    // ── Branch 7 — H < L on any bar throws ──
    [Fact]
    public void Constructor_HighBelowLow_Throws()
    {
        // Bar 0 has H=100 < L=101 (invalid). Throw at construction.
        Assert.Throws<ArgumentException>(() =>
            new CorwinSchultz([100.0, 101], [101.0, 100], period: 2));
    }

    // ── Branch 8 — Negative α → S clamped to 0 (sustained drift across all bars) ──
    [Fact]
    public void Compute_SustainedDrift_ClampsToZero()
    {
        // Both per-bar α values go negative → per-bar S clamps to 0 → rolling mean = 0.
        var cs = new CorwinSchultz(
            [110.0, 130, 150], [90.0, 110, 130], period: 2);
        var r = cs.Compute().Values;
        Assert.Single(r);
        Assert.Equal(0.0, r[0], precision: 12);
    }

    // ── Branch 9 — Flat H == L → β,γ = 0 → α → −∞ → clamped to 0 ──
    [Fact]
    public void Compute_FlatHEqualsL_ReturnsZero()
    {
        // All bars have H == L → β = γ = 0 → α → −∞ → S → −2 → clamped to 0.
        var cs = new CorwinSchultz(
            [100.0, 100, 100, 100], [100.0, 100, 100, 100], period: 2);
        var r = cs.Compute().Values;
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 10 — Normal multi-bar path (reference vector with non-zero S) ──
    // Hand-derived closed form for constant H/L: α = |ln(H/L)|.
    //   S = 2·(e^α − 1) / (1 + e^α) = 2·tanh(α/2)
    // H = 100.5, L = 99.5  → α = ln(100.5/99.5) ≈ 0.010001 → S ≈ 0.01000066.
    [Fact]
    public void Compute_ConstantHL_MatchesClosedForm()
    {
        var cs = new CorwinSchultz(
            [100.5, 100.5, 100.5, 100.5],
            [99.5, 99.5, 99.5, 99.5],
            period: 2);
        var r = cs.Compute().Values;
        Assert.Equal(2, r.Length); // n - period = 4 - 2 = 2
        double alpha = Math.Abs(Math.Log(100.5 / 99.5));
        double expected = 2 * (Math.Exp(alpha) - 1) / (1 + Math.Exp(alpha));
        Assert.Equal(expected, r[0], precision: 10);
        Assert.Equal(expected, r[1], precision: 10);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new CorwinSchultz([100.5, 100.5, 100.5], [99.5, 99.5, 99.5], period: 2).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new CorwinSchultz([100.5, 100.5, 100.5], [99.5, 99.5, 99.5], period: 2).Apply(axes);
        Assert.Equal("CS(2)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultPeriod_Is20()
    {
        var h = new double[25]; var l = new double[25];
        for (int i = 0; i < 25; i++) { h[i] = 101; l[i] = 99; }
        var cs = new CorwinSchultz(h, l);
        Assert.Equal("CS(20)", cs.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsCandleIndicator()
    {
        var cs = new CorwinSchultz([100.5, 100.5, 100.5], [99.5, 99.5, 99.5], period: 2);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(cs);
        Assert.IsAssignableFrom<IIndicator>(cs);
    }
}
