// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

public sealed class StreamingCandlestickSeriesTests
{
    [Fact]
    public void Construction_DefaultCapacity_Is5000()
    {
        var s = new StreamingCandlestickSeries();
        Assert.Equal(5_000, s.Capacity);
        Assert.Equal(0, s.Count);
    }

    [Fact]
    public void AppendBar_IncrementsCountAndVersion()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(100, 110, 95, 105);
        Assert.Equal(1, s.Count);
        Assert.Equal(1, s.Version);
    }

    [Fact]
    public void AppendBar_OhlcBarOverload()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(new OhlcBar(100, 110, 95, 105));
        Assert.Equal(1, s.Count);
    }

    [Fact]
    public void CreateOhlcSnapshot_ReturnsFourParallelArrays()
    {
        var s = new StreamingCandlestickSeries(100);
        s.AppendBar(100, 110, 95, 105);
        s.AppendBar(105, 115, 100, 110);
        var snap = s.CreateOhlcSnapshot();
        Assert.Equal([100.0, 105.0], snap.Open);
        Assert.Equal([110.0, 115.0], snap.High);
        Assert.Equal([95.0, 100.0], snap.Low);
        Assert.Equal([105.0, 110.0], snap.Close);
    }

    [Fact]
    public void CreateOhlcSnapshot_IsImmutable()
    {
        var s = new StreamingCandlestickSeries(100);
        s.AppendBar(100, 110, 95, 105);
        var snap = s.CreateOhlcSnapshot();
        s.AppendBar(200, 220, 190, 210);
        Assert.Equal(1, snap.Open.Length); // snapshot not affected
    }

    [Fact]
    public void ComputeDataRange_UsesLowMinAndHighMax()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(100, 120, 80, 110);
        s.AppendBar(105, 115, 90, 108);
        var range = s.ComputeDataRange(null!);
        Assert.Equal(80.0, range.YMin);
        Assert.Equal(120.0, range.YMax);
    }

    [Fact]
    public void ComputeDataRange_Empty_ReturnsNulls()
    {
        var s = new StreamingCandlestickSeries();
        Assert.Null(s.ComputeDataRange(null!).XMin);
    }

    [Fact]
    public void BarAppended_FiresOnAppend()
    {
        var s = new StreamingCandlestickSeries();
        OhlcBar? received = null;
        s.BarAppended += bar => received = bar;
        s.AppendBar(100, 110, 95, 105);
        Assert.NotNull(received);
        Assert.Equal(100.0, received.Value.Open);
        Assert.Equal(110.0, received.Value.High);
        Assert.Equal(95.0, received.Value.Low);
        Assert.Equal(105.0, received.Value.Close);
    }

    [Fact]
    public void Clear_ResetsAll()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(100, 110, 95, 105);
        s.Clear();
        Assert.Equal(0, s.Count);
    }

    [Fact]
    public void ImplementsIStreamingOhlcSeries() =>
        Assert.IsAssignableFrom<IStreamingOhlcSeries>(new StreamingCandlestickSeries());
}
