// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="VwapZScore"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class VwapZScoreTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();

    /// <summary>The rolling-std pass's OWN local-VWAP guard (distinct from the outer per-window one Branch 6
    /// above covers): a SUB-window ending at an early bar can have zero volume even while the OUTER window it
    /// sits in does not (its later bars carry the volume). window=2, bars 0..4, volume [5,0,0,5,5]: window w=2
    /// (bars 2..3) has outer sum 0+5=5 (passes the outer guard), but its first inner i=2's own sub-window
    /// (bars 1..2) sums to 0+0=0 — the branch this pins.</summary>
    [Fact]
    public void Compute_ASubWindowWithZeroVolume_InAnOtherwiseNonZeroOuterWindow_DoesNotThrow()
    {
        double[] close = [10.0, 11, 12, 13, 14];
        double[] volume = [5.0, 0, 0, 5, 5];

        var z = new VwapZScore(OhlcvBars.Cv(close, volume), window: 2);
        var result = z.Compute();

        Assert.Equal(4, result.Values.Length);
        result.Values.AssertAllFinite();
    }

    // ── Branch 4 — BarCount == window → single output ──
    [Fact]
    public void Compute_BarCountEqualsWindow_ReturnsSingleValue()
    {
        // BarCount=window → single window → one output. With flat input it's 0.
        var z = new VwapZScore(OhlcvBars.Cv(Flat(100, 20), Flat(1000, 20)), window: 20).Compute().Values;
        Assert.Single(z);
        Assert.Equal(0.0, z[0], precision: 10);
    }

    // ── Branch 6 — Zero total volume in window → VWAP guard → Z = 0 ──
    [Fact]
    public void Compute_ZeroVolume_NoDivisionByZero()
    {
        int n = 30;
        var z = new VwapZScore(OhlcvBars.Cv(Flat(100, n), Flat(0, n)), window: 10).Compute().Values;
        Assert.Equal(n - 10 + 1, z.Length);
        Assert.All(z, x => Assert.Equal(0.0, x, precision: 10));
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
        var z = new VwapZScore(OhlcvBars.Cv(c, Flat(1000, n)), window: 20).Compute().Values;
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
        var z = new VwapZScore(OhlcvBars.Cv(c, Flat(1000, n)), window: 20).Compute().Values;
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
        var z = new VwapZScore(OhlcvBars.Cv(c, v), window: 20).Compute().Values;
        Assert.Equal(n - 20 + 1, z.Length);
        z.AssertAllFinite();
    }
}
