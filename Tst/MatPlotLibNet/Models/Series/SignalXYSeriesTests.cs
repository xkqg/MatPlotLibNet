// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SignalXYSeries"/> construction, IMonotonicXY, and serialization.</summary>
public class SignalXYSeriesTests
{
    private static double[] X5 => [1.0, 2.0, 3.0, 4.0, 5.0];
    private static double[] Y5 => [10.0, 20.0, 30.0, 40.0, 50.0];

    // ── IMonotonicXY ─────────────────────────────────────────────────────────

    [Fact]
    public void Length_EqualsPointCount()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Equal(5, s.Length);
    }

    [Fact]
    public void XAtAndYAt_ReturnTheDataAtTheIndex()
    {
        var s = new SignalXYSeries(X5, Y5);

        Assert.Equal(X5, Enumerable.Range(0, 5).Select(s.XAt));
        Assert.Equal(Y5, Enumerable.Range(0, 5).Select(s.YAt));
    }

    // ── IndexRangeFor ────────────────────────────────────────────────────────

    [Fact]
    public void IndexRangeFor_ViewportInside_ReturnsSubset()
    {
        var s = new SignalXYSeries(X5, Y5);
        var (start, end) = s.IndexRangeFor(2.0, 4.0);
        Assert.True(start >= 0 && end <= 5);
        Assert.True(start < end);
        Assert.True(s.XAt(start) <= 4.0);
        Assert.True(s.XAt(end - 1) >= 2.0);
    }

    [Theory]
    [InlineData(-10, 0.5)]
    [InlineData(6.0, 10.0)]
    public void IndexRangeFor_OutsideTheSignal_ReturnsEmpty(double viewMin, double viewMax)
    {
        var s = new SignalXYSeries(X5, Y5); // x = 1..5
        var (start, end) = s.IndexRangeFor(viewMin, viewMax);
        Assert.True(start >= end);
    }

    [Fact]
    public void IndexRangeFor_SpanningViewport_ReturnsAll()
    {
        var s = new SignalXYSeries(X5, Y5);
        var (start, end) = s.IndexRangeFor(-100, 100);
        Assert.Equal(0, start);
        Assert.Equal(5, end);
    }

    [Fact]
    public void IndexRangeFor_SinglePoint_ReturnsOnePoint()
    {
        var s = new SignalXYSeries([3.0], [99.0]);
        var (start, end) = s.IndexRangeFor(2.5, 3.5);
        Assert.True(end - start >= 1);
    }

    [Fact]
    public void IndexRangeFor_EmptyArray_ReturnsEmpty()
    {
        var s = new SignalXYSeries([], []);
        var (start, end) = s.IndexRangeFor(0, 100);
        Assert.True(start >= end);
    }

    // ── ComputeDataRange ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_ReturnsCorrectBounds()
    {
        var s = new SignalXYSeries(X5, Y5);
        var range = s.ComputeDataRange(null!);
        Assert.Equal(1.0, range.XMin!.Value);
        Assert.Equal(5.0, range.XMax!.Value);
        Assert.Equal(10.0, range.YMin!.Value);
        Assert.Equal(50.0, range.YMax!.Value);
    }
}
