// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Geo;

/// <summary>Verifies MapSeries model and builder integration.</summary>
public class MapSeriesTests
{
    private static GeoJsonDocument MakePolygonDoc() => GeoJsonReader.FromJson("""
        {"type":"FeatureCollection","features":[{"type":"Feature","geometry":
        {"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,1],[0,0]]]},"properties":{}}]}
        """);

    [Fact]
    public void MapSeries_DefaultProjection_IsEquirectangular()
    {
        var s = new MapSeries();
        Assert.IsType<EquirectangularProjection>(s.Projection);
    }

    [Fact]
    public void MapSeries_DefaultLineWidth_IsHalfPoint()
    {
        var s = new MapSeries();
        Assert.Equal(0.5, s.LineWidth);
    }

    [Fact]
    public void MapSeries_FaceColor_DefaultIsNull()
    {
        var s = new MapSeries();
        Assert.Null(s.FaceColor);
    }

    [Fact]
    public void MapSeries_EdgeColor_DefaultIsNull()
    {
        var s = new MapSeries();
        Assert.Null(s.EdgeColor);
    }

    [Fact]
    public void MapSeries_GeoData_CanBeSetAfterConstruction()
    {
        var doc = MakePolygonDoc();
        var s = new MapSeries { GeoData = doc };
        Assert.Same(doc, s.GeoData);
    }

    [Fact]
    public void MapSeries_Accept_CallsVisitorVisit()
    {
        var s = new MapSeries(MakePolygonDoc());
        var visitor = new TrackingVisitor();
        s.Accept(visitor, default);
        Assert.True(visitor.MapSeriesVisited);
    }

    [Fact]
    public void MapSeries_ToSeriesDto_TypeIsMap()
    {
        var s = new MapSeries(MakePolygonDoc());
        var dto = s.ToSeriesDto();
        Assert.Equal("map", dto.Type);
    }

    [Fact]
    public void FigureBuilder_Map_AddsSeries()
    {
        var svg = Plt.Create()
            .Map(MakePolygonDoc())
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void FigureBuilder_Map_Configure_SetsProperties()
    {
        var captured = new MapSeries();
        _ = Plt.Create()
            .Map(MakePolygonDoc(), s => { captured = s; s.LineWidth = 2.0; })
            .Build();
        Assert.Equal(2.0, captured.LineWidth);
    }

    [Fact]
    public void MapSeries_VisibleByDefault()
    {
        var s = new MapSeries();
        Assert.True(s.Visible);
    }

    // ── Helper visitor ────────────────────────────────────────────────

    private sealed class TrackingVisitor : ISeriesVisitor
    {
        public bool MapSeriesVisited { get; private set; }

        void ISeriesVisitor.Visit(MapSeries series, RenderArea area) => MapSeriesVisited = true;

        // All other Visit overloads are no-ops (default interface methods cover the rest)
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
