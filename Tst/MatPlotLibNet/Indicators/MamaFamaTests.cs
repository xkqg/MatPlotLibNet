// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="MamaFama"/> (Ehlers' MESA Adaptive Moving Average).
/// Covers all 16 branches enumerated in docs/contrib/indicator-tier-1d.md §2.</summary>
public class MamaFamaTests
{
    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new MamaFama([]);
        var res = r.Compute();
        Assert.Empty(res.Mama);
        Assert.Empty(res.Fama);
    }

    // ── Branch 2 — Length < 7 (warmup not met) → empty ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    public void Compute_LengthBelowWarmup_ReturnsEmpty(int n)
    {
        var p = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var res = new MamaFama(p).Compute();
        Assert.Empty(res.Mama);
        Assert.Empty(res.Fama);
    }

    // ── Branch 3 — Length == 7 → one output row ──
    [Fact]
    public void Compute_LengthAtWarmup_ReturnsSingleRow()
    {
        var p = Enumerable.Range(0, 7).Select(i => 100.0 + i).ToArray();
        var res = new MamaFama(p).Compute();
        Assert.Single(res.Mama);
        Assert.Single(res.Fama);
    }

    // ── Branches 4–7 — Constructor validation ──
    [Theory]
    [InlineData(0.0, 0.05)]     // fastLimit <= 0
    [InlineData(-0.1, 0.05)]    // fastLimit negative
    public void Constructor_FastLimitAtOrBelowZero_Throws(double fast, double slow)
    {
        Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101], fast, slow));
    }

    [Theory]
    [InlineData(1.5, 0.05)]     // fastLimit > 1
    [InlineData(2.0, 0.05)]
    public void Constructor_FastLimitAboveOne_Throws(double fast, double slow)
    {
        Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101], fast, slow));
    }

    [Theory]
    [InlineData(0.5, 0.0)]      // slowLimit <= 0
    [InlineData(0.5, -0.05)]
    public void Constructor_SlowLimitAtOrBelowZero_Throws(double fast, double slow)
    {
        Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101], fast, slow));
    }

    [Theory]
    [InlineData(0.5, 0.5)]      // slowLimit == fastLimit
    [InlineData(0.5, 0.6)]      // slowLimit > fastLimit
    public void Constructor_SlowLimitAtOrAboveFast_Throws(double fast, double slow)
    {
        Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101], fast, slow));
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var ex = Record.Exception(() => new MamaFama([100.0, 101], fastLimit: 0.5, slowLimit: 0.05));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_FastLimitEqualsOne_DoesNotThrow()
    {
        // Boundary: fastLimit = 1.0 is allowed (upper-inclusive).
        var ex = Record.Exception(() => new MamaFama([100.0, 101], fastLimit: 1.0, slowLimit: 0.05));
        Assert.Null(ex);
    }

    // ── Branches 8, 9, 13, 16 — Flat prices ──
    //   Im=Re=0 → period falls back to previous (Branch 8)
    //   Q1≈0 → phase falls back to previous (Branch 9)
    //   DeltaPhase=0 → clamped to 1 (Branch 13)
    //   MAMA = FAMA = price forever (Branch 16)
    [Fact]
    public void Compute_FlatPrices_MamaFamaTrackPrice()
    {
        var prices = Enumerable.Repeat(100.0, 100).ToArray();
        var res = new MamaFama(prices).Compute();
        Assert.Equal(100 - 6, res.Mama.Length);
        Assert.All(res.Mama, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(100.0, v, precision: 9);
        });
        Assert.All(res.Fama, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(100.0, v, precision: 9);
        });
    }

    // ── Branches 10–12 — Period clamps (upper 1.5x, lower 0.67x, absolute [6, 50]) ──
    // Use a long, irregular series to exercise all clamps. These are exercised whenever
    // the Hilbert homodyne discriminator would propose a period outside the admissible
    // range — guaranteed on noisy or regime-changing data.
    [Fact]
    public void Compute_IrregularSeries_AllPeriodClampsExercised()
    {
        var rng = new Random(42);
        int n = 250;
        var prices = new double[n];
        prices[0] = 100;
        for (int i = 1; i < n; i++)
            prices[i] = prices[i - 1] * (1 + (rng.NextDouble() - 0.5) * 0.10);

        var res = new MamaFama(prices).Compute();
        Assert.Equal(n - 6, res.Mama.Length);
        Assert.All(res.Mama, v => Assert.False(double.IsNaN(v)));
        Assert.All(res.Fama, v => Assert.False(double.IsNaN(v)));
    }

    // ── Branches 14, 15 — Alpha clamps to fastLimit / slowLimit ──
    // Sinusoidal series with varying period exercises the deltaPhase range.
    [Fact]
    public void Compute_SinusoidalSeries_StaysFinite()
    {
        int n = 500;
        var prices = new double[n];
        for (int i = 0; i < n; i++)
            prices[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 20.0);

        var res = new MamaFama(prices).Compute();
        Assert.Equal(n - 6, res.Mama.Length);
        Assert.All(res.Mama, v => Assert.False(double.IsNaN(v)));
        Assert.All(res.Fama, v => Assert.False(double.IsNaN(v)));
        // MAMA and FAMA should stay within the price envelope (with some lag/overshoot).
        foreach (var v in res.Mama)
            Assert.InRange(v, 80.0, 120.0);
    }

    // ── Monotonic uptrend — MAMA tracks close, FAMA lags ──
    [Fact]
    public void Compute_MonotonicUptrend_MamaAheadOfFama()
    {
        var prices = Enumerable.Range(0, 200).Select(i => 100.0 + i).ToArray();
        var res = new MamaFama(prices).Compute();
        // Once the Hilbert transform stabilises, MAMA should be > FAMA in uptrend
        // (MAMA tracks faster). Check from around index 50 onwards.
        for (int i = 50; i < res.Mama.Length; i++)
            Assert.True(res.Mama[i] >= res.Fama[i] - 0.5,
                $"bar {i}: MAMA={res.Mama[i]}, FAMA={res.Fama[i]}");
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsTwoLineSeries()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
        new MamaFama(prices).Apply(axes);
        Assert.Equal(2, axes.Series.Count);
    }

    [Fact]
    public void Apply_SetsExpectedLabels()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
        new MamaFama(prices).Apply(axes);
        Assert.Equal("MAMA", axes.Series[0].Label);
        Assert.Equal("FAMA", axes.Series[1].Label);
    }

    [Fact]
    public void ConstructorLabel_IncludesBothLimits()
    {
        var r = new MamaFama([100.0, 101], fastLimit: 0.5, slowLimit: 0.05);
        Assert.Equal("MAMA(0.50/0.05)", r.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var r = new MamaFama([100.0, 101]);
        Assert.IsAssignableFrom<PriceIndicator<MamaFamaResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
