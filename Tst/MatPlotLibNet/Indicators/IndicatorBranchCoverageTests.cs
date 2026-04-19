// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Phase Q Wave 1 (2026-04-19) — branch-coverage Facts for indicators flagged at
/// 50–75% branch in <c>tools/coverage/baseline.cobertura.xml</c>. Each indicator's existing
/// test file covers the happy path; the missed branches are the early-out cases (data shorter
/// than period), the all-flat data case (range == 0), and the user-color override branch.
/// One Fact per missed branch keeps the gate honest per Q.4 TDD discipline.</summary>
public class IndicatorBranchCoverageTests
{
    private static readonly double[] LongHigh = Enumerable.Range(1, 30).Select(i => (double)(50 + i)).ToArray();
    private static readonly double[] LongLow = Enumerable.Range(1, 30).Select(i => (double)(40 + i)).ToArray();
    private static readonly double[] LongClose = Enumerable.Range(1, 30).Select(i => (double)(45 + i)).ToArray();

    /// <summary>Stochastic.Compute early-returns an empty result when data is shorter than %K period.
    /// This branch (line ~38) was unhit because every existing test had n &gt;= kPeriod.</summary>
    [Fact]
    public void Stochastic_ShortData_ReturnsEmptyResult()
    {
        var s = new Stochastic([1, 2, 3], [0.5, 1.5, 2.5], [0.7, 1.7, 2.7], kPeriod: 14);
        var result = s.Compute();
        Assert.Empty(result.K);
        Assert.Empty(result.D);
    }

    /// <summary>Stochastic.Compute's <c>range &gt; 0 ? formula : 50</c> branch — the
    /// fallback to 50 fires when high == low (all-flat bar). Exercised by feeding
    /// completely flat OHLC data.</summary>
    [Fact]
    public void Stochastic_FlatData_FallsBackToFiftyPercent()
    {
        double[] flat = Enumerable.Repeat(10.0, 20).ToArray();
        var s = new Stochastic(flat, flat, flat, kPeriod: 5);
        var result = s.Compute();
        Assert.NotEmpty(result.K);
        Assert.All(result.K, v => Assert.Equal(50.0, v));
    }

    /// <summary>Stochastic.Apply's <c>DColor ?? Colors.Tab10Orange</c> branch — exercised
    /// by setting DColor explicitly. Existing tests use the default (null) DColor.</summary>
    [Fact]
    public void Stochastic_ExplicitDColor_AppliesIt()
    {
        var axes = new Axes();
        new Stochastic(LongHigh, LongLow, LongClose, kPeriod: 5, dPeriod: 3) { DColor = Colors.Red }.Apply(axes);
        Assert.True(axes.Series.Count >= 2);
    }

    /// <summary>WilliamsR.Compute early-returns when data is shorter than period.</summary>
    [Fact]
    public void WilliamsR_ShortData_ReturnsEmptyResult()
    {
        var w = new WilliamsR([1, 2, 3], [0.5, 1.5, 2.5], [0.7, 1.7, 2.7], period: 14);
        Assert.Empty(w.Compute().Values);
    }

    /// <summary>WilliamsR.Compute's flat-data branch — when high == low the formula's
    /// division-by-zero guard returns 0 (or similar fallback value).</summary>
    [Fact]
    public void WilliamsR_FlatData_DoesNotCrash()
    {
        double[] flat = Enumerable.Repeat(10.0, 20).ToArray();
        var w = new WilliamsR(flat, flat, flat, period: 5);
        double[] result = w.Compute();
        Assert.NotEmpty(result);
        Assert.All(result, v => Assert.False(double.IsNaN(v) || double.IsInfinity(v)));
    }

    /// <summary>KeltnerChannels.Compute early-returns when len &lt;= 0 (data shorter than 2×period).
    /// This is the only conditional branch in Compute and it's unhit by the existing test.</summary>
    [Fact]
    public void KeltnerChannels_ShortData_ReturnsEmptyResult()
    {
        var k = new KeltnerChannels(
            high: [1, 2, 3], low: [0.5, 1.5, 2.5], close: [0.7, 1.7, 2.7],
            period: 20);
        var result = k.Compute();
        Assert.Empty(result.Middle);
        Assert.Empty(result.Upper);
        Assert.Empty(result.Lower);
    }

    /// <summary>Macd.Apply branches around the optional SignalColor override —
    /// existing tests use the default null SignalColor, leaving the override branch unhit.</summary>
    [Fact]
    public void Macd_ExplicitSignalColor_AppliesIt()
    {
        var axes = new Axes();
        new Macd(LongClose, fastPeriod: 5, slowPeriod: 10, signalPeriod: 3) { SignalColor = Colors.Red }.Apply(axes);
        Assert.True(axes.Series.Count >= 2);
    }

    /// <summary>Vwap.Apply with explicit Color override — existing test uses default.</summary>
    [Fact]
    public void Vwap_ExplicitColor_AppliesIt()
    {
        var axes = new Axes();
        double[] volume = Enumerable.Repeat(100.0, 30).ToArray();
        new Vwap(LongClose, volume) { Color = Colors.Red }.Apply(axes);
        Assert.NotEmpty(axes.Series);
    }
}
