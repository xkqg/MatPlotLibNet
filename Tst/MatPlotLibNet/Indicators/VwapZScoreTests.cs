// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="VwapZScore"/>. Covers all 10 branches in
/// docs/contrib/indicator-tier-3a.md §4.</summary>
public class VwapZScoreTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();

    // ── Branch 1 — Empty input ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var z = new VwapZScore([], []);
        Assert.Empty(z.Compute().Values);
    }

    // ── Branch 2 — CV length mismatch → throw ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new VwapZScore([1.0, 2, 3], [1000.0, 1000]));
    }

    // ── Branch 3 — BarCount < window → empty ──
    [Fact]
    public void Compute_BarCountBelowWindow_ReturnsEmpty()
    {
        var z = new VwapZScore(Flat(100, 5), Flat(1000, 5), window: 20);
        Assert.Empty(z.Compute().Values);
    }

    // ── Branch 4 — BarCount == window → single output ──
    [Fact]
    public void Compute_BarCountEqualsWindow_ReturnsSingleValue()
    {
        // BarCount=window → single window → one output. With flat input it's 0.
        var z = new VwapZScore(Flat(100, 20), Flat(1000, 20), window: 20).Compute().Values;
        Assert.Single(z);
        Assert.Equal(0.0, z[0], precision: 10);
    }

    // ── Branch 5 — window < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_WindowBelowTwo_Throws(int window)
    {
        Assert.Throws<ArgumentException>(() =>
            new VwapZScore(Flat(100, 30), Flat(1000, 30), window));
    }

    // ── Branch 6 — Zero total volume in window → VWAP guard → Z = 0 ──
    [Fact]
    public void Compute_ZeroVolume_NoDivisionByZero()
    {
        int n = 30;
        var z = new VwapZScore(Flat(100, n), Flat(0, n), window: 10).Compute().Values;
        Assert.Equal(n - 10 + 1, z.Length);
        Assert.All(z, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
            Assert.Equal(0.0, x, precision: 10);
        });
    }

    // ── Branch 7 — Constant price → all deviations 0 → stddev = 0 → Z = 0 (guard) ──
    [Fact]
    public void Compute_ConstantPrice_ReturnsZero()
    {
        int n = 50;
        var z = new VwapZScore(Flat(100, n), Flat(1000, n), window: 10).Compute().Values;
        Assert.Equal(n - 10 + 1, z.Length);
        Assert.All(z, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 8 — Upward drift + noise → tail Z positive ──
    // Note: a *pure* linear drift with constant volume produces a constant deviation
    // (stddev = 0 → Z = 0 by guard). Real drift always has noise — add a small jitter
    // so stddev > 0 and the "Z positive on upward drift" arm is actually exercised.
    [Fact]
    public void Compute_UpwardDriftWithNoise_TailZPositive()
    {
        int n = 60;
        var rng = new Random(3);
        var c = Enumerable.Range(0, n).Select(i => 100.0 + i + (rng.NextDouble() - 0.5) * 0.5).ToArray();
        var z = new VwapZScore(c, Flat(1000, n), window: 20).Compute().Values;
        Assert.True(z[^1] > 0, $"expected positive Z on upward drift tail, got {z[^1]}");
    }

    // ── Branch 9 — Strong dislocation → Z magnitude > 1 (not necessarily > 3 with this sample) ──
    [Fact]
    public void Compute_StepDislocation_ProducesLargeZ()
    {
        int n = 40;
        var c = new double[n];
        for (int i = 0; i < n - 1; i++) c[i] = 100.0;
        c[n - 1] = 150.0;                                // sudden jump on the last bar
        var z = new VwapZScore(c, Flat(1000, n), window: 20).Compute().Values;
        Assert.True(Math.Abs(z[^1]) > 1.0, $"expected |Z| > 1 on dislocation, got {z[^1]}");
    }

    // ── Branch 10 — Normal multi-bar path ──
    [Fact]
    public void Compute_MixedData_AllFinite()
    {
        int n = 100;
        var rng = new Random(7);
        var c = new double[n];
        var v = new double[n];
        for (int i = 0; i < n; i++)
        {
            c[i] = 100 + i + rng.NextDouble();
            v[i] = 1000 + rng.NextDouble() * 500;
        }
        var z = new VwapZScore(c, v, window: 20).Compute().Values;
        Assert.Equal(n - 20 + 1, z.Length);
        Assert.All(z, x =>
        {
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsInfinity(x));
        });
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        int n = 30;
        new VwapZScore(Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray(), Flat(1000, n))
            .Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void DefaultLabel_HasWindow()
    {
        var z = new VwapZScore([1.0, 2], [1000.0, 1000]);
        Assert.Equal("VwapZ(20)", z.Label);
    }

    [Fact]
    public void InheritsCandleIndicator()
    {
        var z = new VwapZScore([1.0, 2], [1000.0, 1000]);
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(z);
        Assert.IsAssignableFrom<IIndicator>(z);
    }
}
