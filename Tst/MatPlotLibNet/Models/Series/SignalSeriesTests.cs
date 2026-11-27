// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SignalSeries"/> construction, IMonotonicXY, lazy XData, and serialization.</summary>
public class SignalSeriesTests
{
    private static double[] Y3 => [10.0, 20.0, 30.0];

    // ── Construction ─────────────────────────���───────────────────────────────

    [Fact]
    public void ASignalGivenNoSampleRate_IsSampledOncePerUnit()
    {
        var s = new SignalSeries(Y3);
        Assert.Equal(1.0, s.SampleRate);
    }

    [Fact]
    public void ASignalGivenNoStart_StartsAtZero()
    {
        var s = new SignalSeries(Y3);
        Assert.Equal(0.0, s.XStart);
    }

    // ── IMonotonicXY ─────────────────��────────────────────────────────��──────

    [Fact]
    public void Length_EqualsYDataLength()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        Assert.Equal(3, s.Length);
    }

    [Fact]
    public void XAt_FirstIndex_EqualsXStart()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        Assert.Equal(5.0, s.XAt(0), 10);
    }

    [Fact]
    public void XAt_SecondIndex_XStartPlusInterval()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        Assert.Equal(5.01, s.XAt(1), 10);
    }

    [Fact]
    public void XAt_LastIndex_CorrectArithmetic()
    {
        var y = new double[1001];
        var s = new SignalSeries(y, sampleRate: 100.0, xStart: 5.0);
        Assert.Equal(5.0 + 1000.0 / 100.0, s.XAt(1000), 10);
    }

    [Fact]
    public void YAt_ReturnsYData()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        for (int i = 0; i < 3; i++)
            Assert.Equal(Y3[i], s.YAt(i));
    }

    // ── IndexRangeFor ──────────────────────────────���────────────────────────��

    [Fact]
    public void IndexRangeFor_ViewportInside_ReturnsSubset()
    {
        // sampleRate=1, xStart=0 → x[i]=i, 100 points
        var s = new SignalSeries(new double[100], sampleRate: 1.0, xStart: 0.0);
        var (start, end) = s.IndexRangeFor(20, 30);
        Assert.True(start <= 20 && end >= 31);
        Assert.True(start >= 0 && end <= 100);
    }

    [Theory]
    [InlineData(100, 10.0, -10.0, 5.0)] // the entire viewport is before xStart
    [InlineData(10, 0.0, 50.0, 100.0)]  // the entire viewport is past the last sample
    public void IndexRangeFor_OutsideTheSignal_ReturnsEmpty(int samples, double xStart, double viewMin, double viewMax)
    {
        var s = new SignalSeries(new double[samples], sampleRate: 1.0, xStart: xStart);
        var (start, end) = s.IndexRangeFor(viewMin, viewMax);
        Assert.True(start >= end);
    }

    [Fact]
    public void IndexRangeFor_SpanningViewport_ReturnsAll()
    {
        var s = new SignalSeries(new double[10], sampleRate: 1.0, xStart: 0.0);
        var (start, end) = s.IndexRangeFor(-1000, 1000);
        Assert.Equal(0, start);
        Assert.Equal(10, end);
    }

    [Fact]
    public void IndexRangeFor_InfinityBounds_ReturnsAll()
    {
        var s = new SignalSeries(new double[5], sampleRate: 1.0, xStart: 0.0);
        var (start, end) = s.IndexRangeFor(double.NegativeInfinity, double.PositiveInfinity);
        Assert.Equal(0, start);
        Assert.Equal(5, end);
    }

    [Fact]
    public void IndexRangeFor_EmptyArray_ReturnsEmpty()
    {
        var s = new SignalSeries([], sampleRate: 1.0);
        var (start, end) = s.IndexRangeFor(0, 100);
        Assert.True(start >= end);
    }

    // ── Lazy XData ──────────────────────────────���─────────────────────────��───

    [Fact]
    public void XData_FirstAccess_Materializes()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        var x = s.XData;
        Assert.NotNull(x);
        Assert.Equal(3, x.Length);
        Assert.Equal(5.0, x[0], 10);
        Assert.Equal(5.01, x[1], 10);
    }

    [Fact]
    public void XData_SecondAccess_ReturnsSameReference()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        var x1 = s.XData;
        var x2 = s.XData;
        Assert.Same(x1, x2);
    }

    // ── ComputeDataRange ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_UsesArithmetic()
    {
        var y = new double[] { 5.0, 3.0, 8.0 };
        var s = new SignalSeries(y, sampleRate: 2.0, xStart: 1.0);
        // x[0]=1.0, x[1]=1.5, x[2]=2.0
        var range = s.ComputeDataRange(null!);
        Assert.Equal(1.0, range.XMin!.Value, 10);
        Assert.Equal(2.0, range.XMax!.Value, 10);
        Assert.Equal(3.0, range.YMin!.Value);
        Assert.Equal(8.0, range.YMax!.Value);
    }

    /// <summary>SignalSeries.ComputeDataRange — `YData.Length == 0 ? new(0,0,0,0) : ...`
    /// TRUE arm: an empty-sample signal falls back to the degenerate all-zero range.</summary>
    [Fact]
    public void SignalSeries_EmptyYData_ComputeDataRange_ReturnsZeroRange()
    {
        var s = new SignalSeries(Array.Empty<double>());
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, range.XMin);
        Assert.Equal(0, range.XMax);
        Assert.Equal(0, range.YMin);
        Assert.Equal(0, range.YMax);
    }
}
