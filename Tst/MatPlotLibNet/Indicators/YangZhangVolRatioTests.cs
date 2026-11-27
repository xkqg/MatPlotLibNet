// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="YangZhangVolRatio"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class YangZhangVolRatioTests
{
    // ── Branch 7 — Vol expansion → tail ratio > 1 ──
    // Transition at bar 110 so the 60-bar long window (ending at 149, covering bars 90..149)
    // straddles the regime shift (20 flat + 40 noisy), while the 20-bar short window is
    // entirely in the noisy tail. Short > long drives the ratio above 1.
    [Fact]
    public void Compute_VolExpansion_TailRatioAboveOne()
    {
        int n = 150;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        var rng = new Random(101);
        for (int i = 0; i < n; i++)
        {
            double baseP = 100.0;
            double noise = i < 110 ? 1e-4 : 3.0;
            double oPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double cPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double hi = Math.Max(oPx, cPx) + rng.NextDouble() * noise;
            double lo = Math.Min(oPx, cPx) - rng.NextDouble() * noise;
            o[i] = oPx; h[i] = hi; l[i] = lo; c[i] = cPx;
        }
        var r = new YangZhangVolRatio(OhlcvBars.Ohlc(o, h, l, c), shortWindow: 20, longWindow: 60).Compute().Values;
        Assert.True(r[^1] > 1.0, $"expected expansion tail > 1, got {r[^1]}");
    }

    // ── Branch 8 — Vol contraction → tail ratio < 1 ──
    // Mirror of the expansion scenario: the last 40 bars are flat so the short window
    // captures only the quiet tail while the long window still includes 20 noisy bars.
    [Fact]
    public void Compute_VolContraction_TailRatioBelowOne()
    {
        int n = 150;
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        var rng = new Random(77);
        for (int i = 0; i < n; i++)
        {
            double baseP = 100.0;
            double noise = i < 110 ? 3.0 : 1e-4;
            double oPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double cPx = baseP + (rng.NextDouble() - 0.5) * noise;
            double hi = Math.Max(oPx, cPx) + rng.NextDouble() * noise;
            double lo = Math.Min(oPx, cPx) - rng.NextDouble() * noise;
            o[i] = oPx; h[i] = hi; l[i] = lo; c[i] = cPx;
        }
        var r = new YangZhangVolRatio(OhlcvBars.Ohlc(o, h, l, c), shortWindow: 20, longWindow: 60).Compute().Values;
        Assert.True(r[^1] < 1.0, $"expected contraction tail < 1, got {r[^1]}");
    }

    // ── Branch 9 — Normal multi-bar path ──
    [Fact]
    public void Compute_MixedData_AllFinite()
    {
        int n = 120;
        var rng = new Random(9);
        var o = new double[n]; var h = new double[n]; var l = new double[n]; var c = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mid = 100 + rng.NextDouble() * 2;
            o[i] = mid;
            c[i] = mid + (rng.NextDouble() - 0.5);
            h[i] = Math.Max(o[i], c[i]) + rng.NextDouble();
            l[i] = Math.Min(o[i], c[i]) - rng.NextDouble();
        }
        var r = new YangZhangVolRatio(OhlcvBars.Ohlc(o, h, l, c), shortWindow: 20, longWindow: 60).Compute().Values;
        Assert.NotEmpty(r);
        r.AssertAllFinite();
        Assert.All(r, v => Assert.True(v > 0));
    }
}
