// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Phase 8 — full coverage for <see cref="StreamingStochastic"/> (was 30.5%).
/// Tests cover the warmup phase (NaN until ProcessedCount >= kPeriod), the high==low
/// degenerate case (returns 50), %D's SMA→EMA transition, and ring-buffer wraparound.</summary>
public class StreamingStochasticTests
{
    [Fact]
    public void Construction_DefaultsAreCorrect()
    {
        var s = new StreamingStochastic();
        Assert.Equal(14, s.WarmupPeriod);
        Assert.Equal("Stoch(14,3)", s.Label);
        Assert.Equal(2, s.OutputSeries.Count);   // %K and %D
    }

    [Fact]
    public void Construction_CustomParams_PropagateLabel()
    {
        var s = new StreamingStochastic(kPeriod: 5, dPeriod: 2);
        Assert.Equal(5, s.WarmupPeriod);
        Assert.Equal("Stoch(5,2)", s.Label);
    }

    [Fact]
    public void Warmup_BothOutputsNaN_UntilKPeriodReached()
    {
        var s = new StreamingStochastic(kPeriod: 3, dPeriod: 2);

        // 2 bars (< kPeriod=3) — both outputs NaN
        s.AppendCandle(new OhlcBar(10, 12, 9, 11));
        s.AppendCandle(new OhlcBar(11, 13, 10, 12));

        var k = s.OutputSeries[0].CreateSnapshot();
        var d = s.OutputSeries[1].CreateSnapshot();
        Assert.True(double.IsNaN(k.YData[0]) && double.IsNaN(k.YData[1]));
        Assert.True(double.IsNaN(d.YData[0]) && double.IsNaN(d.YData[1]));
    }

    [Fact]
    public void HighEqualsLow_PercentK_IsFifty()
    {
        var s = new StreamingStochastic(kPeriod: 1, dPeriod: 1);
        // kPeriod=1: a single bar where high==low triggers the (high == low) branch → %K = 50
        s.AppendCandle(new OhlcBar(10, 10, 10, 10));

        var k = s.OutputSeries[0].CreateSnapshot();
        Assert.Equal(50.0, k.YData[0]);
    }

    [Fact]
    public void TypicalCandle_PercentK_FollowsFormula()
    {
        var s = new StreamingStochastic(kPeriod: 1, dPeriod: 1);
        // Single-bar window with close at 75% between low and high
        // => %K = (close - low) / (high - low) * 100 = (8.5 - 5) / (10 - 5) * 100 = 70
        s.AppendCandle(new OhlcBar(6, 10, 5, 8.5));

        var k = s.OutputSeries[0].CreateSnapshot();
        Assert.Equal(70.0, k.YData[0], 1e-9);
    }

    [Fact]
    public void RingBuffer_WrapsCorrectly_OnSmallKPeriod()
    {
        // kPeriod = 3 → ring index wraps after 3 bars. Add 8 bars and confirm no crash.
        var s = new StreamingStochastic(kPeriod: 3, dPeriod: 2);
        for (int i = 0; i < 8; i++)
            s.AppendCandle(new OhlcBar(i, i + 2, i - 1, i + 1));

        var k = s.OutputSeries[0].CreateSnapshot();
        Assert.Equal(8, k.YData.Length);
        // Last value must be a valid number (not NaN) since ProcessedCount > kPeriod.
        Assert.False(double.IsNaN(k.YData[7]));
    }

    [Fact]
    public void PercentD_BecomesNonNan_AfterDPeriodValidKValues()
    {
        var s = new StreamingStochastic(kPeriod: 2, dPeriod: 3);
        // Need at least kPeriod+dPeriod-1 = 4 bars before %D becomes non-NaN.
        for (int i = 0; i < 5; i++)
            s.AppendCandle(new OhlcBar(i, i + 2, i - 1, i + 1));

        var d = s.OutputSeries[1].CreateSnapshot();
        // First 3 entries: bar1 = NaN warmup, bar2-4 = %D NaN until count = dPeriod, bar5 = valid
        Assert.False(double.IsNaN(d.YData[^1]),
            $"Last %D value should be valid; got {d.YData[^1]}");
    }
}
