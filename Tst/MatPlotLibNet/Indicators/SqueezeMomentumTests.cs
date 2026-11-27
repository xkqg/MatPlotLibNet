// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="SqueezeMomentum"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class SqueezeMomentumTests
{
    private static double[] Flat(int count, double value) =>
        Enumerable.Repeat(value, count).ToArray();

    // ── Branch 6 — Length == period + 2 → single output row ──
    [Fact]
    public void Compute_LengthAtPeriodPlusTwo_ReturnsSingleRow()
    {
        int p = 3;
        var h = new double[p + 2]; var l = new double[p + 2]; var c = new double[p + 2];
        for (int i = 0; i < p + 2; i++) { h[i] = 101 + i; l[i] = 99 + i; c[i] = 100 + i; }
        var sq = new SqueezeMomentum(OhlcvBars.Hlc(h, l, c), period: p);
        var r = sq.Compute();
        Assert.Single(r.Momentum);
        Assert.Single(r.SqueezeOn);
    }

    // ── Branch 8 — Flat HLC → stddev=0, TR=0 → squeezeOn=false (strict <), momentum=0 ──
    [Fact]
    public void Compute_FlatPrices_SqueezeOffAndMomentumZero()
    {
        int p = 20;
        var flat = Flat(22, 100.0);
        var sq = new SqueezeMomentum(OhlcvBars.Hlc(flat, flat, flat), period: p);
        var r = sq.Compute();
        Assert.NotEmpty(r.Momentum);
        Assert.All(r.SqueezeOn, v => Assert.False(v));
        Assert.All(r.Momentum, v => Assert.Equal(0.0, v, precision: 9));
    }

    // ── Branch 9 — Narrow-range window → BB inside KC → squeezeOn=true ──
    [Fact]
    public void Compute_NarrowRange_SqueezeOn()
    {
        // Tiny symmetric bar-to-bar variation keeps stddev of close small (BB narrow);
        // HL range is also small but kcMult=1.5 makes KC wider than BB.
        int p = 5;
        // Alternate close between 100.0 and 100.1 — very low volatility.
        double[] c = [100, 100.1, 100, 100.1, 100, 100.1, 100];
        double[] h = [100.05, 100.15, 100.05, 100.15, 100.05, 100.15, 100.05];
        double[] l = [99.95, 100.05, 99.95, 100.05, 99.95, 100.05, 99.95];
        var sq = new SqueezeMomentum(OhlcvBars.Hlc(h, l, c), period: p, bbMult: 2.0, kcMult: 3.0);
        var r = sq.Compute();
        Assert.NotEmpty(r.SqueezeOn);
        // With kcMult=3 (wide KC) and stable small oscillation → expect BB inside KC.
        Assert.Contains(r.SqueezeOn, v => v == true);
    }

    // ── Branch 10 — Transition: squeeze on → squeeze off ──
    [Fact]
    public void Compute_WideningVolatility_TransitionDetected()
    {
        int p = 4;
        // Tight, identical bars first (BB & KC both narrow; BB wider than KC thanks to bbMult=2
        // on stddev vs kcMult=1 on TR). Then a wide-close-swing block pushes BB outside KC.
        double[] h = [100.05, 100.05, 100.05, 100.05, 100.05, 100.05, 110, 115, 108, 113, 106, 112];
        double[] l = [99.95,  99.95,  99.95,  99.95,  99.95,  99.95,  90,  85,  92,  87,  94,  88];
        double[] c = [100,    100,    100,    100,    100,    100,    108, 88,  106, 89,  105, 90];
        var sq = new SqueezeMomentum(OhlcvBars.Hlc(h, l, c), period: p, bbMult: 2.0, kcMult: 1.0);
        var r = sq.Compute();
        Assert.Contains(r.SqueezeOn, v => v == true);
        Assert.Contains(r.SqueezeOn, v => v == false);
    }

    // ── Branch 11 — Pure uptrend → momentum > 0 ──
    [Fact]
    public void Compute_Uptrend_MomentumPositive()
    {
        int p = 5;
        double[] c = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var sq = new SqueezeMomentum(OhlcvBars.Hlc(h, l, c), period: p);
        var r = sq.Compute();
        Assert.All(r.Momentum, v => Assert.True(v > 0, $"expected momentum > 0, got {v}"));
    }

    // ── Branch 12 — Pure downtrend → momentum < 0 ──
    [Fact]
    public void Compute_Downtrend_MomentumNegative()
    {
        int p = 5;
        double[] c = Enumerable.Range(0, 20).Select(i => 120.0 - i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var sq = new SqueezeMomentum(OhlcvBars.Hlc(h, l, c), period: p);
        var r = sq.Compute();
        Assert.All(r.Momentum, v => Assert.True(v < 0, $"expected momentum < 0, got {v}"));
    }
}
