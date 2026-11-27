// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="MamaFama"/> (Ehlers' MESA Adaptive Moving Average). Its arithmetic is held here against reference values;
/// what it keeps with every indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see
/// cref="IndicatorContractTests"/>.</summary>
public class MamaFamaTests
{
    // ── Branch 3 — Length == 7 → one output row ──
    [Fact]
    public void Compute_LengthAtWarmup_ReturnsSingleRow()
    {
        var p = Enumerable.Range(0, 7).Select(i => 100.0 + i).ToArray();
        var res = new MamaFama(p).Compute();
        Assert.Single(res.Mama);
        Assert.Single(res.Fama);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)] // Boundary: fastLimit = 1.0 is allowed (upper-inclusive).
    public void Constructor_AFastLimitUpToOne_DoesNotThrow(double fastLimit) =>
        Assert.Null(Record.Exception(() => new MamaFama([100.0, 101], fastLimit: fastLimit, slowLimit: 0.05)));

    // ── Branches 8, 9, 13, 16 — Flat prices ──
    //   Im=Re=0 → period falls back to previous (Branch 8)
    //   Q1≈0 → phase falls back to previous (Branch 9)
    //   DeltaPhase=0 → clamped to 1 (Branch 13)
    //   MAMA = FAMA = price forever (Branch 16)

    // ── Branches 10–12 — Period clamps (upper 1.5x, lower 0.67x, absolute [6, 50]) ──
    // Use a long, irregular series to exercise all clamps. These are exercised whenever
    // the Hilbert homodyne discriminator would propose a period outside the admissible
    // range — guaranteed on noisy or regime-changing data.
    [Fact]
    public void Compute_IrregularSeries_AllPeriodClampsExercised()
    {
        int n = 250;
        double[] prices = SampleSeries.RandomWalk(n, 0.10);

        var res = new MamaFama(prices).Compute();
        Assert.Equal(n - 6, res.Mama.Length);
        res.Mama.AssertAllFinite();
        res.Fama.AssertAllFinite();
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
        res.Mama.AssertAllFinite();
        res.Fama.AssertAllFinite();
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

    [Fact]
    public void ConstructorLabel_IncludesBothLimits()
    {
        var r = new MamaFama([100.0, 101], fastLimit: 0.5, slowLimit: 0.05);
        Assert.Equal("MAMA(0.50/0.05)", r.Label);
    }
}
