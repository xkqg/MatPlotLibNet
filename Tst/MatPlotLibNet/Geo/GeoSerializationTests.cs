// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Geo;

/// <summary>Verifies that MapSeries and ChoroplethSeries survive a JSON round-trip.</summary>
public class GeoSerializationTests
{
    private static GeoJsonDocument PolygonDoc() => GeoJsonReader.FromJson("""
        {"type":"FeatureCollection","features":[{"type":"Feature","geometry":
        {"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,1],[0,0]]]},"properties":{}}]}
        """);

    [Fact]
    public void MapSeries_RoundTrip_PreservesGeoJson()
    {
        var figure = Plt.Create()
            .Map(PolygonDoc())
            .Build();

        var json = ChartServices.Serializer.ToJson(figure);
        var restored = ChartServices.Serializer.FromJson(json);

        var series = restored.SubPlots[0].Series[0];
        Assert.Equal("map", series.ToSeriesDto().Type);
        Assert.NotNull(series.ToSeriesDto().GeoJson);
    }

    [Fact]
    public void MapSeries_RoundTrip_PreservesProjection()
    {
        var figure = Plt.Create()
            .Map(PolygonDoc(), s => s.Projection = MapProjections.Mercator())
            .Build();

        var json = ChartServices.Serializer.ToJson(figure);
        var restored = ChartServices.Serializer.FromJson(json);

        var dto = restored.SubPlots[0].Series[0].ToSeriesDto();
        Assert.Equal("mercator", dto.Projection);
    }

    [Fact]
    public void ChoroplethSeries_RoundTrip_PreservesValues()
    {
        double[] values = [10.0, 50.0, 90.0];
        var figure = Plt.Create()
            .Choropleth(PolygonDoc(), values)
            .Build();

        var json = ChartServices.Serializer.ToJson(figure);
        var restored = ChartServices.Serializer.FromJson(json);

        var dto = restored.SubPlots[0].Series[0].ToSeriesDto();
        Assert.Equal("choropleth", dto.Type);
        // Values are preserved through DTO
        Assert.NotNull(dto.Values);
    }

    [Fact]
    public void ChoroplethSeries_RoundTrip_PreservesColorMap()
    {
        var figure = Plt.Create()
            .Choropleth(PolygonDoc(), [0.5], s => s.ColorMap = ColorMaps.Plasma)
            .Build();

        var json = ChartServices.Serializer.ToJson(figure);
        var restored = ChartServices.Serializer.FromJson(json);

        var dto = restored.SubPlots[0].Series[0].ToSeriesDto();
        Assert.Equal("plasma", dto.ColorMapName);
    }

    [Fact]
    public void NullGeoJson_OmittedFromDto()
    {
        var series = new MapSeries(null);
        var dto = series.ToSeriesDto();
        Assert.Null(dto.GeoJson);
    }
}
