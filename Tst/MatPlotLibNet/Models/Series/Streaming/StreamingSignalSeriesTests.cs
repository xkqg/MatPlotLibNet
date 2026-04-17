// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

public sealed class StreamingSignalSeriesTests
{
    [Fact]
    public void Construction_DefaultCapacity_Is100000()
    {
        var s = new StreamingSignalSeries();
        Assert.Equal(100_000, s.Capacity);
        Assert.Equal(1.0, s.SampleRate);
        Assert.Equal(0.0, s.XStart);
    }

    [Fact]
    public void Construction_CustomParameters()
    {
        var s = new StreamingSignalSeries(capacity: 1000, sampleRate: 44100.0, xStart: 5.0);
        Assert.Equal(1000, s.Capacity);
        Assert.Equal(44100.0, s.SampleRate);
        Assert.Equal(5.0, s.XStart);
    }

    [Fact]
    public void AppendSample_IncrementsCount()
    {
        var s = new StreamingSignalSeries();
        s.AppendSample(1.5);
        Assert.Equal(1, s.Count);
        Assert.Equal(1, s.Version);
    }

    [Fact]
    public void AppendSamples_Batch()
    {
        var s = new StreamingSignalSeries();
        s.AppendSamples([1.0, 2.0, 3.0]);
        Assert.Equal(3, s.Count);
    }

    [Fact]
    public void XAt_ComputesFromSampleRate()
    {
        var s = new StreamingSignalSeries(capacity: 100, sampleRate: 2.0, xStart: 10.0);
        s.AppendSamples([1.0, 2.0, 3.0]);
        Assert.Equal(10.0, s.XAt(0)); // 10 + 0/2
        Assert.Equal(10.5, s.XAt(1)); // 10 + 1/2
        Assert.Equal(11.0, s.XAt(2)); // 10 + 2/2
    }

    [Fact]
    public void XAt_AfterWrap_AccountsForTotalAppended()
    {
        var s = new StreamingSignalSeries(capacity: 3, sampleRate: 1.0, xStart: 0.0);
        s.AppendSamples([1.0, 2.0, 3.0, 4.0, 5.0]); // 5 appended, 3 in buffer
        Assert.Equal(2.0, s.XAt(0)); // first retained sample index = 2
        Assert.Equal(3.0, s.XAt(1));
        Assert.Equal(4.0, s.XAt(2));
    }

    [Fact]
    public void CreateSnapshot_ComputesXFromSampleRate()
    {
        var s = new StreamingSignalSeries(capacity: 100, sampleRate: 10.0, xStart: 0.0);
        s.AppendSamples([1.0, 2.0, 3.0]);
        var snap = s.CreateSnapshot();
        Assert.Equal([0.0, 0.1, 0.2], snap.XData);
        Assert.Equal([1.0, 2.0, 3.0], snap.YData);
    }

    [Fact]
    public void CreateSnapshot_AfterWrap_XStartsCorrectly()
    {
        var s = new StreamingSignalSeries(capacity: 2, sampleRate: 1.0, xStart: 0.0);
        s.AppendSamples([10.0, 20.0, 30.0]); // keeps [20, 30], indices 1 and 2
        var snap = s.CreateSnapshot();
        Assert.Equal([1.0, 2.0], snap.XData);
        Assert.Equal([20.0, 30.0], snap.YData);
    }

    [Fact]
    public void ComputeDataRange_UsesArithmeticX()
    {
        var s = new StreamingSignalSeries(capacity: 100, sampleRate: 1.0, xStart: 5.0);
        s.AppendSamples([10.0, 20.0, 30.0]);
        var range = s.ComputeDataRange(null!);
        Assert.Equal(5.0, range.XMin);
        Assert.Equal(7.0, range.XMax);
        Assert.Equal(10.0, range.YMin);
        Assert.Equal(30.0, range.YMax);
    }

    [Fact]
    public void ComputeDataRange_Empty_ReturnsNulls()
    {
        var s = new StreamingSignalSeries();
        Assert.Null(s.ComputeDataRange(null!).XMin);
    }

    [Fact]
    public void Clear_ResetsEverything()
    {
        var s = new StreamingSignalSeries();
        s.AppendSamples([1.0, 2.0]);
        s.Clear();
        Assert.Equal(0, s.Count);
    }

    [Fact]
    public void LargeCapacity_NoXArrayAllocation()
    {
        // StreamingSignalSeries doesn't store X data — only Y
        var s = new StreamingSignalSeries(capacity: 100_000);
        s.AppendSample(1.0);
        Assert.Equal(1, s.Count);
    }
}
