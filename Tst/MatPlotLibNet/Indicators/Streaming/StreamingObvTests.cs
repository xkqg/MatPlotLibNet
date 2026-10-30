// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The cumulative OBV formula across <c>AppendWithVolume</c> and the volume-less <c>AppendCandle</c> path,
/// including the equal-close branch and ring-buffer eviction on capacity overflow. The sum is the batch <see cref="Obv"/>:
/// the first bar opens it with its own volume, and a bar without a volume makes it NaN.</summary>
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
    public void FirstBar_OpensTheSumWithItsVolume_AndWarmupSatisfied()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50);

        Assert.True(obv.IsWarmedUp); // WarmupPeriod = 1
        Assert.Equal(50.0, obv.GetLatest());
    }

    [Fact]
    public void AppendWithVolume_RisingClose_AddsVolume()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50); // OBV=50 (the first volume)
        obv.AppendWithVolume(102, 30); // OBV=50+30 = 80
        Assert.Equal(80.0, obv.GetLatest());
    }

    [Fact]
    public void AppendWithVolume_FallingClose_SubtractsVolume()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50); // OBV=50
        obv.AppendWithVolume(102, 30); // OBV=80
        obv.AppendWithVolume(101, 20); // 101<102 → OBV = 80-20 = 60
        Assert.Equal(60.0, obv.GetLatest());
    }

    [Fact]
    public void AppendWithVolume_EqualClose_LeavesObvUnchanged()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50);
        obv.AppendWithVolume(102, 30); // OBV=80
        obv.AppendWithVolume(102, 99); // equal → unchanged
        Assert.Equal(80.0, obv.GetLatest());
    }

    [Fact]
    public void ManualSequence_MatchesHandComputed()
    {
        // Hand-walked per the formula in the source:
        //   bar 1 (100, 50): OBV = 50 (opens with its volume)
        //   bar 2 (102, 30): up   → 50 + 30 = 80
        //   bar 3 (101, 20): down → 80 - 20 = 60
        //   bar 4 (101, 40): eq   → 60
        //   bar 5 (105, 25): up   → 60 + 25 = 85
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50);
        obv.AppendWithVolume(102, 30);
        obv.AppendWithVolume(101, 20);
        obv.AppendWithVolume(101, 40);
        obv.AppendWithVolume(105, 25);

        var snap = obv.OutputSeries[0].CreateSnapshot();
        Assert.Equal(new[] { 50.0, 80.0, 60.0, 60.0, 85.0 }, snap.YData);
    }

    [Fact]
    public void AppendCandle_WithoutVolume_GivesNaN_NotAUnitStep()
    {
        // A candle carries no volume. It used to count as a step of 1, which is a number nobody traded; the sum of a
        // volume that is not known is not known.
        var obv = new StreamingObv();
        obv.AppendCandle(new OhlcBar(100, 100, 100, 100));
        obv.AppendCandle(new OhlcBar(101, 101, 101, 101));
        obv.AppendCandle(new OhlcBar(100, 100, 100,  99));

        var snap = obv.OutputSeries[0].CreateSnapshot();
        Assert.Equal(3, snap.YData.Length);
        Assert.All(snap.YData, v => Assert.True(double.IsNaN(v)));
    }

    [Fact]
    public void AVolumeThatIsGiven_DoesNotLeakIntoTheNextBarThatHasNone()
    {
        var obv = new StreamingObv();
        obv.AppendWithVolume(100, 50);
        obv.AppendWithVolume(101, 30);                          // 80
        obv.AppendCandle(new OhlcBar(102, 102, 102, 102));      // no volume of its own: NaN, not 80 + 30

        Assert.True(double.IsNaN(obv.GetLatest()));
    }

    [Fact]
    public void AppendBar_TakesTheCloseAndTheVolumeOfTheBar()
    {
        var obv = new StreamingObv();
        obv.AppendBar(new OhlcvBar(1, 9, 0, 100, 50));
        obv.AppendBar(new OhlcvBar(1, 9, 0, 102, 30));

        Assert.Equal([50.0, 80.0], obv.OutputSeries[0].CreateSnapshot().YData);
    }

    [Fact]
    public void RingBuffer_OldestEvicted_OnCapacityOverflow()
    {
        var obv = new StreamingObv(capacity: 3);
        // Push 5 rising bars — buffer should hold the last 3 only.
        for (int i = 0; i < 5; i++) obv.AppendWithVolume(100 + i, 10);

        var snap = obv.OutputSeries[0].CreateSnapshot();
        Assert.Equal(3, snap.YData.Length);
        // OBV after 5 rising bars: 10, 20, 30, 40, 50 → buffer keeps {30, 40, 50}
        Assert.Equal(new[] { 30.0, 40.0, 50.0 }, snap.YData);
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
