// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingSmaTests
{
    [Fact]
    public void WarmupPeriod_EqualsPeriod() =>
        Assert.Equal(5, new StreamingSma(5).WarmupPeriod);

    [Fact]
    public void BeforeWarmup_ReturnsNaN()
    {
        var sma = new StreamingSma(3);
        sma.Append(10);
        sma.Append(20);
        Assert.True(double.IsNaN(sma.GetLatest()));
    }

    [Fact]
    public void AtWarmup_ReturnsAverage()
    {
        var sma = new StreamingSma(3);
        sma.Append(10);
        sma.Append(20);
        sma.Append(30);
        Assert.Equal(20.0, sma.GetLatest(), 6);
    }

    [Fact]
    public void Rolling_EvictsOldest()
    {
        var sma = new StreamingSma(3);
        sma.Append(10); sma.Append(20); sma.Append(30);
        Assert.Equal(20.0, sma.GetLatest(), 6);
        sma.Append(40); // [20, 30, 40]
        Assert.Equal(30.0, sma.GetLatest(), 6);
    }

    [Fact]
    public void MatchesStaticSma_AfterWarmup()
    {
        double[] prices = [44, 44.34, 44.09, 43.61, 44.33, 44.83, 45.10, 45.42, 45.84, 46.08];
        var streaming = new StreamingSma(5);

        // Static SMA returns array starting after warmup
        var staticResult = new MatPlotLibNet.Indicators.Sma(prices, 5).Compute().Values;

        for (int i = 0; i < prices.Length; i++) streaming.Append(prices[i]);

        var snap = streaming.OutputSeries[0].CreateSnapshot();
        // Streaming output has NaN for first 4, then valid values
        // Static result has length = prices.Length - period + 1 = 6, all valid
        for (int i = 0; i < staticResult.Length; i++)
        {
            if (double.IsNaN(staticResult[i])) continue;
            int streamIdx = i + 4; // offset by warmup
            if (streamIdx < snap.YData.Length)
                Assert.Equal(staticResult[i], snap.YData[streamIdx], 4);
        }
    }

    [Fact]
    public void OutputSeries_HasOne() =>
        Assert.Single(new StreamingSma(5).OutputSeries);

    [Fact]
    public void Label_DefaultsToSMA() =>
        Assert.Equal("SMA(10)", new StreamingSma(10).Label);
}
