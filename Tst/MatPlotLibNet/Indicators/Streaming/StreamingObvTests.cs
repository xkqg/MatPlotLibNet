// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Coverage uplift Batch D — full coverage for <see cref="StreamingObv"/> (was 0%).
/// Verifies the cumulative OBV formula across both <c>AppendWithVolume</c> and the
/// volume-less <c>AppendCandle</c> code paths, including the equal-close branch and
/// ring-buffer eviction on capacity overflow.</summary>
public sealed class StreamingObvTests
{
    [Fact]
    public void Construction_DefaultsAreCorrect()
    {
        var obv = new StreamingObv();
        Assert.Equal(1, obv.WarmupPeriod);
        Assert.Equal("OBV", obv.Label);
        Assert.Single(obv.OutputSeries);
    }

    [Fact]
    public void Construction_RespectsCapacity()
    {
        var obv = new StreamingObv(capacity: 32);
        Assert.Equal(32, obv.OutputSeries[0].Capacity);
    }

    [Fact]
    public void FirstBar_OutputIsZero_AndWarmupSatisfied()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50);

        Assert.True(obv.IsWarmedUp); // WarmupPeriod = 1
        Assert.Equal(0.0, obv.GetLatest());
    }

    [Fact]
    public void AppendWithVolume_RisingClose_AddsVolume()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50); // OBV=0  (seed)
        obv.AppendWithVolume(102, 30); // OBV=0+30 = 30
        Assert.Equal(30.0, obv.GetLatest());
    }

    [Fact]
    public void AppendWithVolume_FallingClose_SubtractsVolume()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50); // OBV=0
        obv.AppendWithVolume(102, 30); // OBV=30
        obv.AppendWithVolume(101, 20); // 101<102 → OBV = 30-20 = 10
        Assert.Equal(10.0, obv.GetLatest());
    }

    [Fact]
    public void AppendWithVolume_EqualClose_LeavesObvUnchanged()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50);
        obv.AppendWithVolume(102, 30); // OBV=30
        obv.AppendWithVolume(102, 99); // equal → unchanged
        Assert.Equal(30.0, obv.GetLatest());
    }

    [Fact]
    public void ManualSequence_MatchesHandComputed()
    {
        // Hand-walked per the formula in the source:
        //   bar 1 (100, 50): OBV = 0
        //   bar 2 (102, 30): up   →  0 + 30 = 30
        //   bar 3 (101, 20): down → 30 - 20 = 10
        //   bar 4 (101, 40): eq   → 10
        //   bar 5 (105, 25): up   → 10 + 25 = 35
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50);
        obv.AppendWithVolume(102, 30);
        obv.AppendWithVolume(101, 20);
        obv.AppendWithVolume(101, 40);
        obv.AppendWithVolume(105, 25);

        var snap = obv.OutputSeries[0].CreateSnapshot();
        Assert.Equal(new[] { 0.0, 30.0, 10.0, 10.0, 35.0 }, snap.YData);
    }

    [Fact]
    public void AppendCandle_WithoutVolume_UsesUnitDelta()
    {
        // When AppendCandle is called the per-bar volume defaults to 0 → the formula falls back
        // to a +/-1 unit step (see source: `_lastVolume > 0 ? _lastVolume : 1`).
        var obv = new StreamingObv();
        obv.AppendCandle(new OhlcBar(100, 100, 100, 100)); // OBV=0
        obv.AppendCandle(new OhlcBar(101, 101, 101, 101)); // up   → +1
        obv.AppendCandle(new OhlcBar(100, 100, 100,  99)); // down → -1 → 0
        obv.AppendCandle(new OhlcBar(102, 102, 102, 105)); // up   → +1 → 1

        var snap = obv.OutputSeries[0].CreateSnapshot();
        Assert.Equal(new[] { 0.0, 1.0, 0.0, 1.0 }, snap.YData);
    }

    [Fact]
    public void RingBuffer_OldestEvicted_OnCapacityOverflow()
    {
        var obv = new StreamingObv(capacity: 3);
        // Push 5 rising bars — buffer should hold the last 3 only.
        for (int i = 0; i < 5; i++) obv.AppendWithVolume(100 + i, 10);

        var snap = obv.OutputSeries[0].CreateSnapshot();
        Assert.Equal(3, snap.YData.Length);
        // OBV after 5 rising bars: 0, 10, 20, 30, 40 → buffer keeps {20, 30, 40}
        Assert.Equal(new[] { 20.0, 30.0, 40.0 }, snap.YData);
    }

    [Fact]
    public void GetLatest_BeforeAnyAppend_ReturnsNaN()
    {
        var obv = new StreamingObv();
        Assert.True(double.IsNaN(obv.GetLatest()));
    }

    [Fact]
    public void ProcessedCount_IncrementsPerBar()
    {
        var obv = new StreamingObv();
        Assert.Equal(0, obv.ProcessedCount);
        obv.AppendWithVolume(100, 5);
        obv.AppendWithVolume(101, 5);
        obv.AppendWithVolume(102, 5);
        Assert.Equal(3, obv.ProcessedCount);
    }
}
