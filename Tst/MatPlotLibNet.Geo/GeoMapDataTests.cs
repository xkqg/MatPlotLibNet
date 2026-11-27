// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>What a <see cref="GeoMapData"/> writes of itself into the package data of its series and reads back: one writer and one
/// reader of that shape. A document that does not describe a map this package can make is no map, whatever way it falls short, so a figure
/// that carries it reads without the map and does not fail.</summary>
public class GeoMapDataTests
{
    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private const string Countries = "ne_110m_admin_0_countries";

    /// <summary>Data that describes no map this package can make, and the reason the reader gives, which the chart diagnostics and the
    /// MCP server's refusal print.</summary>
    public static TheoryData<string, string> DataThatDescribesNoMap => new()
    {
        { "[]", "its package data is no JSON object" },
        { "\"map\"", "its package data is no JSON object" },
        { "{}", "its package data names no projection" },
        { $$"""{ "projection": 3, "source": "{{Countries}}" }""", "its package data names no projection" },
        { $$"""{ "projection": {}, "source": "{{Countries}}" }""", "its package data names no projection" },
        { $$"""{ "projection": { "name": null }, "source": "{{Countries}}" }""", "its package data names no projection" },
        { $$"""{ "projection": { "name": "Robinson", "parameters": { "lon_0": "x" } }, "source": "{{Countries}}" }""", "the number lon_0 of its projection is no number" },
        { $$"""{ "projection": { "name": "Nowhere" }, "source": "{{Countries}}" }""", "its projection 'Nowhere' with no numbers is not one this package makes" },
        { """{ "projection": { "name": "Robinson" } }""", "it names no features: neither a built-in source nor a GeoJSON FeatureCollection" },
        { """{ "projection": { "name": "Robinson" }, "source": null }""", "its source is no name of a built-in set" },
        { """{ "projection": { "name": "Robinson" }, "source": "ne_10m_somewhere" }""", "its source 'ne_10m_somewhere' is not a built-in set of this package" },
        { """{ "projection": { "name": "Robinson" }, "features": "none" }""", "it names no features: neither a built-in source nor a GeoJSON FeatureCollection" },
    };

    [Fact]
    public void NoData_IsNoMap_BecauseItCarriesNone()
    {
        var read = GeoMapData.Read(null);

        Assert.Null(read.Map);
        Assert.Equal("it carries no package data", read.Why);
    }

    [Theory]
    [MemberData(nameof(DataThatDescribesNoMap))]
    public void DataThatDescribesNoMap_IsNoMap_AndSaysWhy(string text, string why)
    {
        var read = GeoMapData.Read(Json(text));

        Assert.Null(read.Map);
        Assert.Equal(why, read.Why);
    }
    [Fact]
    public void ABuiltInSource_ReadsAsTheFeaturesOfThatSet_UnderTheProjectionNamed()
    {
        var map = GeoMapData.Read(Json($$"""{ "projection": { "name": "Robinson" }, "source": "{{Countries}}" }"""));

        Assert.Null(map.Why);
        Assert.Equal("Robinson", map.Map!.Value.Projection.Name);
        Assert.Equal(NaturalEarth110m.Countries().Count, map.Map!.Value.Features.Count);
        Assert.False(map.Map!.Value.RawProjected);
    }

    [Fact]
    public void TheNumbersOfTheProjection_AreReadByTheirNames()
    {
        var map = GeoMapData.Read(Json($$"""{ "projection": { "name": "Orthographic", "parameters": { "lat_0": 10, "lon_0": 20 } }, "source": "{{Countries}}" }"""));

        Assert.True(map.Map!.Value.Projection.IsSameProjectionAs(new Orthographic(10, 20)));
    }

    [Fact]
    public void ParametersThatAreNoObject_LeaveTheProjectionItsDefaults()
    {
        var map = GeoMapData.Read(Json($$"""{ "projection": { "name": "Orthographic", "parameters": 3 }, "source": "{{Countries}}" }"""));

        Assert.True(map.Map!.Value.Projection.IsSameProjectionAs(new Orthographic()));
    }

    [Fact]
    public void FeaturesOfYourOwn_AreReadFromTheCollection()
    {
        var map = GeoMapData.Read(Json("""
            { "projection": { "name": "PlateCarree" },
              "features": { "type": "FeatureCollection", "features": [
                { "type": "Feature", "properties": { "NAME": "Square" },
                  "geometry": { "type": "Polygon", "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]] } } ] } }
            """));

        var feature = Assert.Single(map.Map!.Value.Features);
        Assert.Equal("Square", feature.Properties["NAME"]);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\"yes\"", false)]
    public void RawProjected_IsTrueOnlyWhenItIsTrue(string value, bool expected)
    {
        var map = GeoMapData.Read(Json($$"""{ "projection": { "name": "Robinson" }, "source": "{{Countries}}", "rawProjected": {{value}} }"""));

        Assert.Equal(expected, map.Map!.Value.RawProjected);
    }

    [Fact]
    public void AMapOfABuiltInSet_IsWrittenAsTheIdOfTheSet_AndReadsBackTheSame()
    {
        var written = new GeoMapData(new Orthographic(10, 20), NaturalEarth110m.Countries(), RawProjected: false).Write();

        Assert.Equal(Countries, written.GetProperty("source").GetString());
        Assert.False(written.TryGetProperty("features", out _));
        Assert.False(written.TryGetProperty("rawProjected", out _));
        var back = GeoMapData.Read(written).Map;
        Assert.True(back!.Value.Projection.IsSameProjectionAs(new Orthographic(10, 20)));
        Assert.Equal(NaturalEarth110m.Countries().Count, back.Value.Features.Count);
    }

    [Fact]
    public void AMapOfItsOwnFeatures_IsWrittenAsAFeatureCollection_AndReadsBackTheSame()
    {
        var square = new GeoFeature(
            new GeoGeometry { Type = "Polygon", PositionRings = [[new(0, 0), new(1, 0), new(1, 1), new(0, 1), new(0, 0)]] },
            new Dictionary<string, string> { ["NAME"] = "Square" });

        var written = new GeoMapData(GeoProjection.PlateCarree, [square], RawProjected: true).Write();

        Assert.False(written.TryGetProperty("source", out _));
        Assert.Equal("FeatureCollection", written.GetProperty("features").GetProperty("type").GetString());
        Assert.True(written.GetProperty("rawProjected").GetBoolean());
        var back = GeoMapData.Read(written).Map;
        Assert.Equal("Square", Assert.Single(back!.Value.Features).Properties["NAME"]);
        Assert.True(back.Value.RawProjected);
    }
}
