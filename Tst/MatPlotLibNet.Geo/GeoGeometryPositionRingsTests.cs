// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A geometry's rings are positions (<see cref="GeoGeometry.PositionRings"/>), the same <see cref="GeoPosition"/> its
/// polygons are made of. The older <c>Rings</c>, the same rings as pairs of numbers, is a view of them kept for code written
/// against it: a geometry built either way reads the same rings both ways.</summary>
public class GeoGeometryPositionRingsTests
{
    private static readonly GeoPosition[] Square = [new(0, 0), new(4, 0), new(4, 4), new(0, 4), new(0, 0)];
    private static readonly GeoPosition[] Hole = [new(1, 1), new(1, 2), new(2, 2), new(2, 1), new(1, 1)];

    [Fact]
    public void AGeometryBuiltFromPositions_HasThePolygonsItsRingsSay()
    {
        var geometry = new GeoGeometry { Type = "Polygon", PositionRings = [Square, Hole] };

        var polygon = Assert.Single(geometry.Polygons);
        Assert.Equal(5, polygon.Exterior.Count);
        Assert.Single(polygon.Holes);
    }

    [Fact]
    public void AGeometryBuiltFromPositions_HandsThemBackAsTheyWereGiven()
    {
        IReadOnlyList<IReadOnlyList<GeoPosition>> rings = [Square];

        Assert.Same(rings, new GeoGeometry { Type = "LineString", PositionRings = rings }.PositionRings);
    }

    [Fact]
    public void AGeometryBuiltWithoutRings_HasNone()
    {
        var geometry = new GeoGeometry { Type = "Polygon" };

        Assert.Empty(geometry.PositionRings);
        Assert.Empty(geometry.Polygons);
    }

    [Fact]
    public void AGeometryReadFromGeoJson_HasItsRingsAsPositions()
    {
        const string json = """
            {"type":"FeatureCollection","features":[{"type":"Feature","properties":{},
             "geometry":{"type":"MultiLineString","coordinates":[[[10,20],[30,40]],[[5,6],[7,8],[9,10]]]}}]}
            """;

        var rings = Assert.Single(GeoJsonReader.Parse(json)).Geometry.PositionRings;

        Assert.Equal(2, rings.Count);
        Assert.Equal([new GeoPosition(10, 20), new GeoPosition(30, 40)], rings[0]);
        Assert.Equal(3, rings[1].Count);
    }

    [Fact]
    public void AGeometryBuiltFromPositions_IsWrittenAndReadBackTheSame()
    {
        var feature = new GeoFeature(new GeoGeometry { Type = "LineString", PositionRings = [[new(1, 2), new(3, 4)]] },
            new Dictionary<string, string> { ["NAME"] = "road" });

        var back = Assert.Single(GeoJsonReader.Parse(GeoJsonWriter.Write([feature])));

        Assert.Equal([new GeoPosition(1, 2), new GeoPosition(3, 4)], Assert.Single(back.Geometry.PositionRings));
    }
}
