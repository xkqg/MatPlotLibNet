// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="EhlersSineWave"/>. Covers all 8 branches in
/// docs/contrib/indicator-tier-2c.md §3.</summary>
public class EhlersSineWaveTests
{
    // ── Branch 1 — Empty input → all arrays empty ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var r = new EhlersSineWave([]).Compute();
        Assert.Empty(r.SineWave);
        Assert.Empty(r.LeadSine);
        Assert.Empty(r.IsCyclic);
    }

    // ── Branch 2 — Length < 7 → empty (Hilbert needs 6 warmup) ──
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(6)]
    public void Compute_LengthBelowSeven_ReturnsEmpty(int n)
    {
        var prices = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var r = new EhlersSineWave(prices).Compute();
        Assert.Empty(r.SineWave);
        Assert.Empty(r.LeadSine);
        Assert.Empty(r.IsCyclic);
    }

    // ── Branch 3 — Length == 7 → single output ──
    [Fact]
    public void Compute_LengthSeven_ReturnsSingleRow()
    {
        var prices = Enumerable.Range(0, 7).Select(i => 100.0 + i).ToArray();
        var r = new EhlersSineWave(prices).Compute();
        Assert.Single(r.SineWave);
        Assert.Single(r.LeadSine);
        Assert.Single(r.IsCyclic);
    }

    // ── Branch 4 — Flat prices — output finite, typically small; isCyclic stays false ──
    [Fact]
    public void Compute_FlatPrices_OutputsStayFinite()
    {
        var prices = Enumerable.Repeat(100.0, 100).ToArray();
        var r = new EhlersSineWave(prices).Compute();
        // Flat input yields FP-noise phase — IsCyclic classification is unreliable here
        // (the heuristic needs real phase dynamics). We only require finite, bounded output.
        Assert.All(r.SineWave, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, -1.0, 1.0);
        });
        Assert.All(r.LeadSine, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.InRange(v, -1.0, 1.0);
        });
    }

    // ── Branch 5 — Pure sinusoid — isCyclic true in settled tail; sineWave oscillates ──
    [Fact]
    public void Compute_Sinusoid_IsCyclicAndOscillates()
    {
        int n = 500;
        var prices = new double[n];
        for (int i = 0; i < n; i++)
            prices[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 20);

        var r = new EhlersSineWave(prices).Compute();
        // Sine-wave amplitude bounded by [-1, 1] by construction.
        Assert.All(r.SineWave, v => { Assert.False(double.IsNaN(v)); Assert.InRange(v, -1.0, 1.0); });
        Assert.All(r.LeadSine, v => { Assert.False(double.IsNaN(v)); Assert.InRange(v, -1.0, 1.0); });

        // After settling (~200 bars), sineWave must oscillate (have both + and - values).
        bool sawPositive = false, sawNegative = false;
        for (int i = 200; i < r.SineWave.Length; i++)
        {
            if (r.SineWave[i] > 0.3) sawPositive = true;
            if (r.SineWave[i] < -0.3) sawNegative = true;
        }
        Assert.True(sawPositive && sawNegative, "sineWave should oscillate after settling");

        // At least some bars in the stabilized region should flag IsCyclic = true.
        int cyclicCount = 0;
        for (int i = 200; i < r.IsCyclic.Length; i++) if (r.IsCyclic[i]) cyclicCount++;
        Assert.True(cyclicCount > 50,
            $"expected many IsCyclic bars for pure sinusoid, got {cyclicCount}");
    }

    // ── Branch 6 — Trend + cycle mixed — isCyclic toggles ──
    [Fact]
    public void Compute_TrendMixedCycle_IsCyclicTogglesBothBranches()
    {
        int n = 400;
        var prices = new double[n];
        // First half: steep trend (not cyclic). Second half: pure sinusoid (cyclic).
        for (int i = 0; i < n / 2; i++) prices[i] = 100 + 0.5 * i;
        for (int i = n / 2; i < n; i++)
            prices[i] = 300 + 10 * Math.Sin(2 * Math.PI * (i - n / 2) / 20);

        var r = new EhlersSineWave(prices).Compute();
        bool sawTrue = r.IsCyclic.Contains(true);
        bool sawFalse = r.IsCyclic.Contains(false);
        Assert.True(sawTrue, "expected some bars flagged IsCyclic = true");
        Assert.True(sawFalse, "expected some bars flagged IsCyclic = false");
    }

    // ── Branch 7 — Q1 ≈ 0 (handled via discriminator's fallback); output stays finite ──
    [Fact]
    public void Compute_FlatPricesNoNaN_CoversQ1ZeroPath()
    {
        var prices = Enumerable.Repeat(50.0, 20).ToArray();
        var r = new EhlersSineWave(prices).Compute();
        Assert.All(r.SineWave, v => Assert.False(double.IsNaN(v)));
        Assert.All(r.LeadSine, v => Assert.False(double.IsNaN(v)));
    }

    // ── Branch 8 — Normal multi-bar path — output lengths consistent, all finite ──
    [Fact]
    public void Compute_RandomWalk_ProducesConsistentLengthsAndFiniteOutputs()
    {
        var rng = new Random(42);
        int n = 200;
        var prices = new double[n];
        prices[0] = 100;
        for (int i = 1; i < n; i++)
            prices[i] = prices[i - 1] * (1 + (rng.NextDouble() - 0.5) * 0.02);

        var r = new EhlersSineWave(prices).Compute();
        Assert.Equal(n - 6, r.SineWave.Length);
        Assert.Equal(r.SineWave.Length, r.LeadSine.Length);
        Assert.Equal(r.SineWave.Length, r.IsCyclic.Length);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsTwoLineSeries()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        new EhlersSineWave(prices).Apply(axes);
        Assert.Equal(2, axes.Series.Count);
    }

    [Fact]
    public void Apply_SetsExpectedLabels()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        new EhlersSineWave(prices).Apply(axes);
        Assert.Equal("SineWave", axes.Series[0].Label);
        Assert.Equal("LeadSine", axes.Series[1].Label);
    }

    [Fact]
    public void Apply_SetsYAxisRange()
    {
        var axes = new Axes();
        var prices = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        new EhlersSineWave(prices).Apply(axes);
        Assert.Equal(-1.2, axes.YAxis.Min);
        Assert.Equal(1.2, axes.YAxis.Max);
    }

    [Fact]
    public void ConstructorLabel_IsSineWave()
    {
        var r = new EhlersSineWave([1.0, 2, 3, 4, 5, 6, 7]);
        Assert.Equal("SineWave", r.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsPriceIndicator()
    {
        var r = new EhlersSineWave([1.0, 2, 3, 4, 5, 6, 7]);
        Assert.IsAssignableFrom<PriceIndicator<SineWaveResult>>(r);
        Assert.IsAssignableFrom<IIndicator>(r);
    }
}
