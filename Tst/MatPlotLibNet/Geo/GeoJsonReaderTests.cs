// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Tests.Geo;

/// <summary>Verifies GeoJSON parsing via GeoJsonReader.</summary>
public class GeoJsonReaderTests
{
    private const string SimpleFeatureCollection = """
        {
            "type": "FeatureCollection",
            "features": [
                {
                    "type": "Feature",
                    "geometry": {
                        "type": "Polygon",
                        "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]]
                    },
                    "properties": { "name": "Alpha" }
                },
                {
                    "type": "Feature",
                    "geometry": {
                        "type": "Polygon",
                        "coordinates": [[[2,2],[3,2],[3,3],[2,3],[2,2]]]
                    },
                    "properties": { "name": "Beta" }
                }
            ]
        }
        """;

    private const string SimpleFeature = """
        {
            "type": "Feature",
            "geometry": {
                "type": "LineString",
                "coordinates": [[0,0],[1,1],[2,0]]
            },
            "properties": { "id": "line1" }
        }
        """;

    private const string MultiPolygonJson = """
        {
            "type": "FeatureCollection",
            "features": [{
                "type": "Feature",
                "geometry": {
                    "type": "MultiPolygon",
                    "coordinates": [
                        [[[0,0],[1,0],[1,1],[0,0]]],
                        [[[2,2],[3,2],[3,3],[2,2]]]
                    ]
                },
                "properties": {}
            }]
        }
        """;

    private const string PointJson = """
        {
            "type": "Feature",
            "geometry": { "type": "Point", "coordinates": [10.5, 45.3] },
            "properties": {}
        }
        """;

    [Fact]
    public void Read_FeatureCollection_ParsesCorrectly()
    {
        var doc = GeoJsonReader.FromJson(SimpleFeatureCollection);
        Assert.NotNull(doc.FeatureCollection);
        Assert.Equal("FeatureCollection", doc.Type);
    }

    [Fact]
    public void Read_FeatureCollection_CountMatchesInput()
    {
        var doc = GeoJsonReader.FromJson(SimpleFeatureCollection);
        Assert.Equal(2, doc.FeatureCollection!.Features.Length);
    }

    [Fact]
    public void Read_Feature_ParsesGeometry()
    {
        var doc = GeoJsonReader.FromJson(SimpleFeature);
        Assert.NotNull(doc.Feature);
        Assert.Equal(GeoJsonGeometryType.LineString, doc.Feature!.Geometry!.Type);
    }

    [Fact]
    public void Read_Polygon_ParsesRings()
    {
        var doc = GeoJsonReader.FromJson(SimpleFeatureCollection);
        var geom = doc.FeatureCollection!.Features[0].Geometry!;
        Assert.Equal(GeoJsonGeometryType.Polygon, geom.Type);
        Assert.NotNull(geom.CoordinateRings);
        Assert.Single(geom.CoordinateRings!);   // one outer ring
        Assert.Equal(5, geom.CoordinateRings![0].Length); // 5 points (closed)
    }

    [Fact]
    public void Read_MultiPolygon_ParsesAllPolygons()
    {
        var doc = GeoJsonReader.FromJson(MultiPolygonJson);
        var geom = doc.FeatureCollection!.Features[0].Geometry!;
        Assert.Equal(GeoJsonGeometryType.MultiPolygon, geom.Type);
        Assert.NotNull(geom.MultiPolygonRings);
        Assert.Equal(2, geom.MultiPolygonRings!.Length);
    }

    [Fact]
    public void Read_LineString_ParsesCoords()
    {
        var doc = GeoJsonReader.FromJson(SimpleFeature);
        var geom = doc.Feature!.Geometry!;
        Assert.Equal(GeoJsonGeometryType.LineString, geom.Type);
        Assert.NotNull(geom.Coordinates);
        Assert.Equal(3, geom.Coordinates!.Length);
    }

    [Fact]
    public void Read_Point_ParsesCoord()
    {
        var doc = GeoJsonReader.FromJson(PointJson);
        Assert.Equal(GeoJsonGeometryType.Point, doc.Feature!.Geometry!.Type);
        Assert.NotNull(doc.Feature!.Geometry!.Coordinates);
        Assert.Equal(10.5, doc.Feature!.Geometry!.Coordinates![0][0], 3);
        Assert.Equal(45.3, doc.Feature!.Geometry!.Coordinates![0][1], 3);
    }

    [Fact]
    public void Read_Feature_ParsesProperties()
    {
        var doc = GeoJsonReader.FromJson(SimpleFeature);
        Assert.NotNull(doc.Feature!.Properties);
        Assert.True(doc.Feature!.Properties!.ContainsKey("id"));
    }

    [Fact]
    public void Read_InvalidJson_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => GeoJsonReader.FromJson("not json"));
    }

    [Fact]
    public void Read_EmptyFeatureCollection_HasNoFeatures()
    {
        const string empty = """{"type":"FeatureCollection","features":[]}""";
        var doc = GeoJsonReader.FromJson(empty);
        Assert.Empty(doc.FeatureCollection!.Features);
    }
}
