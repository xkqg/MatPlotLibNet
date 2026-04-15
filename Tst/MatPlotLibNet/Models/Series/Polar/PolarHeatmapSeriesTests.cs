// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series.Polar;

/// <summary>Verifies the <see cref="PolarHeatmapSeries"/> model.</summary>
public class PolarHeatmapSeriesTests
{
    private static double[,] MakeData(int thetaBins = 4, int rBins = 3)
    {
        var d = new double[thetaBins, rBins];
        for (int t = 0; t < thetaBins; t++)
            for (int r = 0; r < rBins; r++)
                d[t, r] = t * rBins + r;
        return d;
    }

    // ---- Constructor --------------------------------------------------------

    [Fact]
    public void Constructor_StoresData()
    {
        var data = MakeData(4, 3);
        var s = new PolarHeatmapSeries(data, 4, 3);
        Assert.Same(data, s.Data);
    }

    [Fact]
    public void Constructor_StoresBinCounts()
    {
        var s = new PolarHeatmapSeries(MakeData(6, 4), 6, 4);
        Assert.Equal(6, s.ThetaBins);
        Assert.Equal(4, s.RBins);
    }

    [Fact]
    public void DefaultRMax_Is1()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3);
        Assert.Equal(1.0, s.RMax);
    }

    // ---- GetColorBarRange ---------------------------------------------------

    [Fact]
    public void GetColorBarRange_ReturnsMinAndMax()
    {
        var s = new PolarHeatmapSeries(MakeData(4, 3), 4, 3);
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.0, min);
        Assert.Equal(11.0, max); // 4*3 - 1 = 11
    }

    [Fact]
    public void GetColorBarRange_ConstantData_Returns0To1()
    {
        var data = new double[2, 2];
        data[0, 0] = data[0, 1] = data[1, 0] = data[1, 1] = 5.0;
        var s = new PolarHeatmapSeries(data, 2, 2);
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, max);
    }

    // ---- DataRange ----------------------------------------------------------

    [Fact]
    public void ComputeDataRange_ReturnsNullContribution()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3);
        var contribution = s.ComputeDataRange(null!);
        Assert.Null(contribution.XMin);
        Assert.Null(contribution.XMax);
        Assert.Null(contribution.YMin);
        Assert.Null(contribution.YMax);
    }

    // ---- ToSeriesDto --------------------------------------------------------

    [Fact]
    public void ToSeriesDto_SetsType()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3);
        Assert.Equal("polarheatmap", s.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_SetsBinCounts()
    {
        var s = new PolarHeatmapSeries(MakeData(8, 5), 8, 5);
        var dto = s.ToSeriesDto();
        Assert.Equal(8, dto.ThetaBins);
        Assert.Equal(5, dto.RBins);
    }

    [Fact]
    public void ToSeriesDto_SetsColorMapName_WhenSet()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3)
        {
            ColorMap = ColorMapRegistry.Get("viridis")
        };
        var dto = s.ToSeriesDto();
        Assert.Equal("viridis", dto.ColorMapName);
    }

    // ---- Accept -------------------------------------------------------------

    [Fact]
    public void Accept_CallsVisitor()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3);
        var visitor = new RecordingVisitor();
        s.Accept(visitor, new RenderArea(new Rect(0, 0, 100, 100), new MatPlotLibNet.Rendering.Svg.SvgRenderContext()));
        Assert.True(visitor.WasCalled);
    }

    private sealed class RecordingVisitor : ISeriesVisitor
    {
        public bool WasCalled { get; private set; }
        public void Visit(PolarHeatmapSeries series, RenderArea area) => WasCalled = true;

        // required interface stubs
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
