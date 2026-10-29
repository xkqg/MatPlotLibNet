// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A map comes back from JSON: the series writes its projection (the kind and the numbers it was made with) and its
/// features (a source id for the built-in sets, the features themselves otherwise) into the figure's package data, and the
/// package registers the reader that rebuilds it. The picture read back is the picture that was written.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoMapRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    private static string RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure)).ToSvg();

    private static Figure Map(Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(600, 330).AddSubPlot(1, 1, 1, axes).Build();

    // ---- the four layers -----------------------------------------------------------------------------------------------

    [Fact]
    public void Land_ComesBack_AsTheSamePicture()
    {
        var figure = Map(ax => ax.WithProjection(GeoProjection.Robinson).Land(Colors.Green));

        Assert.Equal(figure.ToSvg(), RoundTrip(figure));
    }

    [Fact]
    public void Borders_ComeBack_AsTheSamePicture()
    {
        var figure = Map(ax => ax.WithProjection(GeoProjection.Mercator).Borders(Colors.Red, 0.4));

        Assert.Equal(figure.ToSvg(), RoundTrip(figure));
    }

    [Fact]
    public void Coastlines_ComeBack_AsTheSamePicture()
    {
        var figure = Map(ax => ax.WithProjection(GeoProjection.Mollweide).Coastlines(Colors.Navy, 0.9));

        Assert.Equal(figure.ToSvg(), RoundTrip(figure));
    }

    [Fact]
    public void TheOcean_ComesBack_AsTheSamePicture()
    {
        var figure = Map(ax => ax.WithProjection(GeoProjection.EqualEarth).Ocean(Colors.Blue));

        Assert.Equal(figure.ToSvg(), RoundTrip(figure));
    }

    [Fact]
    public void AllFourTogether_ComeBack_AsTheSamePicture()
    {
        var figure = Map(ax => ax.WithProjection(GeoProjection.Robinson)
            .Ocean(Colors.Blue).Land(Colors.Green).Coastlines(Colors.White).Borders(Colors.Gray));

        Assert.Equal(figure.ToSvg(), RoundTrip(figure));
    }

    [Fact]
    public void TheSettingsOfALayer_ComeBack_TheOrderItIsDrawnInIncluded()
    {
        var figure = Map(ax => ax.WithProjection(GeoProjection.Robinson).Land(Colors.Green).Ocean(Colors.Blue));

        var back = Serializer.FromJson(Serializer.ToJson(figure)).SubPlots[0].Series.Cast<GeoPolygonSeries>().ToList();

        Assert.Equal(["Land", "Ocean"], back.Select(s => s.Label));
        Assert.Equal([-1, -2], back.Select(s => s.ZOrder));
        Assert.True(back[1].IsRawProjected);
        Assert.Equal(Colors.Green, back[0].Color);
    }

    // ---- the projections -----------------------------------------------------------------------------------------------

    /// <summary>All 13 projections, each with numbers other than its defaults wherever it takes any.</summary>
    private static readonly IGeoProjection[] Projections =
    [
        new PlateCarree(), new Mercator(), new Robinson(), new Mollweide(), new Sinusoidal(), new NaturalEarthProjection(), new EqualEarth(),
        new Orthographic(12, 34), new LambertConformal(20, 50, 10, 45), new AlbersEqualArea(25, 55, -100, 40),
        new AzimuthalEquidistant(-30, 60), new Stereographic(-50, -70), new TransverseMercator(15),
    ];

    [Fact]
    public void TheTableHoldsEveryProjectionThePackageShips() =>
        Assert.Equal(
            typeof(IGeoProjection).Assembly.GetTypes().Count(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && typeof(IGeoProjection).IsAssignableFrom(t)),
            Projections.Select(p => p.GetType()).Distinct().Count());

    public static TheoryData<int> ProjectionIndices => [.. Enumerable.Range(0, Projections.Length)];

    [Theory]
    [MemberData(nameof(ProjectionIndices))]
    public void AProjection_ComesBack_AsTheSameProjection(int index)
    {
        var projection = Projections[index];
        var figure = Map(ax => ax.WithProjection(projection).Ocean(Colors.Blue));

        var back = Assert.IsType<GeoPolygonSeries>(Serializer.FromJson(Serializer.ToJson(figure)).SubPlots[0].Series[0]).Projection;

        Assert.IsType(projection.GetType(), back);
        Assert.Equal(projection.Name, back.Name);
        Assert.Equal(projection.Bounds, back.Bounds);
        foreach (var (lat, lon) in new[] { (0.0, 0.0), (45.0, 10.0), (-30.0, 120.0), (60.0, -75.0), (12.0, 34.0) })
        {
            var written = projection.Forward(lat, lon);
            var read = back.Forward(lat, lon);
            Assert.Equal(written.X, read.X);
            Assert.Equal(written.Y, read.Y);
        }
    }

    [Fact]
    public void AProjectionsNumbers_AreWrittenByTheirProjNames_AllOfThem()
    {
        string json = Serializer.ToJson(Map(ax => ax.WithProjection(new LambertConformal(20, 50, 10, 45)).Ocean(Colors.Blue)));

        Assert.Contains("\"lat_1\":20", json);
        Assert.Contains("\"lat_2\":50", json);
        Assert.Contains("\"lon_0\":10", json);
        Assert.Contains("\"lat_0\":45", json);
    }

    // ---- the features --------------------------------------------------------------------------------------------------

    [Fact]
    public void ABuiltInSet_IsWrittenAsItsSourceId_AndTheJsonStaysSmall()
    {
        string json = Serializer.ToJson(Map(ax => ax.WithProjection(GeoProjection.Robinson).Land(Colors.Green)));

        Assert.Contains("ne_110m_admin_0_countries", json);
        Assert.True(json.Length < 3000, $"the JSON of a map of 177 countries is {json.Length} characters");
    }

    [Fact]
    public void TheCoastlinesSource_IsNamedToo()
    {
        string json = Serializer.ToJson(Map(ax => ax.WithProjection(GeoProjection.Robinson).Coastlines(Colors.Black)));

        Assert.Contains("ne_110m_coastline", json);
    }

    [Fact]
    public void FeaturesOfYourOwn_AreEmbeddedExactly_HolesAndPropertiesIncluded()
    {
        var own = new List<GeoFeature>
        {
            new(new GeoGeometry
                {
                    Type = "Polygon",
                    Rings = [[(0, 0), (20, 0), (20, 20), (0, 20), (0, 0)], [(5, 5), (5, 15), (15, 15), (15, 5), (5, 5)]],
                },
                new Dictionary<string, string> { ["ADM0_A3"] = "AAA", ["NAME"] = "Alpha" }),
            new(new GeoGeometry { Type = "LineString", Rings = [[(30, 30), (40, 35), (50, 30)]] }, new Dictionary<string, string>()),
        };
        var figure = Map(ax => ax.WithProjection(GeoProjection.PlateCarree)
            .AddSeries(new GeoPolygonSeries(own, GeoProjection.PlateCarree) { Color = Colors.Orange, StrokeColor = Colors.Black }));

        var back = Assert.IsType<GeoPolygonSeries>(Serializer.FromJson(Serializer.ToJson(figure)).SubPlots[0].Series[0]);

        Assert.Equal(2, back.Features.Count);
        var polygon = Assert.Single(back.Features[0].Geometry.Polygons);
        Assert.Equal(5, polygon.Exterior.Count);
        Assert.Equal(5, Assert.Single(polygon.Holes).Count);
        Assert.Equal("Alpha", back.Features[0].Properties["NAME"]);
        Assert.Equal("LineString", back.Features[1].Geometry.Type);
        Assert.Equal(figure.ToSvg(), Serializer.FromJson(Serializer.ToJson(figure)).ToSvg());
    }

    // ---- what is not a map -----------------------------------------------------------------------------------------------

    private static (Figure Figure, List<ChartDiagnostic> Diagnostics) Read(string json)
    {
        var diagnostics = new List<ChartDiagnostic>();
        void Handler(ChartDiagnostic d) => diagnostics.Add(d);
        ChartDiagnostics.Emitted += Handler;
        try { return (Serializer.FromJson(json), diagnostics); }
        finally { ChartDiagnostics.Emitted -= Handler; }
    }

    [Fact]
    public void AMapWrittenBefore_WithNoPackageData_IsDroppedWithTheDiagnostic_AsItWas()
    {
        const string json = """{"subPlots":[{"series":[{"type":"geo_polygon","label":"Land"}]}]}""";

        var (figure, diagnostics) = Read(json);

        Assert.Empty(figure.SubPlots[0].Series);
        Assert.Contains(diagnostics, d => d.Message.Contains("geo_polygon", StringComparison.Ordinal));
    }

    /// <summary>What the 1.23.0 build wrote for an ocean, a land and a set of coastlines on a Robinson map, byte for byte (written by
    /// that build, checked out at its tag). A map in it carries no projection and no features, so it cannot be rebuilt: it reads as
    /// it always did, without the maps and with the unknown-series diagnostic for each, and the rest of the figure is intact.</summary>
    private const string WrittenBy1230 =
        """{"width":600,"height":330,"dpi":96,"subPlots":[{"spines":{"top":{"visible":false,"positionValue":0,"lineWidth":0.8},"bottom":{"visible":false,"positionValue":0,"lineWidth":0.8},"left":{"visible":false,"positionValue":0,"lineWidth":0.8},"right":{"visible":false,"positionValue":0,"lineWidth":0.8}},"xAxis":{"scale":"linear","majorTicks":{"visible":false}},"yAxis":{"scale":"linear","majorTicks":{"visible":false}},"zAxis":{"scale":"linear"},"grid":{"visible":false},"series":[{"type":"geo_polygon","label":"Ocean","zOrder":-2},{"type":"geo_polygon","label":"Land","zOrder":-1},{"type":"geo_polygon","label":"Coastlines"}]}]}""";

    [Fact]
    public void TheFixtureIsTheBytesThe1230BuildWrote() =>
        Assert.Equal("beb5eabb33e8133d28e52c5e5bab43062795e25ef0330c7d981c651102d30255",
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(WrittenBy1230))));

    [Fact]
    public void AFigureWrittenBy1230_ReadsWithoutItsMaps_AsItDid_WithOneDiagnosticForEach()
    {
        var (figure, diagnostics) = Read(WrittenBy1230);

        Assert.Equal(600, figure.Width);
        Assert.Empty(figure.SubPlots[0].Series);
        Assert.Equal(3, diagnostics.Count(d => d.Message.Contains("geo_polygon", StringComparison.Ordinal)));
    }

    [Fact]
    public void AProjectionThisPackageDoesNotKnow_IsDroppedWithTheDiagnostic()
    {
        const string json = """{"subPlots":[{"series":[{"type":"geo_polygon","packageData":{"projection":{"name":"Peirce"},"source":"ne_110m_admin_0_countries"}}]}]}""";

        var (figure, diagnostics) = Read(json);

        Assert.Empty(figure.SubPlots[0].Series);
        Assert.NotEmpty(diagnostics);
    }

    [Fact]
    public void ASourceThisPackageDoesNotKnow_IsDroppedWithTheDiagnostic()
    {
        const string json = """{"subPlots":[{"series":[{"type":"geo_polygon","packageData":{"projection":{"name":"Robinson"},"source":"ne_10m_unheard_of"}}]}]}""";

        var (figure, diagnostics) = Read(json);

        Assert.Empty(figure.SubPlots[0].Series);
        Assert.NotEmpty(diagnostics);
    }

    // ---- the reader is the package's -------------------------------------------------------------------------------------

    [Fact]
    public void TheReader_IsRegisteredWhenThePackageLoads() =>
        Assert.Contains("geo_polygon", SeriesRegistry.Discriminators);

    [Fact]
    public void TheReader_IsGoneWhenTheRegistryIsReset_AndRegisterReaderBringsItBack()
    {
#pragma warning disable CS0618 // the public reset is the one a test outside the core project can reach
        SeriesRegistry.ResetForTests();
#pragma warning restore CS0618
        try
        {
            Assert.DoesNotContain("geo_polygon", SeriesRegistry.Discriminators);
        }
        finally
        {
            GeoPolygonSeries.RegisterReader();
        }

        Assert.Contains("geo_polygon", SeriesRegistry.Discriminators);
        GeoPolygonSeries.RegisterReader();   // idempotent
        Assert.Single(SeriesRegistry.Discriminators, d => d == "geo_polygon");
    }
}
