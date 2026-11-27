// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The streaming stochastic: the high == low degenerate case (it says 50) and %D as the simple mean of the last d values
/// of %K.</summary>
public class StreamingStochasticTests
{
    [Fact]
    public void HighEqualsLow_PercentK_IsFifty()
    {
        var s = new StreamingStochastic(kPeriod: 1, dPeriod: 1, smoothK: 1, capacity: 16);
        // kPeriod=1, raw %K: a single bar where high==low triggers the (high == low) branch → %K = 50
        s.AppendCandle(new OhlcBar(10, 10, 10, 10));

        var k = s.OutputSeries[0].CreateSnapshot();
        Assert.Equal(50.0, k.YData[0]);
    }

    [Fact]
    public void TypicalCandle_PercentK_FollowsFormula()
    {
        var s = new StreamingStochastic(kPeriod: 1, dPeriod: 1, smoothK: 1, capacity: 16);
        // Single-bar window with close at 75% between low and high
        // => %K = (close - low) / (high - low) * 100 = (8.5 - 5) / (10 - 5) * 100 = 70
        s.AppendCandle(new OhlcBar(6, 10, 5, 8.5));

        var k = s.OutputSeries[0].CreateSnapshot();
        Assert.Equal(70.0, k.YData[0], 1e-9);
    }
}
