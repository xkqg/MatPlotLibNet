// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The one place a geometry is made from polygons (<c>GeoGeometry.OfPolygons</c>): the cells of an H3 map, a region a
/// cartogram resized or flowed and the ocean of a map all take their <c>Type</c>, their rings and their polygons from it, so the
/// three cannot disagree on what a Polygon and what a MultiPolygon is.</summary>
public class GeoGeometryOfPolygonsTests
{
    private static readonly GeoPosition[] Square = [new(0, 0), new(4, 0), new(4, 4), new(0, 4), new(0, 0)];
    private static readonly GeoPosition[] Hole = [new(2, 2), new(2, 1), new(1, 1), new(1, 2), new(2, 2)];
    private static readonly GeoPosition[] Island = [new(10, 0), new(12, 0), new(12, 2), new(10, 2), new(10, 0)];

    [Fact]
    public void OnePolygon_IsAPolygon_ItsExteriorThenItsHolesAreTheRings()
    {
        var polygon = GeoPolygon.FromRings(Square, [Hole]);

        var geometry = GeoGeometry.OfPolygons([polygon]);

        Assert.Equal("Polygon", geometry.Type);
        Assert.Equal([polygon], geometry.Polygons);
        Assert.Equal([polygon.Exterior, polygon.Holes[0]], geometry.PositionRings);
    }

    [Fact]
    public void SeveralPolygons_AreAMultiPolygon_TheirRingsOnePolygonAfterTheOther()
    {
        var withHole = GeoPolygon.FromRings(Square, [Hole]);
        var island = GeoPolygon.FromRings(Island, []);

        var geometry = GeoGeometry.OfPolygons([withHole, island]);

        Assert.Equal("MultiPolygon", geometry.Type);
        Assert.Equal([withHole, island], geometry.Polygons);
        Assert.Equal([withHole.Exterior, withHole.Holes[0], island.Exterior], geometry.PositionRings);
    }

    [Fact]
    public void NoPolygon_IsAMultiPolygonOfNone()
    {
        var geometry = GeoGeometry.OfPolygons([]);

        Assert.Equal("MultiPolygon", geometry.Type);
        Assert.Empty(geometry.Polygons);
        Assert.Empty(geometry.PositionRings);
    }

    [Fact]
    public void TheKindOfTheGeometryIsAnArea()
    {
        Assert.True(GeoGeometry.OfPolygons([GeoPolygon.FromRings(Square, [])]).Kind.IsArea);
        Assert.True(GeoGeometry.OfPolygons([]).Kind.IsArea);
    }

    [Fact]
    public void AGeometryMadeFromPolygons_IsWrittenAndReadBackAsTheSamePolygons()
    {
        var geometry = GeoGeometry.OfPolygons([GeoPolygon.FromRings(Square, [Hole]), GeoPolygon.FromRings(Island, [])]);
        var feature = new GeoFeature(geometry, new Dictionary<string, string>());

        var back = Assert.Single(GeoJsonReader.Parse(GeoJsonWriter.Write([feature])));

        Assert.Equal("MultiPolygon", back.Geometry.Type);
        Assert.Equal(geometry.Polygons.Count, back.Geometry.Polygons.Count);
        for (int i = 0; i < geometry.Polygons.Count; i++)
        {
            Assert.Equal(geometry.Polygons[i].Exterior, back.Geometry.Polygons[i].Exterior);
            Assert.Equal(geometry.Polygons[i].Holes, back.Geometry.Polygons[i].Holes);
        }
    }
}
