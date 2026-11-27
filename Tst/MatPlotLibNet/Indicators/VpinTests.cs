// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Vpin"/> (BVC approximation). Its arithmetic is held here against reference values; what it keeps with
/// every indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see
/// cref="IndicatorContractTests"/>.</summary>
public class VpinTests
{
    /// <summary>The two degenerate inputs read zero, never NaN. With every volume 0 the total bucket volume is 0, which the division-by-zero
    /// guard answers with 0 (the close varies, so σ_r is defined and this is not the next row). With closes that double every bar the
    /// log-returns are exactly equal in IEEE-754 (a ratio of 2 is exact), so σ_r is 0 in every sigma window, which the 50/50 split
    /// convention answers with 0 for that bucket.</summary>
    [Theory]
    [InlineData(new[] { 100.0, 101, 102, 103, 104, 105, 106 }, new[] { 0.0, 0, 0, 0, 0, 0, 0 })]
    [InlineData(new[] { 100.0, 200, 400, 800, 1600, 3200, 6400 }, new[] { 1000.0, 1000, 1000, 1000, 1000, 1000, 1000 })]
    public void Compute_ZeroBucketVolume_OrIdenticalReturnsInSigmaWindow_ReturnsZero(double[] close, double[] volume)
    {
        var r = new Vpin(OhlcvBars.Cv(close, volume), bucketPeriod: 3, sigmaPeriod: 3).Compute().Values;

        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.Equal(0.0, v, precision: 12));
    }

    // ── Branch 9 — Normal multi-bar path ──
    [Fact]
    public void Compute_VariablePrices_ProducesFiniteOutputInZeroOne()
    {
        // Mixed-direction moves produce non-degenerate BVC classification.
        var close = new double[20];
        var vol = new double[20];
        var rng = new Random(42);
        close[0] = 100;
        for (int i = 1; i < 20; i++)
            close[i] = close[i - 1] * (1 + (rng.NextDouble() - 0.5) * 0.02);
        for (int i = 0; i < 20; i++) vol[i] = 1000 + rng.NextDouble() * 500;

        var vpin = new Vpin(OhlcvBars.Cv(close, vol), bucketPeriod: 5, sigmaPeriod: 5);
        var r = vpin.Compute().Values;
        Assert.NotEmpty(r);
        Assert.All(r, v => Assert.InRange(v, 0.0, 1.0));
    }
}
