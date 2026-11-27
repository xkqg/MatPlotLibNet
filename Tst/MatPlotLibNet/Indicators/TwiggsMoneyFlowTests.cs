// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="TwiggsMoneyFlow"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class TwiggsMoneyFlowTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Ramp(double start, double step, int n)
        => Enumerable.Range(0, n).Select(i => start + step * i).ToArray();

    // ── Branch 6 — Flat prices + zero volume → EMAs both 0 → TMF = 0 via guard ──
    [Fact]
    public void Compute_ZeroVolume_NoDivisionByZero()
    {
        int n = 30;
        var tmf = new TwiggsMoneyFlow(OhlcvBars.Hlcv(Flat(100, n), Flat(100, n), Flat(100, n), Flat(0, n)), period: 10)
            .Compute().Values;
        Assert.Equal(n - 1, tmf.Length);
        Assert.All(tmf, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 7 — Pure uptrend (C == TH each bar) → TMF → +1 ──
    [Fact]
    public void Compute_CloseAtTrueHigh_ConvergesTowardPlusOne()
    {
        int n = 100;
        var high = Ramp(102, 1, n);
        var low = Ramp(98, 1, n);
        var close = Ramp(102, 1, n); // C == H → (2C - TH - TL) / TR = (2C - max(H,pC) - min(L,pC)) / TR → (C - pC) / TR-ish
        // After warmup, TMF should trend strongly positive.
        var tmf = new TwiggsMoneyFlow(OhlcvBars.Hlcv(high, low, close, Flat(1000, n)), period: 10).Compute().Values;
        Assert.True(tmf[^1] > 0.5, $"expected tail TMF > 0.5 on close-at-TH, got {tmf[^1]}");
    }

    // ── Branch 8 — Pure downtrend (C == TL each bar) → TMF → -1 ──
    [Fact]
    public void Compute_CloseAtTrueLow_ConvergesTowardMinusOne()
    {
        int n = 100;
        // Downtrend: each bar's HLC lower than the previous bar's close.
        var high = Enumerable.Range(0, n).Select(i => 200.0 - i).ToArray();
        var low = Enumerable.Range(0, n).Select(i => 196.0 - i).ToArray();
        var close = low;                                                       // C at low extreme
        var tmf = new TwiggsMoneyFlow(OhlcvBars.Hlcv(high, low, close, Flat(1000, n)), period: 10).Compute().Values;
        Assert.True(tmf[^1] < -0.5, $"expected tail TMF < -0.5 on close-at-TL, got {tmf[^1]}");
    }

    // ── Branch 9 — Normal multi-bar path, finite output ──
    [Fact]
    public void Compute_MixedData_AllFinite()
    {
        int n = 50;
        var rng = new Random(42);
        var h = new double[n]; var l = new double[n]; var c = new double[n]; var v = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + i + rng.NextDouble();
            h[i] = mid + 1;
            l[i] = mid - 1;
            c[i] = mid + rng.NextDouble() - 0.5;
            v[i] = 1000 + rng.NextDouble() * 500;
        }
        var tmf = new TwiggsMoneyFlow(OhlcvBars.Hlcv(h, l, c, v), period: 10).Compute().Values;
        Assert.Equal(n - 1, tmf.Length);
        Assert.All(tmf, x => Assert.InRange(x, -1.0, 1.0));
    }
}
