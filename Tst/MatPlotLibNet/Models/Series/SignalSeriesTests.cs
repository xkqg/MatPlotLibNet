// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SignalSeries"/> construction, IMonotonicXY, lazy XData, and serialization.</summary>
public class SignalSeriesTests
{
    private static double[] Y3 => [10.0, 20.0, 30.0];

    // ── Construction ─────────────────────────���───────────────────────────────

    [Fact]
    public void Constructor_StoresYData()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        Assert.Equal(Y3, s.YData);
    }

    [Fact]
    public void Constructor_StoresSampleRate()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0);
        Assert.Equal(100.0, s.SampleRate);
    }

    [Fact]
    public void Constructor_StoresXStart()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        Assert.Equal(5.0, s.XStart);
    }

    [Fact]
    public void DefaultSampleRate_Is1()
    {
        var s = new SignalSeries(Y3);
        Assert.Equal(1.0, s.SampleRate);
    }

    [Fact]
    public void DefaultXStart_Is0()
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

    [Fact]
    public void IndexRangeFor_OutsideLeft_ReturnsEmpty()
    {
        var s = new SignalSeries(new double[100], sampleRate: 1.0, xStart: 10.0);
        var (start, end) = s.IndexRangeFor(-10, 5); // entire viewport before xStart
        Assert.True(start >= end);
    }

    [Fact]
    public void IndexRangeFor_OutsideRight_ReturnsEmpty()
    {
        var s = new SignalSeries(new double[10], sampleRate: 1.0, xStart: 0.0);
        var (start, end) = s.IndexRangeFor(50, 100);
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

    // ── Default visual properties ─────────────────────────────────────────────

    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var s = new SignalSeries(Y3);
        Assert.Equal(LineStyle.Solid, s.LineStyle);
    }

    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var s = new SignalSeries(Y3);
        Assert.Equal(1.5, s.LineWidth);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var s = new SignalSeries(Y3);
        Assert.Null(s.Color);
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

    // ── Serialization ────────────────────────────────────────────────────────

    [Fact]
    public void ToSeriesDto_TypeIsSignal()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        Assert.Equal("signal", s.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_HasSignalSampleRate()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        var dto = s.ToSeriesDto();
        Assert.Equal(100.0, dto.SignalSampleRate);
    }

    [Fact]
    public void ToSeriesDto_HasSignalXStart()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        var dto = s.ToSeriesDto();
        Assert.Equal(5.0, dto.SignalXStart);
    }

    [Fact]
    public void ToSeriesDto_HasYData_NoXDataInDto()
    {
        var s = new SignalSeries(Y3, sampleRate: 100.0, xStart: 5.0);
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto.YData);
        Assert.Null(dto.XData);
    }
}
