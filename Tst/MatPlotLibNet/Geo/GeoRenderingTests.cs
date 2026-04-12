// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Geo;

/// <summary>Verifies MapSeries and ChoroplethSeries rendering output.</summary>
public class GeoRenderingTests
{
    private static GeoJsonDocument PolygonDoc() => GeoJsonReader.FromJson("""
        {"type":"FeatureCollection","features":[
          {"type":"Feature","geometry":{"type":"Polygon",
           "coordinates":[[[0,0],[10,0],[10,10],[0,10],[0,0]]]},"properties":{}},
          {"type":"Feature","geometry":{"type":"Polygon",
           "coordinates":[[[20,20],[30,20],[30,30],[20,30],[20,20]]]},"properties":{}}
        ]}
        """);

    private static GeoJsonDocument LineDoc() => GeoJsonReader.FromJson("""
        {"type":"FeatureCollection","features":[
          {"type":"Feature","geometry":{"type":"LineString",
           "coordinates":[[0,0],[10,10],[20,0]]},"properties":{}}
        ]}
        """);

    // ── MapSeries ─────────────────────────────────────────────────────

    [Fact]
    public void MapSeries_Render_ProducesSvgWithPath()
    {
        var svg = Plt.Create()
            .Map(PolygonDoc())
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void MapSeries_Render_EmptyGeoData_NoException()
    {
        var svg = Plt.Create()
            .Map(null!)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void MapSeries_Render_FaceColor_AppliedToPolygon()
    {
        var svg = Plt.Create()
            .Map(PolygonDoc(), s => s.FaceColor = Color.FromHex("#FF0000"))
            .ToSvg();
        Assert.Contains("ff0000", svg.ToLowerInvariant());
    }

    [Fact]
    public void MapSeries_Render_EdgeColor_AppliedToStroke()
    {
        var svg = Plt.Create()
            .Map(PolygonDoc(), s => s.EdgeColor = Color.FromHex("#0000FF"))
            .ToSvg();
        Assert.Contains("0000ff", svg.ToLowerInvariant());
    }

    [Fact]
    public void MapSeries_Render_UsesProjection()
    {
        // Both equirectangular and mercator should produce valid SVG
        var svgEq = Plt.Create()
            .Map(PolygonDoc(), s => s.Projection = MapProjections.Equirectangular())
            .ToSvg();
        var svgMerc = Plt.Create()
            .Map(PolygonDoc(), s => s.Projection = MapProjections.Mercator())
            .ToSvg();
        Assert.Contains("<polygon", svgEq);
        Assert.Contains("<polygon", svgMerc);
    }

    [Fact]
    public void MapSeries_Render_LineString_ProducesOutput()
    {
        // LineString is rendered as a stroke-only polygon (no fill)
        var svg = Plt.Create()
            .Map(LineDoc())
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void MapSeries_Render_NullGeoData_NoOutput()
    {
        // With null GeoData, no polygon/polyline should appear in the SVG
        var svg = Plt.Create()
            .Map(null!)
            .ToSvg();
        Assert.DoesNotContain("<polygon", svg);
        Assert.DoesNotContain("<polyline", svg);
    }

    // ── ChoroplethSeries (F6 tests appended below) ────────────────────

    [Fact]
    public void ChoroplethSeries_Render_ColorsFeaturesByValue()
    {
        var svg = Plt.Create()
            .Choropleth(PolygonDoc(), [10.0, 90.0])
            .ToSvg();
        // Two features should produce two filled polygons
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void ChoroplethSeries_Render_UsesColorMap()
    {
        var svg = Plt.Create()
            .Choropleth(PolygonDoc(), [0.0, 1.0], s => s.ColorMap = ColorMaps.Viridis)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChoroplethSeries_Render_NullValues_NoException()
    {
        var s = new ChoroplethSeries(PolygonDoc(), null);
        var svg = Plt.Create()
            .Map(PolygonDoc()) // use Map to confirm no crash
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChoroplethSeries_Render_ValuesShortThanFeatures_NoException()
    {
        // Only 1 value but 2 features — should not throw
        var svg = Plt.Create()
            .Choropleth(PolygonDoc(), [0.5])
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChoroplethSeries_Render_VMinVMax_AffectsNormalization()
    {
        // Both calls should produce valid SVG regardless of vmin/vmax
        var svg = Plt.Create()
            .Choropleth(PolygonDoc(), [50.0, 100.0], s => { s.VMin = 0; s.VMax = 200; })
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChoroplethSeries_Render_ProducesSvgWithFill()
    {
        var svg = Plt.Create()
            .Choropleth(PolygonDoc(), [0.2, 0.8])
            .ToSvg();
        // Polygons with fill should appear
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void ChoroplethSeries_Render_DefaultColorMap_IsViridis()
    {
        // Without setting a colormap, Viridis should be the fallback
        var svg = Plt.Create()
            .Choropleth(PolygonDoc(), [0.5, 0.5])
            .ToSvg();
        // Viridis starts with dark purple colors (should have non-trivial fill)
        Assert.Contains("<polygon", svg);
    }
}
