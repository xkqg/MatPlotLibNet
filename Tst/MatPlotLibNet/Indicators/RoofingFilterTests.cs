// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="RoofingFilter"/>. Its arithmetic is held here against reference values; what it keeps with every indicator
/// (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class RoofingFilterTests
{
    // ── Branch 3 — Length == 3 → 3 outputs (filters produce full-length output) ──
    [Fact]
    public void Compute_LengthThree_ReturnsThreeValues()
    {
        var r = new RoofingFilter([100.0, 101, 102]).Compute().Values;
        Assert.Equal(3, r.Length);
        r.AssertAllFinite();
    }

    // ── Branch 8 — Pure HF noise — HP passes it, LP attenuates it → small output ──
    [Fact]
    public void Compute_HighFrequencyNoise_StrongAttenuation()
    {
        var prices = new double[200];
        for (int i = 0; i < 200; i++) prices[i] = 100 + (i % 2 == 0 ? 1.0 : -1.0);
        var r = new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Compute().Values;
        double tailMax = 0;
        for (int i = 100; i < r.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        // Alternating ±1 is well above the 10-period LP cutoff → strongly attenuated.
        Assert.True(tailMax < 0.5, $"tailMax={tailMax}; HF should attenuate");
    }

    // ── Branch 9 — Pure LF trend — HP removes it → output near 0 ──
    [Fact]
    public void Compute_LowFrequencyTrend_StrongAttenuation()
    {
        var prices = new double[300];
        for (int i = 0; i < 300; i++) prices[i] = 100 + 0.05 * i;
        var r = new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Compute().Values;
        double tailMax = 0;
        for (int i = 200; i < r.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax < 0.1, $"tailMax={tailMax}; slow trend should attenuate");
    }

    // ── Branch 10 — Target-frequency sinusoid — output has non-trivial amplitude ──
    [Fact]
    public void Compute_TargetFrequencySinusoid_AmplitudePreserved()
    {
        // Period between lpPeriod (10) and hpPeriod (48) — e.g., 20 — sits in the passband.
        var prices = new double[300];
        for (int i = 0; i < 300; i++) prices[i] = 100 + 5 * Math.Sin(2 * Math.PI * i / 20);
        var r = new RoofingFilter(prices, hpPeriod: 48, lpPeriod: 10).Compute().Values;
        double tailMax = 0;
        for (int i = 200; i < r.Length; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax > 0.5, $"tailMax={tailMax}; target-band signal should pass");
    }
}
