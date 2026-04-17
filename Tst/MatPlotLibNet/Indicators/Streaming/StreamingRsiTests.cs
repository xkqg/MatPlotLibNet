// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingRsiTests
{
    [Fact]
    public void WarmupPeriod_IsPeriodPlusOne() =>
        Assert.Equal(15, new StreamingRsi(14).WarmupPeriod);

    [Fact]
    public void BeforeWarmup_ReturnsNaN()
    {
        var rsi = new StreamingRsi(14);
        for (int i = 0; i < 10; i++) rsi.Append(100 + i);
        Assert.True(double.IsNaN(rsi.GetLatest()));
    }

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

    [Fact]
    public void OutputInRange0To100()
    {
        var rng = new Random(42);
        var rsi = new StreamingRsi(14);
        for (int i = 0; i < 100; i++) rsi.Append(100 + rng.NextDouble() * 10 - 5);
        double value = rsi.GetLatest();
        Assert.InRange(value, 0, 100);
    }

    [Fact]
    public void Label_DefaultsToRSI() =>
        Assert.Equal("RSI(14)", new StreamingRsi(14).Label);
}
