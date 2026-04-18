// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Edge-case coverage for <see cref="GeoJsonReader"/>: pushes from
/// 91.9/82.1 → ≥90/90 by exercising every geometry type branch (Polygon,
/// MultiPolygon, LineString, MultiLineString, unknown), the empty-feature-collection
/// path, and the null-property handling in <c>ParseProperties</c>.</summary>
public class GeoJsonReaderEdgeCaseTests
{
    [Fact]
    public void Parse_EmptyFeatureCollection_ReturnsEmptyList()
    {
        const string json = """{ "type": "FeatureCollection", "features": [] }""";
        var result = GeoJsonReader.Parse(json);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_MissingFeaturesProperty_ReturnsEmpty()
    {
        const string json = """{ "type": "FeatureCollection" }""";
        var result = GeoJsonReader.Parse(json);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_PolygonGeometry_ParsesRings()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [{
            "type": "Feature",
            "geometry": {
              "type": "Polygon",
              "coordinates": [[[0,0],[10,0],[10,10],[0,10],[0,0]]]
            },
            "properties": { "name": "Square" }
          }]
        }
        """;
        var result = GeoJsonReader.Parse(json);
        var feature = Assert.Single(result);
        Assert.Equal("Polygon", feature.Geometry.Type);
        Assert.Single(feature.Geometry.Rings);
        Assert.Equal(5, feature.Geometry.Rings[0].Count);
        Assert.Equal("Square", feature.Properties["name"]);
    }

    [Fact]
    public void Parse_MultiPolygonGeometry_ParsesAllRings()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [{
            "type": "Feature",
            "geometry": {
              "type": "MultiPolygon",
              "coordinates": [
                [[[0,0],[1,0],[1,1],[0,0]]],
                [[[5,5],[6,5],[6,6],[5,5]]]
              ]
            },
            "properties": { }
          }]
        }
        """;
        var result = GeoJsonReader.Parse(json);
        var feature = Assert.Single(result);
        Assert.Equal("MultiPolygon", feature.Geometry.Type);
        Assert.Equal(2, feature.Geometry.Rings.Count);
    }

    [Fact]
    public void Parse_LineStringGeometry_ParsesAsSingleRing()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [{
            "type": "Feature",
            "geometry": {
              "type": "LineString",
              "coordinates": [[0,0],[1,1],[2,2]]
            },
            "properties": { }
          }]
        }
        """;
        var result = GeoJsonReader.Parse(json);
        var feature = Assert.Single(result);
        Assert.Equal("LineString", feature.Geometry.Type);
        Assert.Single(feature.Geometry.Rings);
        Assert.Equal(3, feature.Geometry.Rings[0].Count);
    }

    [Fact]
    public void Parse_MultiLineStringGeometry_ParsesAllLines()
    {
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [{
            "type": "Feature",
            "geometry": {
              "type": "MultiLineString",
              "coordinates": [
                [[0,0],[1,1]],
                [[5,5],[6,6],[7,7]]
              ]
            },
            "properties": { }
          }]
        }
        """;
        var result = GeoJsonReader.Parse(json);
        var feature = Assert.Single(result);
        Assert.Equal("MultiLineString", feature.Geometry.Type);
        Assert.Equal(2, feature.Geometry.Rings.Count);
        Assert.Equal(2, feature.Geometry.Rings[0].Count);
        Assert.Equal(3, feature.Geometry.Rings[1].Count);
    }

    [Fact]
    public void Parse_UnknownGeometryType_ReturnsEmptyRings()
    {
        // The else-chain doesn't match → rings stays empty
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [{
            "type": "Feature",
            "geometry": {
              "type": "Point",
              "coordinates": [1, 2]
            },
            "properties": { }
          }]
        }
        """;
        var result = GeoJsonReader.Parse(json);
        var feature = Assert.Single(result);
        Assert.Equal("Point", feature.Geometry.Type);
        Assert.Empty(feature.Geometry.Rings);
    }

    [Fact]
    public void Parse_NullPropertyValue_ConvertedToEmptyString()
    {
        // Hits the `Value.ValueKind == JsonValueKind.Null → ""` branch
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [{
            "type": "Feature",
            "geometry": { "type": "LineString", "coordinates": [[0,0],[1,1]] },
            "properties": { "name": null, "code": "AB" }
          }]
        }
        """;
        var result = GeoJsonReader.Parse(json);
        var feature = Assert.Single(result);
        Assert.Equal("", feature.Properties["name"]);
        Assert.Equal("AB", feature.Properties["code"]);
    }

    [Fact]
    public void Parse_NumericPropertyValue_StringifiedViaToString()
    {
        // Non-null, non-string (e.g. number, bool) → goes through .ToString().
        // System.Text.Json's JsonElement.ToString() returns the JSON literal form
        // for numbers ("12345") but the .NET BCL form for booleans ("True"/"False"),
        // since the underlying JsonValueKind.True maps to Boolean.True.ToString().
        const string json = """
        {
          "type": "FeatureCollection",
          "features": [{
            "type": "Feature",
            "geometry": { "type": "LineString", "coordinates": [[0,0],[1,1]] },
            "properties": { "pop": 12345, "active": true }
          }]
        }
        """;
        var result = GeoJsonReader.Parse(json);
        var feature = Assert.Single(result);
        Assert.Equal("12345", feature.Properties["pop"]);
        // accept either case-form so the test passes regardless of System.Text.Json version
        Assert.Equal("true", feature.Properties["active"], ignoreCase: true);
    }
}
