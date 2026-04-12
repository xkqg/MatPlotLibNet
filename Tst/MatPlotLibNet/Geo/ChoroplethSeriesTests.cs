// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Geo;

/// <summary>Verifies ChoroplethSeries model and builder integration.</summary>
public class ChoroplethSeriesTests
{
    private static GeoJsonDocument MakePolygonDoc() => GeoJsonReader.FromJson("""
        {"type":"FeatureCollection","features":[
          {"type":"Feature","geometry":{"type":"Polygon",
           "coordinates":[[[0,0],[1,0],[1,1],[0,1],[0,0]]]},"properties":{}},
          {"type":"Feature","geometry":{"type":"Polygon",
           "coordinates":[[[2,2],[3,2],[3,3],[2,3],[2,2]]]},"properties":{}}
        ]}
        """);

    [Fact]
    public void ChoroplethSeries_InheritsMapSeries()
    {
        var s = new ChoroplethSeries();
        Assert.IsAssignableFrom<MapSeries>(s);
    }

    [Fact]
    public void ChoroplethSeries_DefaultColorMap_IsNull()
    {
        var s = new ChoroplethSeries();
        Assert.Null(s.ColorMap);
    }

    [Fact]
    public void ChoroplethSeries_Accept_CallsVisitorVisit()
    {
        var s = new ChoroplethSeries(MakePolygonDoc(), [0.5, 0.7]);
        var visitor = new ChoroplethTrackingVisitor();
        s.Accept(visitor, default!);
        Assert.True(visitor.ChoroplethVisited);
    }

    [Fact]
    public void ChoroplethSeries_Values_CanBeSet()
    {
        double[] vals = [1.0, 2.0, 3.0];
        var s = new ChoroplethSeries { Values = vals };
        Assert.Equal(vals, s.Values);
    }

    [Fact]
    public void ChoroplethSeries_VMinVMax_CanBeSet()
    {
        var s = new ChoroplethSeries { VMin = 0.0, VMax = 100.0 };
        Assert.Equal(0.0, s.VMin);
        Assert.Equal(100.0, s.VMax);
    }

    [Fact]
    public void FigureBuilder_Choropleth_AddsSeries()
    {
        var svg = Plt.Create()
            .Choropleth(MakePolygonDoc(), [10.0, 90.0])
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void FigureBuilder_Choropleth_Configure_SetsColorMap()
    {
        ChoroplethSeries? captured = null;
        _ = Plt.Create()
            .Choropleth(MakePolygonDoc(), [0.5, 0.8], s => { captured = s; s.ColorMap = ColorMaps.Plasma; })
            .Build();
        Assert.NotNull(captured?.ColorMap);
        Assert.Equal("plasma", captured!.ColorMap!.Name);
    }

    [Fact]
    public void ChoroplethSeries_ToSeriesDto_TypeIsChoropleth()
    {
        var s = new ChoroplethSeries(MakePolygonDoc(), [1.0, 2.0]);
        var dto = s.ToSeriesDto();
        Assert.Equal("choropleth", dto.Type);
    }

    // ── Helper visitor ────────────────────────────────────────────────

    private sealed class ChoroplethTrackingVisitor : ISeriesVisitor
    {
        public bool ChoroplethVisited { get; private set; }

        void ISeriesVisitor.Visit(ChoroplethSeries series, RenderArea area) => ChoroplethVisited = true;

        void ISeriesVisitor.Visit(LineSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ScatterSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(BarSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(HistogramSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(PieSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(HeatmapSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ImageSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(Histogram2DSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(BoxSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ViolinSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(KdeSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(RegressionSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(HexbinSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ContourSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ContourfSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(StemSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(AreaSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(StepSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(EcdfSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(StackedAreaSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ErrorBarSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(CandlestickSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(QuiverSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(StreamplotSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(RadarSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(DonutSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(BubbleSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(OhlcBarSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(WaterfallSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(FunnelSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(GanttSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(GaugeSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ProgressBarSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(SparklineSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(TreemapSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(SunburstSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(SankeySeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(PolarLineSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(PolarScatterSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(PolarBarSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(SurfaceSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(WireframeSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(Scatter3DSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(RugplotSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(StripplotSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(EventplotSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(BrokenBarSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(CountSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(PcolormeshSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(ResidualSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(PointplotSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(SwarmplotSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(SpectrogramSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(TableSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(TricontourSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(TripcolorSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(QuiverKeySeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(BarbsSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(Stem3DSeries s, RenderArea a) { }
        void ISeriesVisitor.Visit(Bar3DSeries s, RenderArea a) { }
    }
}
