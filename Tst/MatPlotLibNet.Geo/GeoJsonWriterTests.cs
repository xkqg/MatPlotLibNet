// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The writer is the counterpart of the reader: what it writes, the reader reads back as the same features, whatever
/// kind of geometry they have. A polygon keeps its holes, a line its positions.</summary>
public class GeoJsonWriterTests
{
    private static GeoFeature Feature(string type, IReadOnlyList<IReadOnlyList<GeoPosition>> rings, string name = "feature") =>
        new(new GeoGeometry { Type = type, PositionRings = rings }, new Dictionary<string, string> { ["NAME"] = name });

    private static List<GeoFeature> RoundTrip(params GeoFeature[] features) =>
        GeoJsonReader.Parse(GeoJsonWriter.Write(features));

    [Fact]
    public void ALineString_ComesBackAsTheSameLine()
    {
        var back = Assert.Single(RoundTrip(Feature("LineString", [[new(0, 0), new(10, 5), new(20, 0)]])));

        Assert.Equal("LineString", back.Geometry.Type);
        Assert.Equal([new GeoPosition(0, 0), new GeoPosition(10, 5), new GeoPosition(20, 0)], Assert.Single(back.Geometry.PositionRings));
    }

    [Fact]
    public void ALineStringWithNoPositions_IsWrittenWithNone()
    {
        var back = Assert.Single(RoundTrip(Feature("LineString", [])));

        Assert.Equal("LineString", back.Geometry.Type);
        Assert.Empty(back.Geometry.PositionRings.SelectMany(ring => ring));
    }

    [Fact]
    public void AMultiLineString_ComesBackAsItsLines()
    {
        var back = Assert.Single(RoundTrip(Feature("MultiLineString", [[new(0, 0), new(1, 1)], [new(5, 5), new(6, 6), new(7, 5)]])));

        Assert.Equal("MultiLineString", back.Geometry.Type);
        Assert.Equal(2, back.Geometry.PositionRings.Count);
        Assert.Equal([new GeoPosition(5, 5), new GeoPosition(6, 6), new GeoPosition(7, 5)], back.Geometry.PositionRings[1]);
    }

    [Fact]
    public void APolygonWithAHole_ComesBackWithTheHole()
    {
        var back = Assert.Single(RoundTrip(Feature("Polygon",
            [[new(0, 0), new(10, 0), new(10, 10), new(0, 10), new(0, 0)], [new(2, 2), new(2, 8), new(8, 8), new(8, 2), new(2, 2)]])));

        var polygon = Assert.Single(back.Geometry.Polygons);
        Assert.Equal(5, polygon.Exterior.Count);
        Assert.Single(polygon.Holes);
    }

    [Fact]
    public void AMultiPolygon_ComesBackAsItsPolygons()
    {
        var back = Assert.Single(RoundTrip(Feature("MultiPolygon",
            [[new(0, 0), new(1, 0), new(1, 1), new(0, 0)], [new(5, 5), new(6, 5), new(6, 6), new(5, 5)]])));

        Assert.Equal("MultiPolygon", back.Geometry.Type);
        Assert.Equal(2, back.Geometry.Polygons.Count);
    }

    [Fact]
    public void TheProperties_AreWrittenWithTheirNames()
    {
        var back = Assert.Single(RoundTrip(Feature("LineString", [[new(0, 0), new(1, 1)]], "the Netherlands")));

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
