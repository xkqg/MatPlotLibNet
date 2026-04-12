// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Tests.Geo;

/// <summary>Verifies GeoJSON round-trip via GeoJsonReader + GeoJsonWriter.</summary>
public class GeoJsonWriterTests
{
    private const string PolygonCollection = """
        {"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[0.0,0.0],[1.0,0.0],[1.0,1.0],[0.0,1.0],[0.0,0.0]]]},"properties":{"name":"Alpha"}}]}
        """;

    [Fact]
    public void Write_FeatureCollection_RoundTrips()
    {
        var doc = GeoJsonReader.FromJson(PolygonCollection);
        var json = GeoJsonWriter.ToJson(doc);
        var restored = GeoJsonReader.FromJson(json);

        Assert.NotNull(restored.FeatureCollection);
        Assert.Single(restored.FeatureCollection!.Features);
    }

    [Fact]
    public void Write_Feature_PreservesProperties()
    {
        var doc = GeoJsonReader.FromJson(PolygonCollection);
        var json = GeoJsonWriter.ToJson(doc);
        var restored = GeoJsonReader.FromJson(json);

        var props = restored.FeatureCollection!.Features[0].Properties;
        Assert.NotNull(props);
        Assert.True(props!.ContainsKey("name"));
    }

    [Fact]
    public void Write_Polygon_RoundTrips()
    {
        var doc = GeoJsonReader.FromJson(PolygonCollection);
        var json = GeoJsonWriter.ToJson(doc);
        var restored = GeoJsonReader.FromJson(json);

        var geom = restored.FeatureCollection!.Features[0].Geometry!;
        Assert.Equal(GeoJsonGeometryType.Polygon, geom.Type);
        Assert.NotNull(geom.CoordinateRings);
        Assert.Equal(5, geom.CoordinateRings![0].Length);
    }

    [Fact]
    public void Write_MultiPolygon_RoundTrips()
    {
        const string multiPoly = """
            {"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"MultiPolygon","coordinates":[[[[0.0,0.0],[1.0,0.0],[1.0,1.0],[0.0,0.0]]],[[[2.0,2.0],[3.0,2.0],[3.0,3.0],[2.0,2.0]]]]},"properties":{}}]}
            """;
        var doc = GeoJsonReader.FromJson(multiPoly);
        var json = GeoJsonWriter.ToJson(doc);
        var restored = GeoJsonReader.FromJson(json);

        var geom = restored.FeatureCollection!.Features[0].Geometry!;
        Assert.Equal(GeoJsonGeometryType.MultiPolygon, geom.Type);
        Assert.Equal(2, geom.MultiPolygonRings!.Length);
    }
}
