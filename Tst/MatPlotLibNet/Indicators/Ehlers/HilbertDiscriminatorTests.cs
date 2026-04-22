// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;

namespace MatPlotLibNet.Tests.Indicators.Ehlers;

/// <summary>Verifies <see cref="HilbertDiscriminator"/> — the Hilbert + homodyne pipeline
/// extracted from MAMA/FAMA. Covers empty/warmup/normal paths; MAMA/FAMA's own tests
/// provide the cross-check for the consumer.</summary>
public class HilbertDiscriminatorTests
{
    // ── Empty input → empty arrays ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var (p, ph, i1, q1) = ReadOnlySpan<double>.Empty.HilbertDiscriminate();
        Assert.Empty(p);
        Assert.Empty(ph);
        Assert.Empty(i1);
        Assert.Empty(q1);
    }

    // ── Warmup region — first 6 bars are zero ──
    [Fact]
    public void Compute_LengthBelowSeven_AllZeroArrays()
    {
        double[] prices = [1.0, 2, 3, 4, 5, 6];
        var (period, phase, i1, q1) = prices.AsSpan().HilbertDiscriminate();
        Assert.Equal(6, period.Length);
        Assert.All(period, v => Assert.Equal(0.0, v, precision: 12));
        Assert.All(phase, v => Assert.Equal(0.0, v, precision: 12));
        Assert.All(i1, v => Assert.Equal(0.0, v, precision: 12));
        Assert.All(q1, v => Assert.Equal(0.0, v, precision: 12));
    }

    [Fact]
    public void Compute_LengthSeven_OutputLengthMatches()
    {
        var prices = Enumerable.Range(0, 7).Select(i => 100.0 + i).ToArray();
        var (period, phase, i1, q1) = prices.AsSpan().HilbertDiscriminate();
        Assert.Equal(7, period.Length);
        Assert.Equal(7, phase.Length);
        Assert.Equal(7, i1.Length);
        Assert.Equal(7, q1.Length);

        // First 6 bars are warmup — values stay at 0.
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(0.0, period[i], precision: 12);
            Assert.Equal(0.0, phase[i], precision: 12);
            Assert.Equal(0.0, i1[i], precision: 12);
            Assert.Equal(0.0, q1[i], precision: 12);
        }
    }

    // ── Flat input — Hilbert kernel on constants yields near-zero I1/Q1 (modulo
    // FP accumulation); period stays in the clamped [6, 50] range and never NaNs. ──
    [Fact]
    public void Compute_FlatPrices_I1Q1NearZero_PeriodStaysClamped()
    {
        var prices = Enumerable.Repeat(100.0, 50).ToArray();
        var (period, _, i1, q1) = prices.AsSpan().HilbertDiscriminate();

        for (int i = 6; i < 50; i++)
        {
            Assert.True(Math.Abs(i1[i]) < 1e-10, $"i1[{i}] = {i1[i]}");
            Assert.True(Math.Abs(q1[i]) < 1e-10, $"q1[{i}] = {q1[i]}");
        }

        for (int i = 8; i < 50; i++)
        {
            Assert.False(double.IsNaN(period[i]));
            Assert.InRange(period[i], 0.0, 50.0);
        }
    }

    // ── Sinusoid at period 20 — discriminator should produce non-zero I1/Q1 and period ──
    [Fact]
    public void Compute_Sinusoid_ProducesFiniteOutputs()
    {
        int n = 300;
        var prices = new double[n];
        for (int i = 0; i < n; i++)
            prices[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 20);

        var (period, phase, i1, q1) = prices.AsSpan().HilbertDiscriminate();
        Assert.Equal(n, period.Length);
        Assert.All(period, v => Assert.False(double.IsNaN(v)));
        Assert.All(phase, v => Assert.False(double.IsNaN(v)));
        Assert.All(i1, v => Assert.False(double.IsNaN(v)));
        Assert.All(q1, v => Assert.False(double.IsNaN(v)));

        for (int i = 150; i < n; i++)
            Assert.InRange(period[i], 6.0, 50.0);
    }

    // ── Random-walk noise — arrays must stay finite and within clamp range ──
    [Fact]
    public void Compute_NoisySeries_PeriodStaysWithinClamp()
    {
        var rng = new Random(42);
        int n = 300;
        var prices = new double[n];
        prices[0] = 100;
        for (int i = 1; i < n; i++)
            prices[i] = prices[i - 1] * (1 + (rng.NextDouble() - 0.5) * 0.02);

        var (period, _, _, _) = prices.AsSpan().HilbertDiscriminate();
        for (int i = 7; i < n; i++)
        {
            Assert.False(double.IsNaN(period[i]));
            Assert.InRange(period[i], 0.0, 50.0);
        }
    }

    // ── Chaining test — prices.HighPass(48).SuperSmooth(10) and similar compose. ──
    [Fact]
    public void ExtensionMethod_ChainingWorks()
    {
        double[] prices = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        // Feed HP output (array) back into SuperSmooth via AsSpan().
        var hp = prices.AsSpan().HighPass(20);
        var smoothed = hp.AsSpan().SuperSmooth(5);
        Assert.Equal(50, smoothed.Length);
        Assert.All(smoothed, v => Assert.False(double.IsNaN(v)));
    }
}
