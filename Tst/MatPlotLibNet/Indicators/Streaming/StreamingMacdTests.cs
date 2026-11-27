// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingMacdTests
{
    [Fact]
    public void RisingPrices_MacdPositive()
    {
        var macd = new StreamingMacd(3, 5, 2);
        for (int i = 0; i < 30; i++) macd.Append(100 + i * 2); // strong uptrend
        Assert.True(macd.GetLatest() > 0);
    }

    [Fact]
    public void Labels_SetCorrectly()
    {
        var macd = new StreamingMacd();
        Assert.Contains("MACD", macd.OutputSeries[0].Label);
        Assert.Contains("Signal", macd.OutputSeries[1].Label);
        Assert.Contains("Histogram", macd.OutputSeries[2].Label);
    }
}
