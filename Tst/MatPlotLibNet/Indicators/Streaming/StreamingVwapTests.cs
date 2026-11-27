// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Coverage uplift Batch D — full coverage for <see cref="StreamingVwap"/> (was 0%).
/// Asserts the cumulative price-volume formula, the no-volume fallback (returns the spot
/// price when cumulative volume is zero).</summary>
public sealed class StreamingVwapTests
{
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
    public void APriceWithoutAVolume_ReadsThePrice()
    {
        // A price is a bar of that price without a volume: the sums are not a number, so the average reads the price.
        IStreamingIndicator vwap = new StreamingVwap();
        vwap.Append(42);
        vwap.Append(99);
        var snap = vwap.OutputSeries[0].CreateSnapshot();
        Assert.Equal(42.0, snap.YData[0], 1e-9);
        Assert.Equal(99.0, snap.YData[1], 1e-9);
    }

    [Fact]
    public void ProcessedCount_IncrementsPerAppend()
    {
        var vwap = new StreamingVwap();
        for (int i = 0; i < 4; i++) vwap.AppendWithVolume(100 + i, 10);
        Assert.Equal(4, vwap.ProcessedCount);
    }

    [Fact]
    public void ACandleGivenToTheVwap_IsABarWithoutAVolume()
    {
        var withCandle = new StreamingVwap();
        var withBar = new StreamingVwap();

        withCandle.AppendCandle(new OhlcBar(10, 12, 9, 11));
        withBar.AppendBar(OhlcvBar.From(new OhlcBar(10, 12, 9, 11)));

        Assert.Equal(withBar.GetLatest(), withCandle.GetLatest());
        Assert.Equal(1, withCandle.ProcessedCount);
    }
}
