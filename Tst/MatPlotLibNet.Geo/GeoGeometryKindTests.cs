// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The kind of a GeoJSON geometry: one <see cref="GeoGeometryKind"/> for each <c>type</c> the library reads, chosen from
/// the type's name in one place, that reads its coordinates, writes them, and says whether it encloses an area.</summary>
public class GeoGeometryKindTests
{
    private static GeoGeometry Geometry(string type, IReadOnlyList<IReadOnlyList<GeoPosition>> rings) => new() { Type = type, PositionRings = rings };

    [Theory]
    [InlineData("Polygon", typeof(PolygonKind))]
    [InlineData("MultiPolygon", typeof(MultiPolygonKind))]
    [InlineData("LineString", typeof(LineStringKind))]
    [InlineData("MultiLineString", typeof(MultiLineStringKind))]
    [InlineData("Point", typeof(UnknownGeometryKind))]
    [InlineData("Unknown", typeof(UnknownGeometryKind))]
    [InlineData("polygon", typeof(UnknownGeometryKind))]
    public void EveryTypeName_HasItsKind(string type, Type kind) => Assert.IsType(kind, GeoGeometryKind.Of(type));

    [Theory]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    public void ATypeNameIsMappedToOneKindInstance(string type) =>
        Assert.Same(GeoGeometryKind.Of(type), GeoGeometryKind.Of(type));

    [Theory]
    [InlineData("Polygon", true)]
    [InlineData("MultiPolygon", true)]
    [InlineData("LineString", false)]
    [InlineData("MultiLineString", false)]
    [InlineData("Point", false)]
    public void OnlyThePolygonKinds_EncloseAnArea(string type, bool area) => Assert.Equal(area, GeoGeometryKind.Of(type).IsArea);

    [Fact]
    public void AGeometry_IsOfTheKindItsTypeNames() =>
        Assert.IsType<MultiPolygonKind>(Geometry("MultiPolygon", []).Kind);

    [Fact]
    public void ALine_HasNoPolygons() => Assert.Empty(Geometry("LineString", [[new(0, 0), new(1, 1)]]).Polygons);

    [Fact]
    public void APolygonBuiltFromNoRings_HasNoPolygons() => Assert.Empty(Geometry("Polygon", []).Polygons);

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  A type the library does not read keeps the rings it was given, and they are written as lines: the shape that holds any list of
        // rings, so nothing a caller put in is lost on the way out.
        new DrawnToken("AnUnknownType_IsWrittenWithItsName_AndItsRingsAsLines", () =>
        {
            var feature = new GeoFeature(Geometry("Point", [[new(1, 2)]]), []);
            return GeoJsonWriter.Write([feature]);
        }, "\"type\":\"Point\",\"coordinates\":[[[1,2]]]"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
