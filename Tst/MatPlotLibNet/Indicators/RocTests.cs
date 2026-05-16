// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>v1.11.0 — Verifies <see cref="Roc"/> (Rate of Change) indicator.</summary>
public class RocTests
{
    // ── Length guards ─────────────────────────────────────────────────────────

    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        double[] result = new Roc([], 3).Compute();
        Assert.Empty(result);
    }

    [Fact]
    public void Compute_LengthEqualToLookback_ReturnsEmpty()
    {
        // Exactly at the lookback boundary: n == lookback → no valid ROC value.
        double[] result = new Roc([1.0, 2.0, 3.0], 3).Compute();
        Assert.Empty(result);
    }

    [Fact]
    public void Compute_LengthBelowLookback_ReturnsEmpty()
    {
        double[] result = new Roc([1.0, 2.0], 5).Compute();
        Assert.Empty(result);
    }

    // ── Output length ─────────────────────────────────────────────────────────

    [Fact]
    public void Compute_OutputLengthEqualsInputMinusLookback()
    {
        double[] prices = [10.0, 11.0, 12.0, 13.0, 14.0];
        double[] result = new Roc(prices, 2).Compute();
        Assert.Equal(prices.Length - 2, result.Length);
    }

    // ── Correctness ───────────────────────────────────────────────────────────

    [Fact]
    public void Compute_Lookback1_ReturnsSimplePctChange()
    {
        // prices = [100, 110] → ROC(1) at index 1 = 110/100 - 1 = 0.10
        double[] result = new Roc([100.0, 110.0], 1).Compute();
        Assert.Single(result);
        Assert.Equal(0.10, result[0], precision: 10);
    }

    [Fact]
    public void Compute_Lookback2_HandComputedValues()
    {
        // prices = [100, 110, 115, 121]
        // ROC(2): [115/100 - 1, 121/110 - 1] = [0.15, 0.10]
        double[] prices = [100.0, 110.0, 115.0, 121.0];
        double[] result = new Roc(prices, 2).Compute();
        Assert.Equal(2, result.Length);
        Assert.Equal(0.15,  result[0], precision: 10);
        Assert.Equal(0.10,  result[1], precision: 10);
    }

    [Fact]
    public void Compute_NegativeReturn_IsNegative()
    {
        // 100 → 90: ROC = -0.10
        double[] result = new Roc([100.0, 90.0], 1).Compute();
        Assert.Equal(-0.10, result[0], precision: 10);
    }

    [Fact]
    public void Compute_ZeroPriceDenominator_ReturnsNaN()
    {
        // Lookback price is 0 → undefined rate-of-change.
        double[] result = new Roc([0.0, 5.0], 1).Compute();
        Assert.Single(result);
        Assert.True(double.IsNaN(result[0]));
    }

    // ── Label ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Label_ContainsPeriod()
    {
        var roc = new Roc([1.0, 2.0, 3.0], 2);
        Assert.Contains("2", roc.Label);
    }

    // ── Apply ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_AddsLineSeriesWithCorrectLength()
    {
        var axes = new Axes();
        new Roc([100.0, 110.0, 115.0, 121.0], 2).Apply(axes);
        Assert.Single(axes.Series);
        var s = Assert.IsType<LineSeries>(axes.Series[0]);
        // 4 prices, lookback 2 → 2 ROC values → X array length 2
        Assert.Equal(2, s.XData.Length);
    }
}
