// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Tests.Indicators.Ehlers;

/// <summary>Verifies <see cref="HilbertStep"/>, the Hilbert transform and homodyne discriminator that MAMA/FAMA, the sine wave, the
/// instantaneous trend and the adaptive stochastic fold; the tests fold it over a series themselves. Covers the warmup and the normal
/// paths; the tests of those indicators are the cross-check for the consumers.</summary>
public class HilbertStepTests
{
    private static HilbertRow[] Fold(double[] prices)
    {
        var step = new HilbertStep();
        var rows = new HilbertRow[prices.Length];
        for (int i = 0; i < prices.Length; i++) rows[i] = step.Next(prices[i]);
        return rows;
    }

    // ── Warmup region — first 6 bars are zero ──
    [Fact]
    public void LengthBelowSeven_AllZeroRows()
    {
        var rows = Fold([1.0, 2, 3, 4, 5, 6]);
        Assert.Equal(6, rows.Length);
        Assert.All(rows, row =>
        {
            Assert.Equal(0.0, row.Period, precision: 12);
            Assert.Equal(0.0, row.Phase, precision: 12);
            Assert.Equal(0.0, row.I1, precision: 12);
            Assert.Equal(0.0, row.Q1, precision: 12);
        });
    }

    [Fact]
    public void LengthSeven_FirstSixBarsStayAtZero()
    {
        var rows = Fold(Enumerable.Range(0, 7).Select(i => 100.0 + i).ToArray());
        Assert.Equal(7, rows.Length);
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(0.0, rows[i].Period, precision: 12);
            Assert.Equal(0.0, rows[i].Phase, precision: 12);
            Assert.Equal(0.0, rows[i].I1, precision: 12);
            Assert.Equal(0.0, rows[i].Q1, precision: 12);
        }
    }

    // ── Flat input — Hilbert kernel on constants yields near-zero I1/Q1 (modulo
    // FP accumulation); period stays in the clamped [6, 50] range and never NaNs. ──
    [Fact]
    public void FlatPrices_I1Q1NearZero_PeriodStaysClamped()
    {
        var rows = Fold(Enumerable.Repeat(100.0, 50).ToArray());

        for (int i = 6; i < 50; i++)
        {
            Assert.True(Math.Abs(rows[i].I1) < 1e-10, $"i1[{i}] = {rows[i].I1}");
            Assert.True(Math.Abs(rows[i].Q1) < 1e-10, $"q1[{i}] = {rows[i].Q1}");
        }

        for (int i = 8; i < 50; i++)
        {
            Assert.InRange(rows[i].Period, 0.0, 50.0);
        }
    }

    // ── Sinusoid at period 20 — discriminator should produce non-zero I1/Q1 and period ──
    [Fact]
    public void Sinusoid_ProducesFiniteOutputs()
    {
        int n = 300;
        var prices = new double[n];
        for (int i = 0; i < n; i++)
            prices[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 20);

        var rows = Fold(prices);
        Assert.Equal(n, rows.Length);
        Assert.All(rows, row =>
        {
            row.Period.AssertFinite();
            row.Phase.AssertFinite();
            row.I1.AssertFinite();
            row.Q1.AssertFinite();
        });

        for (int i = 150; i < n; i++)
            Assert.InRange(rows[i].Period, 6.0, 50.0);
    }

    // ── Random-walk noise — the period must stay finite and within the clamp range ──
    [Fact]
    public void NoisySeries_PeriodStaysWithinClamp()
    {
        int n = 300;
        double[] prices = SampleSeries.RandomWalk(n, 0.02);

        var rows = Fold(prices);
        for (int i = 7; i < n; i++)
        {
            Assert.InRange(rows[i].Period, 0.0, 50.0);
        }
    }
}
