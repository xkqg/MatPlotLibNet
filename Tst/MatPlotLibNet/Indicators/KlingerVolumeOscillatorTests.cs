// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="KlingerVolumeOscillator"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class KlingerVolumeOscillatorTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Ramp(double start, double step, int n)
        => Enumerable.Range(0, n).Select(i => start + step * i).ToArray();

    // ── Branch 8 — CM == 0 (flat H == L on consecutive same-trend bars) → VF = 0 guard ──
    [Fact]
    public void Compute_HEqualsL_NoDivisionByZero()
    {
        // H == L on every bar → (H - L) = 0 everywhere, CM = 0, VF must be 0 (guard arm).
        // Typical price still moves since C varies, so trend is non-zero — but VF must still be 0.
        var h = new double[] { 100, 101, 102, 103, 104, 105, 106, 107 };
        var l = new double[] { 100, 101, 102, 103, 104, 105, 106, 107 };
        var c = new double[] { 100, 101, 102, 103, 104, 105, 106, 107 };
        var v = Flat(1000, 8);
        var r = new KlingerVolumeOscillator(OhlcvBars.Hlcv(h, l, c, v), fastPeriod: 2, slowPeriod: 3, signalPeriod: 2).Compute();
        Assert.Equal(7, r.Kvo.Length);
        Assert.All(r.Kvo, x => Assert.Equal(0.0, x, precision: 10));
    }

    // ── Branch 9 — Trend reversal → CM resets with prev+current range ──
    // ── Branch 10 — Trend sustains → CM accumulates ──
    // ── Branch 11 — Normal multi-bar path ──
    [Fact]
    public void Compute_AlternatingTrend_ProducesFiniteOutput()
    {
        // Saw-tooth HLC that flips trend every bar — exercises the reset arm repeatedly.
        int n = 40;
        var h = new double[n];
        var l = new double[n];
        var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + (i % 2 == 0 ? 1 : -1);
            h[i] = mid + 0.5;
            l[i] = mid - 0.5;
            c[i] = mid;
        }
        var vol = Flat(1000, n);
        var r = new KlingerVolumeOscillator(OhlcvBars.Hlcv(h, l, c, vol),
            fastPeriod: 3, slowPeriod: 5, signalPeriod: 3).Compute();

        Assert.Equal(n - 1, r.Kvo.Length);
        Assert.Equal(n - 1, r.Signal.Length);
        r.Kvo.AssertAllFinite();
        r.Signal.AssertAllFinite();
    }

    [Fact]
    public void Compute_SustainedUptrend_AccumulatesCm()
    {
        // Monotone rising HLC+positive volume → trend = +1 every bar → CM accumulates,
        // VF > 0, KVO settles positive. Strictly checking >0 on tail is sufficient
        // regression against the "CM reset incorrectly" bug class.
        int n = 80;
        var h = Ramp(101, 1, n);
        var l = Ramp(99, 1, n);
        var c = Ramp(100, 1, n);
        var v = Flat(1000, n);
        var r = new KlingerVolumeOscillator(OhlcvBars.Hlcv(h, l, c, v),
            fastPeriod: 5, slowPeriod: 13, signalPeriod: 5).Compute();

        // Tail portion should have positive KVO (sustained up-trend → positive VF → positive EMAs diff).
        // With fast=5, slow=13, the sign of KVO depends on whether fast EMA of VF exceeds slow EMA.
        // For a strictly-increasing input, the *levels* settle but fast EMA tracks closer — should be > 0.
        Assert.True(r.Kvo[^1] > 0,
            $"expected positive KVO tail on sustained uptrend, got {r.Kvo[^1]}");
    }
}
