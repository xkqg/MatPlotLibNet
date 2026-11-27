// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="AmihudIlliquidity"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class AmihudIlliquidityTests
{
    // Canonical vector (spec §1):
    //   close:  [100, 101, 103, 102, 105]
    //   volume: [1000, 1000, 500, 1500, 1000]
    //   period: 3
    // Python reference:
    //   import numpy as np
    //   c = np.array([100,101,103,102,105]); v = np.array([1000,1000,500,1500,1000])
    //   r = np.log(c[1:]/c[:-1]); illiq = np.abs(r)/(c[1:]*v[1:])
    //   out = [illiq[i:i+3].mean() for i in range(len(illiq)-2)]
    //   → out = [1.81010331e-07, 2.40194780e-07]
    private static readonly double[] RefClose = [100, 101, 103, 102, 105];
    private static readonly double[] RefVolume = [1000, 1000, 500, 1500, 1000];

    // ── Branch 6 — Zero volume on a bar → PositiveInfinity in that bar's contribution ──
    [Fact]
    public void Compute_ZeroVolume_ReturnsPositiveInfinity()
    {
        // Period=1, close=[100,101], volume=[1000,0]
        // Bar 1: |r|=0.00995, dollar_vol = 101*0 = 0 → infinity policy.
        var illiq = new AmihudIlliquidity(OhlcvBars.Cv([100.0, 101], [1000.0, 0]), period: 1);
        Assert.Equal(double.PositiveInfinity, illiq.Compute().Values[0]);
    }

    // ── Branch 9 — Normal multi-bar path (reference vector) ──
    [Fact]
    public void Compute_KnownVector_MatchesReference()
    {
        var illiq = new AmihudIlliquidity(OhlcvBars.Cv(RefClose, RefVolume), period: 3);
        var r = illiq.Compute().Values;
        Assert.Equal(2, r.Length);
        Assert.Equal(1.81010331e-7, r[0], precision: 12);
        Assert.Equal(2.40194780e-7, r[1], precision: 12);
    }
}
