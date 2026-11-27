// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="EaseOfMovement"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class EaseOfMovementTests
{
    private static double[] Flat(double v, int n) => Enumerable.Repeat(v, n).ToArray();

    // ── Branches 6 and 7 — Zero volume (BoxRatio = 0) and zero range (H == L, BoxRatio = ∞): each guard gives EMV_1 = 0 ──
    [Theory]
    [InlineData(1.0, 0.0)]      // the high above the low, no volume
    [InlineData(0.0, 1000.0)]   // the high on the low, the midpoint still moving
    public void Compute_ADegenerateBar_GivesZero_NotNaNOrInfinity(double range, double volume)
    {
        int n = 30;
        var high = Enumerable.Range(0, n).Select(i => 100.0 + i).ToArray();
        var low = high.Select(h => h - range).ToArray();
        var emv = new EaseOfMovement(OhlcvBars.Hlv(high, low, Flat(volume, n)), period: 10).Compute().Values;
        Assert.All(emv, x => Assert.Equal(0.0, x, precision: 10));
    }

    // ── Branch 8 — Flat midpoints → MidpointMove = 0 → EMV = 0 ──
    [Fact]
    public void Compute_FlatMidpoints_ReturnsZero()
    {
        int n = 30;
        var emv = new EaseOfMovement(OhlcvBars.Hlv(Flat(100, n), Flat(99, n), Flat(1000, n)), period: 10)
            .Compute().Values;
        Assert.Equal(n - 10, emv.Length);
        Assert.All(emv, v => Assert.Equal(0.0, v, precision: 10));
    }

    // ── Branch 9 — Normal multi-bar path ──
    [Fact]
    public void Compute_RisingMidpoint_AllFinite()
    {
        int n = 40;
        var h = Enumerable.Range(0, n).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, n).Select(i => 99.0 + i).ToArray();
        var v = Flat(1000, n);
        var emv = new EaseOfMovement(OhlcvBars.Hlv(h, l, v), period: 14, scale: 1e6).Compute().Values;
        Assert.Equal(n - 14, emv.Length);
        emv.AssertAllFinite();
        // Rising midpoints + constant volume + constant range → positive EMV.
        Assert.True(emv[^1] > 0, $"expected positive EMV on rising midpoint, got {emv[^1]}");
    }
}
