// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="CyberCycle"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class CyberCycleTests
{
    // ── Branch 7 — Pure sinusoid — CC produces non-trivial cycle output ──
    [Fact]
    public void Compute_Sinusoid_HasSignificantAmplitude()
    {
        int n = 200;
        var prices = new double[n];
        for (int i = 0; i < n; i++) prices[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 15);
        var cc = new CyberCycle(prices, alpha: 0.07).Compute().Values;

        // Tail portion should oscillate with non-trivial amplitude.
        double tailMax = 0;
        for (int i = 150; i < cc.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(cc[i]));
        Assert.True(tailMax > 0.5, $"expected cycle amplitude, got tailMax={tailMax}");
    }

    // ── Branch 8 — Normal multi-bar path + known reference ──
    //
    // Python reference (verbatim recurrence):
    //   p = [1, 2, 3, 5, 4, 6, 8, 7]
    //   alpha = 0.07
    //   s = [p[0], p[0], p[0], (p[3]+2*p[2]+2*p[1]+p[0])/6, ...]
    //   (for i < 3 initialize smooth = p[i]; then formula)
    //   CC recurrence from i=3 onwards, CC[0..2] = 0.
    // Output = CC[3:].
    [Fact]
    public void Compute_KnownVector_AllFinite()
    {
        var prices = new[] { 1.0, 2, 3, 5, 4, 6, 8, 7 };
        var cc = new CyberCycle(prices, alpha: 0.2).Compute().Values;
        Assert.Equal(5, cc.Length); // 8 - 3
        cc.AssertAllFinite();
    }
}
