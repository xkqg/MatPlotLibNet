// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingWarmupTests
{
    /// <summary>The two streaming averages with a window of three: the EMA seeds itself with the SMA of its first window.</summary>
    public static TheoryData<Func<StreamingIndicator<double>>> Averages => new()
    {
        () => new StreamingEma(3),
        () => new StreamingSma(3),
    };

    [Theory]
    [MemberData(nameof(Averages))]
    public void AtWarmup_ReturnsTheAverageOfTheWindow(Func<StreamingIndicator<double>> create)
    {
        var indicator = create();
        indicator.Append(10);
        indicator.Append(20);
        indicator.Append(30);
        Assert.Equal(20.0, indicator.GetLatest(), 6);
    }
}
