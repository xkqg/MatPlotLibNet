// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Phase L.4 (v1.7.2, 2026-04-21) — abstract base for OHLC-based streaming indicator
/// tests. Eliminates the three [Fact] methods that were duplicated verbatim across
/// <see cref="StreamingCciTests"/>, <see cref="StreamingWilliamsRTests"/>,
/// and <see cref="StreamingAtrTests"/>.</summary>
public abstract class OhlcStreamingIndicatorTests<TIndicator>
    where TIndicator : IStreamingIndicator
{
    /// <summary>Creates an instance of the indicator under test with the given <paramref name="period"/>
    /// and optional ring-buffer <paramref name="capacity"/>.</summary>
    protected abstract TIndicator CreateIndicator(int period, int capacity = 256);

    private static OhlcBar TestBar(int i) => new(i, i + 2, i - 1, i + 1);

    [Fact]
    public void RingBuffer_RespectsCapacity_OnOverflow()
    {
        const int capacity = 3;
        var indicator = CreateIndicator(period: 2, capacity: capacity);
        // Push WarmupPeriod + capacity + 2 bars so the ring buffer evicts older entries.
        int bars = indicator.WarmupPeriod + capacity + 2;
        for (int i = 0; i < bars; i++) indicator.AppendCandle(TestBar(i));

        Assert.Equal(capacity, indicator.OutputSeries[0].Count);
    }

    [Fact]
    public void ComputeNext_ScalarPath_ReturnsNaN()
    {
        // The price-only Append path is unsupported for OHLC indicators; ComputeNext must
        // return NaN so callers that mistakenly use Append() see an unambiguous signal.
        var indicator = CreateIndicator(period: 3);
        indicator.Append(100);

        Assert.True(double.IsNaN(indicator.GetLatest()));
    }

    [Fact]
    public void Warmup_OutputIsNaN_UntilPeriodReached()
    {
        var indicator = CreateIndicator(period: 3);
        int warmupBars = indicator.WarmupPeriod - 1;
        for (int i = 0; i < warmupBars; i++) indicator.AppendCandle(TestBar(i));

        var y = indicator.OutputSeries[0].CreateSnapshot().YData;
        Assert.All(y, v => Assert.True(double.IsNaN(v)));
    }
}
