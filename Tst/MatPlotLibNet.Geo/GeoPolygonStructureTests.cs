// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A polygon keeps its structure: an exterior and the holes cut out of it, wound the way a fill needs them (the exterior
/// counter-clockwise, a hole clockwise), and it is drawn as ONE path under the non-zero fill rule, so what a hole encloses is
/// not filled. South Africa is the case in the data: Lesotho lies inside it.</summary>
public class GeoPolygonStructureTests
{
    private static GeoFeature Country(string a3) =>
        NaturalEarth110m.Countries().Single(f => f.Properties["ADM0_A3"] == a3);

    /// <summary>The signed area of a ring in the plane of longitude and latitude (positive counter-clockwise).</summary>
    private static double SignedArea(IReadOnlyList<GeoPosition> ring)
    {
        double sum = 0;
        for (int i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            sum += a.Lon * b.Lat - b.Lon * a.Lat;
        }

        return sum / 2;
    }

    // ---- what the reader keeps ----------------------------------------------------------------------------------------

    [Fact]
    public void SouthAfrica_IsOnePolygon_WithOneHole_Lesotho()
    {
        var polygon = Assert.Single(Country("ZAF").Geometry.Polygons);

        Assert.Equal(82, polygon.Exterior.Count);
        var hole = Assert.Single(polygon.Holes);
        Assert.Equal(12, hole.Count);
        Assert.True(hole.All(p => p.Lon is > 26 and < 30 && p.Lat is > -31 and < -28), "the hole is where Lesotho lies");
    }

    [Fact]
    public void TheFlattenedRings_AreStillEveryRingOfEveryPolygon()
    {
        var geometry = Country("ZAF").Geometry;

        Assert.Equal(2, geometry.PositionRings.Count);
        Assert.Equal(geometry.Polygons.Sum(p => 1 + p.Holes.Count), geometry.PositionRings.Count);
    }

    [Fact]
    public void AMultiPolygon_IsOnePolygonForEachOfItsParts()
    {
        var geometry = NaturalEarth110m.Countries().Select(f => f.Geometry).First(g => g.Type == "MultiPolygon");

        Assert.True(geometry.Polygons.Count > 1);
        Assert.Equal(geometry.Polygons.Sum(p => 1 + p.Holes.Count), geometry.PositionRings.Count);
    }

    [Fact]
    public void EveryExteriorRunsCounterClockwise_AndEveryHoleClockwise()
    {
        var polygons = NaturalEarth110m.Countries().SelectMany(f => f.Geometry.Polygons).ToList();

        Assert.NotEmpty(polygons);
        Assert.All(polygons, p => Assert.True(SignedArea(p.Exterior) > 0, "an exterior winds counter-clockwise"));
        Assert.All(polygons.SelectMany(p => p.Holes), hole => Assert.True(SignedArea(hole) < 0, "a hole winds clockwise"));
    }

    [Fact]
    public void RingsGivenTheWrongWayRound_AreWound_ByTheirSignedArea()
    {
        GeoPosition[] clockwiseExterior = [new(0, 0), new(0, 10), new(10, 10), new(10, 0), new(0, 0)];
        GeoPosition[] counterClockwiseHole = [new(2, 2), new(8, 2), new(8, 8), new(2, 8), new(2, 2)];

        var polygon = GeoPolygon.FromRings(clockwiseExterior, [counterClockwiseHole]);

        Assert.True(SignedArea(polygon.Exterior) > 0);
        Assert.True(SignedArea(Assert.Single(polygon.Holes)) < 0);
        Assert.Equal(100, Math.Abs(SignedArea(polygon.Exterior)), 9);
    }

    [Fact]
    public void RingsAlreadyWoundTheRightWay_AreLeftAlone()
    {
        GeoPosition[] exterior = [new(0, 0), new(10, 0), new(10, 10), new(0, 10), new(0, 0)];

        var polygon = GeoPolygon.FromRings(exterior, []);

        Assert.Equal(exterior, polygon.Exterior);
    }

    [Fact]
    public void AGeometryBuiltFromItsRingsAlone_ReadsAPolygonsFirstRingAsItsExterior_AndTheRestAsHoles()
    {
        var geometry = new GeoGeometry
        {
            Type = "Polygon",
            PositionRings = [[new(0, 0), new(10, 0), new(10, 10), new(0, 10), new(0, 0)], [new(2, 2), new(2, 8), new(8, 8), new(8, 2), new(2, 2)]],
        };

        var polygon = Assert.Single(geometry.Polygons);

        Assert.Equal(5, polygon.Exterior.Count);
        Assert.Single(polygon.Holes);
    }

    [Fact]
    public void ALine_IsNoPolygon() =>
        Assert.Empty(NaturalEarth110m.Coastlines()[0].Geometry.Polygons);

    [Fact]
    public void AMultiPolygonBuiltFromItsRingsAlone_ReadsEveryRingAsAnExterior_ForItsRingsCarryNoGrouping()
    {
        var geometry = new GeoGeometry
        {
            Type = "MultiPolygon",
            PositionRings = [[new(0, 0), new(1, 0), new(1, 1), new(0, 0)], [new(5, 5), new(6, 5), new(6, 6), new(5, 5)]],
        };

        Assert.Equal(2, geometry.Polygons.Count);
        Assert.All(geometry.Polygons, polygon => Assert.Empty(polygon.Holes));
    }

    [Theory]
    [InlineData("Polygon")]       // no ring to be its exterior
    [InlineData("LineString")]
    [InlineData("Point")]
    public void AGeometryBuiltFromNoRings_IsNoPolygon(string type) =>
        Assert.Empty(new GeoGeometry { Type = type, PositionRings = [] }.Polygons);

    [Fact]
    public void ALineBuiltFromItsRingsAlone_IsNoPolygon_WhateverItsRings() =>
        Assert.Empty(new GeoGeometry { Type = "LineString", PositionRings = [[new(0, 0), new(1, 1)]] }.Polygons);

    // ---- what is drawn ------------------------------------------------------------------------------------------------------

    /// <summary>The paths of the picture, in the order drawn.</summary>
    private static List<SvgElement> Paths(string svg) => [.. svg.Elements("path").Where(path => path["d"] is not null)];

    /// <summary>The SVG without its clip outlines: under the matplotlib themes a map is cut off at its outline, a path in a
    /// <c>clipPath</c>, which is not drawn; these tests count the paths that are.</summary>
    private static string Drawn(string svg) => svg.WithoutClipPaths();

    private static string SouthAfricaOnPlateCarree() => Drawn(
        Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .Ocean(Colors.Blue)
            .AddSeries(new GeoPolygonSeries([Country("ZAF")], GeoProjection.PlateCarree) { Color = Colors.Green })
            .WithLegend(LegendPosition.Best, visible: false)).ToSvg());   // a legend's frame is a path too, and these tests count paths

    /// <summary>The pixel of a longitude and latitude on a PlateCarree map: the ocean (the first path) is the frame.</summary>
    private static (double X, double Y) Pixel(string svg, double lon, double lat)
    {
        var frame = Paths(svg)[0].PathPoints();
        double left = frame.Min(p => p.X), right = frame.Max(p => p.X), top = frame.Min(p => p.Y), bottom = frame.Max(p => p.Y);
        return (left + (lon + 180) / 360 * (right - left), top + (90 - lat) / 180 * (bottom - top));
    }

    /// <summary>The non-zero winding number of <paramref name="at"/> over every subpath of the path's data: 0 outside (and in a
    /// hole), anything else where the fill reaches.</summary>
    private static int Winding(SvgElement path, (double X, double Y) at)
    {
        int winding = 0;
        foreach (var points in path.Subpaths())
        {
            for (int i = 0; i < points.Length; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                if (a.Y <= at.Y)
                {
                    if (b.Y > at.Y && Cross(a, b, at) > 0) winding++;
                }
                else if (b.Y <= at.Y && Cross(a, b, at) < 0)
                {
                    winding--;
                }
            }
        }

        return winding;

        static double Cross(Point a, Point b, (double X, double Y) p) =>
            (b.X - a.X) * (p.Y - a.Y) - (p.X - a.X) * (b.Y - a.Y);
    }

    [Fact]
    public void ACountryWithAHole_IsOnePath_NotOnePolygonForEachRing()
    {
        string svg = SouthAfricaOnPlateCarree();

        // The ocean is the first path; South Africa the second: its exterior and its one hole are the two subpaths.
        var paths = Paths(svg);
        Assert.Equal(2, paths.Count);
        Assert.Equal(2, paths[1].Subpaths().Length);
    }

    [Fact]
    public void WhatTheHoleEncloses_IsNotFilled_AndTheRestOfTheCountryIs()
    {
        string svg = SouthAfricaOnPlateCarree();
        var path = Paths(svg)[1];

        // Lesotho lies at about 28.2 E, 29.6 S; the Northern Cape at about 21 E, 29 S.
        Assert.Equal(0, Winding(path, Pixel(svg, 28.2, -29.6)));
        Assert.NotEqual(0, Winding(path, Pixel(svg, 21, -29)));
    }

    [Fact]
    public void ACountryWithoutAHole_StillDrawsItsEveryPart()
    {
        var indonesia = NaturalEarth110m.Countries().Single(f => f.Properties["ADM0_A3"] == "IDN");
        string svg = Drawn(Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .AddSeries(new GeoPolygonSeries([indonesia], GeoProjection.PlateCarree) { Color = Colors.Green })).ToSvg());

        Assert.Equal(indonesia.Geometry.Polygons.Count, Paths(svg).Count);   // no ocean here: only its parts
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  a legend's frame is a path
        new DrawnToken("ALineFeature_IsStillDrawnAsLines", () => Drawn(Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .Coastlines(Colors.Black)
            .WithLegend(LegendPosition.Best, visible: false)).ToSvg()), "<polyline") { Absent = ["<path d="] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
