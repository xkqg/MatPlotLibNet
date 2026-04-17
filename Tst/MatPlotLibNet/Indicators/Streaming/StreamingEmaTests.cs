// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingEmaTests
{
    [Fact]
    public void WarmupPeriod_EqualsPeriod() =>
        Assert.Equal(10, new StreamingEma(10).WarmupPeriod);

    [Fact]
    public void BeforeWarmup_ReturnsNaN()
    {
        var ema = new StreamingEma(3);
        ema.Append(10);
        Assert.True(double.IsNaN(ema.GetLatest()));
    }

    [Fact]
    public void AtWarmup_ReturnsSmaAsFirst()
    {
        var ema = new StreamingEma(3);
        ema.Append(10); ema.Append(20); ema.Append(30);
        Assert.Equal(20.0, ema.GetLatest(), 6); // SMA seed
    }

    [Fact]
    public void AfterWarmup_AppliesMultiplier()
    {
        var ema = new StreamingEma(3);
        ema.Append(10); ema.Append(20); ema.Append(30); // seed = 20
        ema.Append(40); // (40-20)*0.5 + 20 = 30
        Assert.Equal(30.0, ema.GetLatest(), 6);
    }

    [Fact]
    public void Label_DefaultsToEMA() =>
        Assert.Equal("EMA(12)", new StreamingEma(12).Label);
}
