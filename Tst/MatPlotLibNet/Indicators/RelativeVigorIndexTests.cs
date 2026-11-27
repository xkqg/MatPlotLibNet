// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="RelativeVigorIndex"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class RelativeVigorIndexTests
{
    /// <summary>With exactly <c>period + 4</c> bars the RVI has its first two values and the signal (which needs one more bar) none.
    /// It used to throw an OverflowException (a signal of length -1), which a streaming form compared against the batch over every
    /// prefix ran into on its fifth bar.</summary>
    [Fact]
    public void Compute_OneBarShortOfTheSignal_HasTheRviAndNoSignal_InsteadOfThrowing()
    {
        int period = 5, n = period + 4;
        double[] open = [.. Enumerable.Range(0, n).Select(i => 100.0 + i)];
        double[] close = [.. open.Select(v => v + 0.5)];
        double[] high = [.. close.Select(v => v + 1)];
        double[] low = [.. open.Select(v => v - 1)];

        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(open, high, low, close), period).Compute();

        Assert.Equal(2, r.Rvi.Length);
        Assert.Empty(r.Signal);
    }

    // ── Branch 4 — BarCount <= period + 3 → empty (insufficient for weighted-smooth + SMA) ──
    [Fact]
    public void Compute_InsufficientBars_ReturnsEmpty()
    {
        // period=5 → need > period + 5 = 10 bars for full RVI+Signal output; 7 < 8 = period+3.
        var a = Enumerable.Repeat(100.0, 7).ToArray();
        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(a, a, a, a), period: 5).Compute();
        Assert.Empty(r.Rvi);
        Assert.Empty(r.Signal);
    }

    // ── Branch 6 — Ranging market: Close == Open every bar, but H > L → RVI = 0 ──
    [Fact]
    public void Compute_PureRangingMarket_RviZero()
    {
        int n = 30;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            o[i] = 100; c[i] = 100; // Close == Open
            h[i] = 101; l[i] = 99;  // H > L
        }
        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(o, h, l, c), period: 5).Compute();
        Assert.All(r.Rvi, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 7 — Sustained uptrend (Close > Open always) → RVI positive ──
    [Fact]
    public void Compute_Uptrend_RviPositive()
    {
        int n = 40;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            o[i] = 100 + i;
            c[i] = o[i] + 0.8;   // strong close above open
            h[i] = o[i] + 1.0;
            l[i] = o[i] - 0.2;
        }
        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(o, h, l, c), period: 5).Compute();
        Assert.NotEmpty(r.Rvi);
        // Tail values should all be > 0 after smoothing settles.
        for (int i = r.Rvi.Length - 5; i < r.Rvi.Length; i++)
            Assert.True(r.Rvi[i] > 0, $"bar {i} RVI = {r.Rvi[i]}, expected > 0");
    }

    // ── Branch 8 — Sustained downtrend (Close < Open always) → RVI negative ──
    [Fact]
    public void Compute_Downtrend_RviNegative()
    {
        int n = 40;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            o[i] = 100 - i;
            c[i] = o[i] - 0.8;
            h[i] = o[i] + 0.2;
            l[i] = o[i] - 1.0;
        }
        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(o, h, l, c), period: 5).Compute();
        Assert.NotEmpty(r.Rvi);
        for (int i = r.Rvi.Length - 5; i < r.Rvi.Length; i++)
            Assert.True(r.Rvi[i] < 0, $"bar {i} RVI = {r.Rvi[i]}, expected < 0");
    }

    // ── Branch 9 — Den == 0 guard: tested via Branch 5 (flat OHLC) + this targeted case ──
    [Fact]
    public void Compute_ZeroRangeWindow_GuardFiresNoNaN()
    {
        // Constant HL (zero range) everywhere, but Close drifts. DenSmooth stays 0.
        int n = 30;
        var h = Enumerable.Repeat(100.0, n).ToArray();
        var l = Enumerable.Repeat(100.0, n).ToArray();
        var o = Enumerable.Repeat(100.0, n).ToArray();
        var c = Enumerable.Range(0, n).Select(i => 100.0 + 0.01 * i).ToArray();
        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(o, h, l, c), period: 5).Compute();
        // All Den = 0 → RVI forced to 0 by guard → Signal = 0.
        Assert.All(r.Rvi, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 10 — Normal multi-bar path — output bounded ──
    [Fact]
    public void Compute_RandomSeries_StaysFinite()
    {
        var rng = new Random(42);
        int n = 60;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + rng.NextDouble() * 10;
            o[i] = mid;
            c[i] = mid + (rng.NextDouble() - 0.5) * 2;
            h[i] = Math.Max(o[i], c[i]) + rng.NextDouble();
            l[i] = Math.Min(o[i], c[i]) - rng.NextDouble();
        }
        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(o, h, l, c), period: 10).Compute();
        r.Rvi.AssertAllFinite();
        r.Signal.AssertAllFinite();
    }

    [Fact]
    public void ConstructorLabel_IncludesPeriod()
    {
        var a = Enumerable.Repeat(100.0, 10).ToArray();
        var r = new RelativeVigorIndex(OhlcvBars.Ohlc(a, a, a, a), period: 10);
        Assert.Equal("RVI(10)", r.Label);
    }

    // ── RviResult record struct — deconstruction works ──
    [Fact]
    public void RviResult_Deconstructs()
    {
        double[] rvi = [0.1, 0.2];
        double[] sig = [0.05, 0.15];
        var result = new RviResult(rvi, sig);
        var (a, b) = result;
        Assert.Same(rvi, a);
        Assert.Same(sig, b);
    }
}
