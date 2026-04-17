// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingBollingerTests
{
    [Fact]
    public void OutputSeries_HasThree()
    {
        var bb = new StreamingBollinger(20, 2.0);
        Assert.Equal(3, bb.OutputSeries.Count);
    }

    [Fact]
    public void BeforeWarmup_ReturnsNaN()
    {
        var bb = new StreamingBollinger(5);
        for (int i = 0; i < 3; i++) bb.Append(100);
        Assert.True(double.IsNaN(bb.GetLatest()));
    }

    [Fact]
    public void ConstantInput_BandsEqualMiddle()
    {
        var bb = new StreamingBollinger(5, 2.0);
        for (int i = 0; i < 10; i++) bb.Append(50.0);

        var mid = bb.OutputSeries[0].CreateSnapshot();
        var upper = bb.OutputSeries[1].CreateSnapshot();
        var lower = bb.OutputSeries[2].CreateSnapshot();

        // With constant input, stddev = 0, so upper = lower = mid
        Assert.Equal(mid.YData[^1], upper.YData[^1], 6);
        Assert.Equal(mid.YData[^1], lower.YData[^1], 6);
    }

    [Fact]
    public void UpperAboveMiddleAboveLower()
    {
        var bb = new StreamingBollinger(5, 2.0);
        var rng = new Random(42);
        for (int i = 0; i < 20; i++) bb.Append(100 + rng.NextDouble() * 10);

        var mid = bb.OutputSeries[0].CreateSnapshot().YData[^1];
        var upper = bb.OutputSeries[1].CreateSnapshot().YData[^1];
        var lower = bb.OutputSeries[2].CreateSnapshot().YData[^1];

        Assert.True(upper > mid);
        Assert.True(mid > lower);
    }

    [Fact]
    public void WarmupPeriod_EqualsPeriod() =>
        Assert.Equal(20, new StreamingBollinger(20).WarmupPeriod);

    [Fact]
    public void Labels_SetCorrectly()
    {
        var bb = new StreamingBollinger(20);
        // OutputSeries[0] label is overwritten by base.Label setter to "BB(20)"
        Assert.Contains("BB", bb.OutputSeries[0].Label!);
        Assert.Contains("Upper", bb.OutputSeries[1].Label!);
        Assert.Contains("Lower", bb.OutputSeries[2].Label!);
    }
}
