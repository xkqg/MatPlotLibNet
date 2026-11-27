// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="AroonOscillator"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class AroonOscillatorTests
{
    // ── Branch 7 — Ties on highest high: most recent wins ──
    //   h = [100, 100, 100], l = [99, 99, 99], period = 2
    //   At output index 0 (bar t=2): window = bars [0..2], all H=100 → tie → most recent (bar 2).
    //   bars_since_high = 0 → Up = 100.
    //   L window: all L=99 → tie → most recent (bar 2). bars_since_low = 0 → Down = 100.
    //   Osc = 0.
    [Fact]
    public void Compute_HighTies_MostRecentWins()
    {
        double[] h = [100.0, 100, 100];
        double[] l = [99.0, 99, 99];
        var r = new AroonOscillator(OhlcvBars.Hl(h, l), period: 2).Compute().Values;
        Assert.Single(r);
        Assert.Equal(0.0, r[0], precision: 10);
    }

    // ── Branch 8 — Ties on lowest low: most recent wins ──
    //   Exercised together with Branch 7 in the flat-prices test above. Add a targeted
    //   case where H varies but L ties: low never changes, Up fluctuates, Down stays 100.
    [Fact]
    public void Compute_LowTies_DownAlwaysHundred()
    {
        // H rises, L stays flat → Down always 100 (most recent low = today).
        double[] h = [100.0, 101, 102, 103];
        double[] l = [99.0, 99, 99, 99];
        var r = new AroonOscillator(OhlcvBars.Hl(h, l), period: 3).Compute().Values;
        Assert.Single(r);
        // Bar t=3, window [0..3]:
        //   Highs = 100, 101, 102, 103 — newest is bar 3 → bars_since_high = 0 → Up = 100.
        //   Lows = all 99 — tie → most recent → bars_since_low = 0 → Down = 100.
        //   Osc = 0.
        Assert.Equal(0.0, r[0], precision: 10);
    }

    // ── Branch 10 — Monotonic rise → Osc near +100 ──
    [Fact]
    public void Compute_MonotonicRise_OscillatorNearHundred()
    {
        var rising = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        var r = new AroonOscillator(OhlcvBars.Hl(rising, rising), period: 5).Compute().Values;
        Assert.NotEmpty(r);
        // At first output (bar t=5), window [0..5]: high = bar 5, low = bar 0.
        // Up = 100*(5 − 0)/5 = 100; Down = 100*(5 − 5)/5 = 0; Osc = 100.
        Assert.Equal(100.0, r[0], precision: 10);
        Assert.All(r, v => Assert.Equal(100.0, v, precision: 10));
    }

    // ── Branch 11 — Monotonic fall → Osc near -100 ──
    [Fact]
    public void Compute_MonotonicFall_OscillatorNearMinusHundred()
    {
        var falling = Enumerable.Range(0, 20).Select(i => 120.0 - i).ToArray();
        var r = new AroonOscillator(OhlcvBars.Hl(falling, falling), period: 5).Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.Equal(-100.0, v, precision: 10));
    }
}
