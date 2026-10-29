// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The writer is the counterpart of the reader: what it writes, the reader reads back as the same features, whatever
/// kind of geometry they have. A polygon keeps its holes, a line its positions.</summary>
public class GeoJsonWriterTests
{
    private static GeoFeature Feature(string type, List<List<(double Lon, double Lat)>> rings, string name = "feature") =>
        new(new GeoGeometry { Type = type, Rings = rings }, new Dictionary<string, string> { ["NAME"] = name });

    private static List<GeoFeature> RoundTrip(params GeoFeature[] features) =>
        GeoJsonReader.Parse(GeoJsonWriter.Write(features));

    [Fact]
    public void ALineString_ComesBackAsTheSameLine()
    {
        var back = Assert.Single(RoundTrip(Feature("LineString", [[(0, 0), (10, 5), (20, 0)]])));

        Assert.Equal("LineString", back.Geometry.Type);
        Assert.Equal([(0.0, 0.0), (10.0, 5.0), (20.0, 0.0)], Assert.Single(back.Geometry.Rings));
    }

    [Fact]
    public void ALineStringWithNoPositions_IsWrittenWithNone()
    {
        var back = Assert.Single(RoundTrip(Feature("LineString", [])));

        Assert.Equal("LineString", back.Geometry.Type);
        Assert.Empty(back.Geometry.Rings.SelectMany(ring => ring));
    }

    [Fact]
    public void AMultiLineString_ComesBackAsItsLines()
    {
        var back = Assert.Single(RoundTrip(Feature("MultiLineString", [[(0, 0), (1, 1)], [(5, 5), (6, 6), (7, 5)]])));

        Assert.Equal("MultiLineString", back.Geometry.Type);
        Assert.Equal(2, back.Geometry.Rings.Count);
        Assert.Equal([(5.0, 5.0), (6.0, 6.0), (7.0, 5.0)], back.Geometry.Rings[1]);
    }

    [Fact]
    public void APolygonWithAHole_ComesBackWithTheHole()
    {
        var back = Assert.Single(RoundTrip(Feature("Polygon",
            [[(0, 0), (10, 0), (10, 10), (0, 10), (0, 0)], [(2, 2), (2, 8), (8, 8), (8, 2), (2, 2)]])));

        var polygon = Assert.Single(back.Geometry.Polygons);
        Assert.Equal(5, polygon.Exterior.Count);
        Assert.Single(polygon.Holes);
    }

    [Fact]
    public void AMultiPolygon_ComesBackAsItsPolygons()
    {
        var back = Assert.Single(RoundTrip(Feature("MultiPolygon",
            [[(0, 0), (1, 0), (1, 1), (0, 0)], [(5, 5), (6, 5), (6, 6), (5, 5)]])));

        Assert.Equal("MultiPolygon", back.Geometry.Type);
        Assert.Equal(2, back.Geometry.Polygons.Count);
    }

    [Fact]
    public void TheProperties_AreWrittenWithTheirNames()
    {
        var back = Assert.Single(RoundTrip(Feature("LineString", [[(0, 0), (1, 1)]], "the Netherlands")));

        Assert.Equal("the Netherlands", back.Properties["NAME"]);
    }

    [Fact]
    public void NoFeatures_AreAnEmptyCollection()
    {
        var json = GeoJsonWriter.Write([]);

        Assert.Contains("\"FeatureCollection\"", json, StringComparison.Ordinal);
        Assert.Empty(GeoJsonReader.Parse(json));
    }
}
