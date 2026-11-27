// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

public sealed class StreamingSeriesConstructionTests
{
    /// <summary>What a streaming series holds before anyone gives it a capacity: the line series ten thousand points, the signal series a
    /// hundred thousand samples at a rate of one from zero.</summary>
    public static TheoryData<Action> TheDefaults => new()
    {
        () =>
        {
            var s = new StreamingLineSeries();
            Assert.Equal(10_000, s.Capacity);
            Assert.Equal(0, s.Count);
            Assert.Equal(0, s.Version);
        },
        () =>
        {
            var s = new StreamingSignalSeries();
            Assert.Equal(100_000, s.Capacity);
            Assert.Equal(1.0, s.SampleRate);
            Assert.Equal(0.0, s.XStart);
        },
    };

    [Theory]
    [MemberData(nameof(TheDefaults))]
    public void Construction_DefaultCapacity(Action check) => check();
}
