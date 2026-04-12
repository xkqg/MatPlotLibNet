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

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_StoresXData()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Equal(X5, s.XData);
    }

    [Fact]
    public void Constructor_StoresYData()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Equal(Y5, s.YData);
    }

    // ── IMonotonicXY ─────────────────────────────────────────────────────────

    [Fact]
    public void Length_EqualsPointCount()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Equal(5, s.Length);
    }

    [Fact]
    public void XAt_ReturnsXData()
    {
        var s = new SignalXYSeries(X5, Y5);
        for (int i = 0; i < 5; i++)
            Assert.Equal(X5[i], s.XAt(i));
    }

    [Fact]
    public void YAt_ReturnsYData()
    {
        var s = new SignalXYSeries(X5, Y5);
        for (int i = 0; i < 5; i++)
            Assert.Equal(Y5[i], s.YAt(i));
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

    [Fact]
    public void IndexRangeFor_OutsideLeft_ReturnsEmpty()
    {
        var s = new SignalXYSeries(X5, Y5); // x = 1..5
        var (start, end) = s.IndexRangeFor(-10, 0.5);
        Assert.True(start >= end);
    }

    [Fact]
    public void IndexRangeFor_OutsideRight_ReturnsEmpty()
    {
        var s = new SignalXYSeries(X5, Y5); // x = 1..5
        var (start, end) = s.IndexRangeFor(6.0, 10.0);
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

    // ── Default properties ────────────────────────────────────────────────────

    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Equal(LineStyle.Solid, s.LineStyle);
    }

    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Equal(1.5, s.LineWidth);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Null(s.Color);
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

    // ── Serialization ────────────────────────────────────────────────────────

    [Fact]
    public void ToSeriesDto_TypeIsSignalXY()
    {
        var s = new SignalXYSeries(X5, Y5);
        Assert.Equal("signal-xy", s.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_IncludesXDataAndYData()
    {
        var s = new SignalXYSeries(X5, Y5);
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto.XData);
        Assert.NotNull(dto.YData);
    }

    [Fact]
    public void ToSeriesDto_NoSignalSampleRate()
    {
        var s = new SignalXYSeries(X5, Y5);
        var dto = s.ToSeriesDto();
        Assert.Null(dto.SignalSampleRate);
    }

    // ── Visitor dispatch ─────────────────────────────────────────────────────

    [Fact]
    public void Accept_DispatchesToVisitSignalXY()
    {
        var s = new SignalXYSeries(X5, Y5);
        var spy = new VisitorSpy();
        s.Accept(spy, default!);
        Assert.True(spy.VisitedSignalXY);
    }

    private sealed class VisitorSpy : ISeriesVisitor
    {
        public bool VisitedSignalXY { get; private set; }
        public void Visit(SignalXYSeries s, RenderArea a) => VisitedSignalXY = true;

        // Required abstract methods — no-op for test
        public void Visit(LineSeries s, RenderArea a) { }
        public void Visit(ScatterSeries s, RenderArea a) { }
        public void Visit(BarSeries s, RenderArea a) { }
        public void Visit(HistogramSeries s, RenderArea a) { }
        public void Visit(PieSeries s, RenderArea a) { }
        public void Visit(HeatmapSeries s, RenderArea a) { }
        public void Visit(ImageSeries s, RenderArea a) { }
        public void Visit(Histogram2DSeries s, RenderArea a) { }
        public void Visit(BoxSeries s, RenderArea a) { }
        public void Visit(ViolinSeries s, RenderArea a) { }
        public void Visit(KdeSeries s, RenderArea a) { }
        public void Visit(RegressionSeries s, RenderArea a) { }
        public void Visit(HexbinSeries s, RenderArea a) { }
        public void Visit(ContourSeries s, RenderArea a) { }
        public void Visit(ContourfSeries s, RenderArea a) { }
        public void Visit(StemSeries s, RenderArea a) { }
        public void Visit(AreaSeries s, RenderArea a) { }
        public void Visit(StepSeries s, RenderArea a) { }
        public void Visit(EcdfSeries s, RenderArea a) { }
        public void Visit(StackedAreaSeries s, RenderArea a) { }
        public void Visit(ErrorBarSeries s, RenderArea a) { }
        public void Visit(CandlestickSeries s, RenderArea a) { }
        public void Visit(QuiverSeries s, RenderArea a) { }
        public void Visit(StreamplotSeries s, RenderArea a) { }
        public void Visit(RadarSeries s, RenderArea a) { }
        public void Visit(DonutSeries s, RenderArea a) { }
        public void Visit(BubbleSeries s, RenderArea a) { }
        public void Visit(OhlcBarSeries s, RenderArea a) { }
        public void Visit(WaterfallSeries s, RenderArea a) { }
        public void Visit(FunnelSeries s, RenderArea a) { }
        public void Visit(GanttSeries s, RenderArea a) { }
        public void Visit(GaugeSeries s, RenderArea a) { }
        public void Visit(ProgressBarSeries s, RenderArea a) { }
        public void Visit(SparklineSeries s, RenderArea a) { }
        public void Visit(TreemapSeries s, RenderArea a) { }
        public void Visit(SunburstSeries s, RenderArea a) { }
        public void Visit(SankeySeries s, RenderArea a) { }
        public void Visit(PolarLineSeries s, RenderArea a) { }
        public void Visit(PolarScatterSeries s, RenderArea a) { }
        public void Visit(PolarBarSeries s, RenderArea a) { }
        public void Visit(SurfaceSeries s, RenderArea a) { }
        public void Visit(WireframeSeries s, RenderArea a) { }
        public void Visit(Scatter3DSeries s, RenderArea a) { }
        public void Visit(RugplotSeries s, RenderArea a) { }
        public void Visit(StripplotSeries s, RenderArea a) { }
        public void Visit(EventplotSeries s, RenderArea a) { }
        public void Visit(BrokenBarSeries s, RenderArea a) { }
        public void Visit(CountSeries s, RenderArea a) { }
        public void Visit(PcolormeshSeries s, RenderArea a) { }
        public void Visit(ResidualSeries s, RenderArea a) { }
        public void Visit(PointplotSeries s, RenderArea a) { }
        public void Visit(SwarmplotSeries s, RenderArea a) { }
        public void Visit(SpectrogramSeries s, RenderArea a) { }
        public void Visit(TableSeries s, RenderArea a) { }
        public void Visit(TricontourSeries s, RenderArea a) { }
        public void Visit(TripcolorSeries s, RenderArea a) { }
        public void Visit(QuiverKeySeries s, RenderArea a) { }
        public void Visit(BarbsSeries s, RenderArea a) { }
        public void Visit(Stem3DSeries s, RenderArea a) { }
        public void Visit(Bar3DSeries s, RenderArea a) { }
    }
}
