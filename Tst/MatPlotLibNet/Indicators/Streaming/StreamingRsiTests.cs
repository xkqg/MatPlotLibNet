// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingRsiTests
{
    [Fact]
    public void AllGains_Returns100()
    {
        var rsi = new StreamingRsi(5);
        for (int i = 0; i < 20; i++) rsi.Append(100 + i); // monotonically increasing
        Assert.Equal(100.0, rsi.GetLatest(), 1);
    }

    [Fact]
    public void AllLosses_Returns0()
    {
        var rsi = new StreamingRsi(5);
        for (int i = 0; i < 20; i++) rsi.Append(100 - i); // monotonically decreasing
        Assert.Equal(0.0, rsi.GetLatest(), 1);
    }
}
