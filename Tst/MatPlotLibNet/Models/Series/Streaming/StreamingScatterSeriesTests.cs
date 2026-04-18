// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

public sealed class StreamingScatterSeriesTests
{
    [Fact]
    public void Construction_DefaultCapacity_Is10000()
    {
        var s = new StreamingScatterSeries();
        Assert.Equal(10_000, s.Capacity);
    }

    [Fact]
    public void AppendPoint_Works()
    {
        var s = new StreamingScatterSeries();
        s.AppendPoint(1.0, 2.0);
        Assert.Equal(1, s.Count);
        Assert.Equal(1, s.Version);
    }

    [Fact]
    public void CreateSnapshot_ReturnsCorrectData()
    {
        var s = new StreamingScatterSeries(100);
        s.AppendPoint(1.0, 2.0);
        s.AppendPoint(3.0, 4.0);
        var snap = s.CreateSnapshot();
        Assert.Equal([1.0, 3.0], snap.XData);
        Assert.Equal([2.0, 4.0], snap.YData);
    }

    [Fact]
    public void DefaultVisualProperties()
    {
        var s = new StreamingScatterSeries();
        Assert.Null(s.Color);
        Assert.Equal(1.0, s.Alpha);
        Assert.Equal(6.0, s.MarkerSize);
    }

    [Fact]
    public void ComputeDataRange_WithData_ReturnsCorrectBounds()
    {
        var s = new StreamingScatterSeries();
        s.AppendPoints([1.0, 5.0], [3.0, 7.0]);
        var range = s.ComputeDataRange(null!);
        Assert.Equal(1.0, range.XMin);
        Assert.Equal(5.0, range.XMax);
        Assert.Equal(3.0, range.YMin);
        Assert.Equal(7.0, range.YMax);
    }
}
