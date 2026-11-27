// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="CorwinSchultz"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class CorwinSchultzTests
{
    // ── Branch 8 — Negative α → S clamped to 0 (sustained drift across all bars) ──
    [Fact]
    public void Compute_SustainedDrift_ClampsToZero()
    {
        // Both per-bar α values go negative → per-bar S clamps to 0 → rolling mean = 0.
        var cs = new CorwinSchultz(
            OhlcvBars.Hl([110.0, 130, 150], [90.0, 110, 130]), period: 2);
        var r = cs.Compute().Values;
        Assert.Single(r);
        Assert.Equal(0.0, r[0], precision: 12);
    }

    // ── Branch 10 — Normal multi-bar path (reference vector with non-zero S) ──
    // Hand-derived closed form for constant H/L: α = |ln(H/L)|.
    //   S = 2·(e^α − 1) / (1 + e^α) = 2·tanh(α/2)
    // H = 100.5, L = 99.5  → α = ln(100.5/99.5) ≈ 0.010001 → S ≈ 0.01000066.
    [Fact]
    public void Compute_ConstantHL_MatchesClosedForm()
    {
        var cs = new CorwinSchultz(
            OhlcvBars.Hl(
                [100.5, 100.5, 100.5, 100.5],
                [99.5, 99.5, 99.5, 99.5]),
            period: 2);
        var r = cs.Compute().Values;
        Assert.Equal(2, r.Length); // n - period = 4 - 2 = 2
        double alpha = Math.Abs(Math.Log(100.5 / 99.5));
        double expected = 2 * (Math.Exp(alpha) - 1) / (1 + Math.Exp(alpha));
        Assert.Equal(expected, r[0], precision: 10);
        Assert.Equal(expected, r[1], precision: 10);
    }
}
