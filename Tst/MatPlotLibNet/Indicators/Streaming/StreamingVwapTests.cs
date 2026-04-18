// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Coverage uplift Batch D — full coverage for <see cref="StreamingVwap"/> (was 0%).
/// Asserts the cumulative price-volume formula, the no-volume fallback (returns the spot
/// price when cumulative volume is zero), and ring-buffer wraparound on capacity overflow.</summary>
public sealed class StreamingVwapTests
{
    [Fact]
    public void Construction_DefaultsAreCorrect()
    {
        var vwap = new StreamingVwap();
        Assert.Equal(1, vwap.WarmupPeriod);
        Assert.Equal("VWAP", vwap.Label);
        Assert.Single(vwap.OutputSeries);
    }

    [Fact]
    public void Construction_CustomCapacity_PropagatesToOutputSeries()
    {
        var vwap = new StreamingVwap(capacity: 64);
        Assert.Equal(64, vwap.OutputSeries[0].Capacity);
    }

    [Fact]
    public void FirstBar_VwapEqualsPrice()
    {
        var vwap = new StreamingVwap();
        vwap.AppendWithVolume(100, 10);
        Assert.Equal(100.0, vwap.GetLatest(), 1e-9);
    }

    [Fact]
    public void AppendWithVolume_ManualSequence_MatchesFormula()
    {
        // Hand walk:
        //  bar 1 (100, 10):  sumPV=1000, sumV=10  → 100
        //  bar 2 (110, 20):  sumPV=3200, sumV=30  → 3200/30 = 106.6666...
        //  bar 3 (120, 30):  sumPV=6800, sumV=60  → 6800/60 = 113.3333...
        var vwap = new StreamingVwap();
        vwap.AppendWithVolume(100, 10);
        vwap.AppendWithVolume(110, 20);
        vwap.AppendWithVolume(120, 30);

        var snap = vwap.OutputSeries[0].CreateSnapshot();
        Assert.Equal(100.0, snap.YData[0], 1e-9);
        Assert.Equal(3200.0 / 30.0, snap.YData[1], 1e-9);
        Assert.Equal(6800.0 / 60.0, snap.YData[2], 1e-9);
    }

    [Fact]
    public void AppendWithVolume_ConstantPriceAndVolume_VwapEqualsPrice()
    {
        var vwap = new StreamingVwap();
        for (int i = 0; i < 10; i++) vwap.AppendWithVolume(50, 7);
        Assert.Equal(50.0, vwap.GetLatest(), 1e-9);
    }

    [Fact]
    public void AppendWithVolume_VwapWeightsLargerVolumesMore()
    {
        // Smaller-volume bar at 100, then huge-volume bar at 200 → result must skew toward 200.
        var vwap = new StreamingVwap();
        vwap.AppendWithVolume(100, 1);
        vwap.AppendWithVolume(200, 999);
        // (100*1 + 200*999) / 1000 = (100 + 199800) / 1000 = 199.9
        Assert.Equal(199.9, vwap.GetLatest(), 1e-9);
    }

    [Fact]
    public void Append_WithZeroVolume_FallsBackToPrice()
    {
        // The Append(price) path skips _cumulativeVolume updates, so the divisor stays zero and
        // ComputeNext returns `price` (see StreamingVwap.cs branch).
        var vwap = new StreamingVwap();
        vwap.Append(42);
        vwap.Append(99);
        var snap = vwap.OutputSeries[0].CreateSnapshot();
        Assert.Equal(42.0, snap.YData[0], 1e-9);
        Assert.Equal(99.0, snap.YData[1], 1e-9);
    }

    [Fact]
    public void RingBuffer_OldestEvicted_OnCapacityOverflow()
    {
        var vwap = new StreamingVwap(capacity: 2);
        vwap.AppendWithVolume(100, 10); // 100
        vwap.AppendWithVolume(120, 10); // (1000+1200)/20 = 110
        vwap.AppendWithVolume(140, 10); // (2200+1400)/30 = 120
        vwap.AppendWithVolume(160, 10); // (3600+1600)/40 = 130

        var snap = vwap.OutputSeries[0].CreateSnapshot();
        Assert.Equal(2, snap.YData.Length);
        Assert.Equal(120.0, snap.YData[0], 1e-9);
        Assert.Equal(130.0, snap.YData[1], 1e-9);
    }

    [Fact]
    public void GetLatest_BeforeAnyAppend_ReturnsNaN()
    {
        var vwap = new StreamingVwap();
        Assert.True(double.IsNaN(vwap.GetLatest()));
    }

    [Fact]
    public void ProcessedCount_IncrementsPerAppend()
    {
        var vwap = new StreamingVwap();
        for (int i = 0; i < 4; i++) vwap.AppendWithVolume(100 + i, 10);
        Assert.Equal(4, vwap.ProcessedCount);
    }
}
