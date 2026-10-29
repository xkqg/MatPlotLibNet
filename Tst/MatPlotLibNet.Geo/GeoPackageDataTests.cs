// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>What a map writes of itself into the package data of its series and reads back: one writer and one reader of that
/// shape. A document that does not describe a map this package can make is no map, whatever way it falls short, so a figure
/// that carries it reads without the map and does not fail.</summary>
public class GeoPackageDataTests
{
    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private const string Countries = "ne_110m_admin_0_countries";

    public static TheoryData<string> DataThatDescribesNoMap =>
    [
        "[]",                                                                                    // not an object
        "\"map\"",                                                                               // not an object
        "{}",                                                                                    // no projection
        $$"""{ "projection": 3, "source": "{{Countries}}" }""",                                  // a projection that is no object
        $$"""{ "projection": {}, "source": "{{Countries}}" }""",                                 // a projection without a name
        $$"""{ "projection": { "name": null }, "source": "{{Countries}}" }""",                   // a name that is no text
        $$"""{ "projection": { "name": "Robinson", "parameters": { "lon_0": "x" } }, "source": "{{Countries}}" }""", // a number that is no number
        $$"""{ "projection": { "name": "Nowhere" }, "source": "{{Countries}}" }""",              // a projection the package does not ship
        """{ "projection": { "name": "Robinson" } }""",                                         // no features at all
        """{ "projection": { "name": "Robinson" }, "source": null }""",                          // a source that is no text
        """{ "projection": { "name": "Robinson" }, "source": "ne_10m_somewhere" }""",            // a source the package does not hold
        """{ "projection": { "name": "Robinson" }, "features": "none" }""",                      // features that are no collection
    ];

    [Fact]
    public void NoData_IsNoMap() =>
        Assert.Null(GeoPackageData.Read(null));

    [Theory]
    [MemberData(nameof(DataThatDescribesNoMap))]
    public void DataThatDescribesNoMap_IsNoMap(string text) =>
        Assert.Null(GeoPackageData.Read(Json(text)));

    [Fact]
    public void ABuiltInSource_ReadsAsTheFeaturesOfThatSet_UnderTheProjectionNamed()
    {
        var map = GeoPackageData.Read(Json($$"""{ "projection": { "name": "Robinson" }, "source": "{{Countries}}" }"""));

        Assert.NotNull(map);
        Assert.Equal("Robinson", map.Value.Projection.Name);
        Assert.Equal(NaturalEarth110m.Countries().Count, map.Value.Features.Count);
        Assert.False(map.Value.RawProjected);
    }

    [Fact]
    public void TheNumbersOfTheProjection_AreReadByTheirNames()
    {
        var map = GeoPackageData.Read(Json($$"""{ "projection": { "name": "Orthographic", "parameters": { "lat_0": 10, "lon_0": 20 } }, "source": "{{Countries}}" }"""));

        Assert.True(map!.Value.Projection.IsSameProjectionAs(new Orthographic(10, 20)));
    }

    [Fact]
    public void ParametersThatAreNoObject_LeaveTheProjectionItsDefaults()
    {
        var map = GeoPackageData.Read(Json($$"""{ "projection": { "name": "Orthographic", "parameters": 3 }, "source": "{{Countries}}" }"""));

        Assert.True(map!.Value.Projection.IsSameProjectionAs(new Orthographic()));
    }

    [Fact]
    public void FeaturesOfYourOwn_AreReadFromTheCollection()
    {
        var map = GeoPackageData.Read(Json("""
            { "projection": { "name": "PlateCarree" },
              "features": { "type": "FeatureCollection", "features": [
                { "type": "Feature", "properties": { "NAME": "Square" },
                  "geometry": { "type": "Polygon", "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]] } } ] } }
            """));

        var feature = Assert.Single(map!.Value.Features);
        Assert.Equal("Square", feature.Properties["NAME"]);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\"yes\"", false)]
    public void RawProjected_IsTrueOnlyWhenItIsTrue(string value, bool expected)
    {
        var map = GeoPackageData.Read(Json($$"""{ "projection": { "name": "Robinson" }, "source": "{{Countries}}", "rawProjected": {{value}} }"""));

        Assert.Equal(expected, map!.Value.RawProjected);
    }
}
