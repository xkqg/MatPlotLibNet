// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

public sealed class StreamingLineSeriesTests
{
    [Fact]
    public void Construction_DefaultCapacity_Is10000()
    {
        var s = new StreamingLineSeries();
        Assert.Equal(10_000, s.Capacity);
        Assert.Equal(0, s.Count);
        Assert.Equal(0, s.Version);
    }

    [Fact]
    public void Construction_CustomCapacity()
    {
        var s = new StreamingLineSeries(500);
        Assert.Equal(500, s.Capacity);
    }

    [Fact]
    public void AppendPoint_IncrementsCountAndVersion()
    {
        var s = new StreamingLineSeries();
        s.AppendPoint(1.0, 2.0);
        Assert.Equal(1, s.Count);
        Assert.Equal(1, s.Version);
        s.AppendPoint(3.0, 4.0);
        Assert.Equal(2, s.Count);
        Assert.Equal(2, s.Version);
    }

    [Fact]
    public void AppendPoints_BatchAppendsAll()
    {
        var s = new StreamingLineSeries();
        s.AppendPoints([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]);
        Assert.Equal(3, s.Count);
    }

    [Fact]
    public void AppendPoints_MismatchedLengths_Throws()
    {
        var s = new StreamingLineSeries();
        Assert.Throws<ArgumentException>(() => s.AppendPoints([1.0], [2.0, 3.0]));
    }

    [Fact]
    public void ComputeDataRange_ReturnsCorrectBounds()
    {
        var s = new StreamingLineSeries();
        s.AppendPoint(1.0, 10.0);
        s.AppendPoint(3.0, 5.0);
        s.AppendPoint(2.0, 15.0);
        var range = s.ComputeDataRange(null!);
        Assert.Equal(1.0, range.XMin);
        Assert.Equal(3.0, range.XMax);
        Assert.Equal(5.0, range.YMin);
        Assert.Equal(15.0, range.YMax);
    }

    [Fact]
    public void ComputeDataRange_Empty_ReturnsNulls()
    {
        var s = new StreamingLineSeries();
        var range = s.ComputeDataRange(null!);
        Assert.Null(range.XMin);
    }

    [Fact]
    public void CreateSnapshot_ReturnsImmutableCopy()
    {
        var s = new StreamingLineSeries(100);
        s.AppendPoint(1.0, 2.0);
        s.AppendPoint(3.0, 4.0);
        var snap = s.CreateSnapshot();
        Assert.Equal([1.0, 3.0], snap.XData);
        Assert.Equal([2.0, 4.0], snap.YData);

        // Modifying series doesn't affect snapshot
        s.AppendPoint(5.0, 6.0);
        Assert.Equal(2, snap.XData.Length);
    }

    [Fact]
    public void CreateSnapshot_VersionCaptured()
    {
        var s = new StreamingLineSeries();
        s.AppendPoint(1.0, 2.0);
        var snap = s.CreateSnapshot();
        Assert.Equal(1, snap.Version);
    }

    [Fact]
    public void Clear_ResetsCountAndIncrementsVersion()
    {
        var s = new StreamingLineSeries();
        s.AppendPoint(1.0, 2.0);
        s.AppendPoint(3.0, 4.0);
        long vBefore = s.Version;
        s.Clear();
        Assert.Equal(0, s.Count);
        Assert.True(s.Version > vBefore);
    }

    [Fact]
    public void DefaultVisualProperties()
    {
        var s = new StreamingLineSeries();
        Assert.Null(s.Color);
        Assert.Equal(LineStyle.Solid, s.LineStyle);
        Assert.Equal(1.5, s.LineWidth);
    }

    [Fact]
    public void Label_Settable()
    {
        var s = new StreamingLineSeries { Label = "Test" };
        Assert.Equal("Test", s.Label);
    }

    [Fact]
    public void Visible_DefaultTrue()
    {
        var s = new StreamingLineSeries();
        Assert.True(s.Visible);
    }

    [Fact]
    public void ToSeriesDto_TypeIsStreaming()
    {
        var s = new StreamingLineSeries { Label = "L1" };
        var dto = s.ToSeriesDto();
        Assert.Equal("streaming", dto.Type);
        Assert.Equal("L1", dto.Label);
    }
}
