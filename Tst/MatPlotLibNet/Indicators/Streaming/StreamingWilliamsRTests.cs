// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Coverage uplift Batch D — full coverage for <see cref="StreamingWilliamsR"/> (was 0%).
/// Verifies the rolling high/low window, the high==low degenerate branch (returns -50),
/// hand-derived %R values across a small zig-zag price series, and ring-buffer wraparound.</summary>
public sealed class StreamingWilliamsRTests : OhlcStreamingIndicatorTests<StreamingWilliamsR>
{
    protected override StreamingWilliamsR CreateIndicator(int period, int capacity = 256)
        => new(period: period, capacity: capacity);

    [Fact]
    public void Construction_DefaultsAreCorrect()
    {
        var wr = new StreamingWilliamsR();
        Assert.Equal(14, wr.WarmupPeriod);
        Assert.Equal("W%R(14)", wr.Label);
        Assert.Single(wr.OutputSeries);
    }

    [Fact]
    public void Construction_CustomPeriod_PropagatesLabel()
    {
        var wr = new StreamingWilliamsR(period: 5);
        Assert.Equal(5, wr.WarmupPeriod);
        Assert.Equal("W%R(5)", wr.Label);
    }

    [Fact]
    public void HighEqualsLow_PercentR_IsMinusFifty()
    {
        // period=1: a single flat bar where high==low triggers the (high == low) branch → -50
        var wr = new StreamingWilliamsR(period: 1);
        wr.AppendCandle(new OhlcBar(10, 10, 10, 10));

        var snap = wr.OutputSeries[0].CreateSnapshot();
        Assert.Equal(-50.0, snap.YData[0]);
    }

    [Fact]
    public void CloseAtHigh_PercentR_IsZero()
    {
        // %R = (highest - close)/(highest - lowest) * -100. close == highest → 0.
        var wr = new StreamingWilliamsR(period: 1);
        wr.AppendCandle(new OhlcBar(5, 10, 5, 10));
        Assert.Equal(0.0, wr.OutputSeries[0].CreateSnapshot().YData[0], 1e-9);
    }

    [Fact]
    public void CloseAtLow_PercentR_IsMinusOneHundred()
    {
        // close == lowest → (high-low)/(high-low) * -100 = -100
        var wr = new StreamingWilliamsR(period: 1);
        wr.AppendCandle(new OhlcBar(5, 10, 5, 5));
        Assert.Equal(-100.0, wr.OutputSeries[0].CreateSnapshot().YData[0], 1e-9);
    }

    [Fact]
    public void ZigZagSequence_MatchesHandDerivedValues()
    {
        // bars (H, L, C):
        //   1 (12, 8,10), 2 (14, 9,13), 3 (13, 7, 8), 4 (15, 8,14), 5 (14, 6, 7)
        // period=3 → bars 1,2 are NaN (warmup).
        // bar 3: window = bars{1,2,3}. maxH=14, minL=7, close=8 → (14-8)/(14-7) * -100 = -600/7
        // bar 4: ring slot 0 = bar 4 → window = {bar4, bar2, bar3}. maxH=15, minL=7, close=14 → -1/8 * 100 = -12.5
        // bar 5: ring slot 1 = bar 5 → window = {bar4, bar5, bar3}. maxH=15, minL=6, close=7 → -8/9 * 100
        var wr = new StreamingWilliamsR(period: 3);
        foreach (var bar in StreamingTestData.ZigZagBars()) wr.AppendCandle(bar);

        var y = wr.OutputSeries[0].CreateSnapshot().YData;
        Assert.Equal(5, y.Length);
        Assert.True(double.IsNaN(y[0]));
        Assert.True(double.IsNaN(y[1]));
        Assert.Equal(-600.0 / 7.0, y[2], 1e-9);
        Assert.Equal(-12.5,        y[3], 1e-9);
        Assert.Equal(-800.0 / 9.0, y[4], 1e-9);
    }

    [Fact]
    public void FlatBars_HighEqualsLowAcrossWindow_ProducesMinusFifty()
    {
        // Every bar is identical → max(high) == min(low), producing the high==low branch.
        var wr = new StreamingWilliamsR(period: 3);
        foreach (var bar in StreamingTestData.FlatBars(5, price: 50)) wr.AppendCandle(bar);

        var y = wr.OutputSeries[0].CreateSnapshot().YData;
        Assert.True(double.IsNaN(y[0]));
        Assert.True(double.IsNaN(y[1]));
        Assert.Equal(-50.0, y[2]);
        Assert.Equal(-50.0, y[3]);
        Assert.Equal(-50.0, y[4]);
    }

    [Fact]
    public void RingBuffer_WrapsCorrectly_WhenBarsExceedPeriod()
    {
        // Push 8 bars at period 3 — the index wraps multiple times. Just verify shape + non-NaN tail.
        var wr = new StreamingWilliamsR(period: 3);
        for (int i = 0; i < 8; i++)
            wr.AppendCandle(new OhlcBar(i, i + 2, i - 1, i + 1));

        var y = wr.OutputSeries[0].CreateSnapshot().YData;
        Assert.Equal(8, y.Length);
        Assert.False(double.IsNaN(y[7]));
        Assert.InRange(y[7], -100, 0);
    }

}
