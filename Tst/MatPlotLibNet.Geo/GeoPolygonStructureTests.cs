// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Styling;

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

        Assert.Equal(2, geometry.Rings.Count);
        Assert.Equal(geometry.Polygons.Sum(p => 1 + p.Holes.Count), geometry.Rings.Count);
    }

    [Fact]
    public void AMultiPolygon_IsOnePolygonForEachOfItsParts()
    {
        var geometry = NaturalEarth110m.Countries().Select(f => f.Geometry).First(g => g.Type == "MultiPolygon");

        Assert.True(geometry.Polygons.Count > 1);
        Assert.Equal(geometry.Polygons.Sum(p => 1 + p.Holes.Count), geometry.Rings.Count);
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
            Rings = [[(0, 0), (10, 0), (10, 10), (0, 10), (0, 0)], [(2, 2), (2, 8), (8, 8), (8, 2), (2, 2)]],
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
            Rings = [[(0, 0), (1, 0), (1, 1), (0, 0)], [(5, 5), (6, 5), (6, 6), (5, 5)]],
        };

        Assert.Equal(2, geometry.Polygons.Count);
        Assert.All(geometry.Polygons, polygon => Assert.Empty(polygon.Holes));
    }

    [Theory]
    [InlineData("Polygon")]       // no ring to be its exterior
    [InlineData("LineString")]
    [InlineData("Point")]
    public void AGeometryBuiltFromNoRings_IsNoPolygon(string type) =>
        Assert.Empty(new GeoGeometry { Type = type, Rings = [] }.Polygons);

    [Fact]
    public void ALineBuiltFromItsRingsAlone_IsNoPolygon_WhateverItsRings() =>
        Assert.Empty(new GeoGeometry { Type = "LineString", Rings = [[(0, 0), (1, 1)]] }.Polygons);

    // ---- what is drawn ------------------------------------------------------------------------------------------------------

    private static readonly Regex PathData = new("<path d=\"(?<d>[^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex PathPoint = new(@"[ML] (?<x>[-0-9.eE]+) (?<y>[-0-9.eE]+)", RegexOptions.Compiled);

    private static string SouthAfricaOnPlateCarree() =>
        Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .Ocean(Colors.Blue)
            .AddSeries(new GeoPolygonSeries([Country("ZAF")], GeoProjection.PlateCarree) { Color = Colors.Green })).ToSvg();

    /// <summary>The pixel of a longitude and latitude on a PlateCarree map: the ocean (the first path) is the frame.</summary>
    private static (double X, double Y) Pixel(string svg, double lon, double lat)
    {
        var frame = PathPoint.Matches(PathData.Match(svg).Groups["d"].Value)
            .Select(m => (X: double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture), Y: double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture)))
            .ToArray();
        double left = frame.Min(p => p.X), right = frame.Max(p => p.X), top = frame.Min(p => p.Y), bottom = frame.Max(p => p.Y);
        return (left + (lon + 180) / 360 * (right - left), top + (90 - lat) / 180 * (bottom - top));
    }

    /// <summary>The non-zero winding number of <paramref name="at"/> over every subpath of the path's data: 0 outside (and in a
    /// hole), anything else where the fill reaches.</summary>
    private static int Winding(string pathData, (double X, double Y) at)
    {
        int winding = 0;
        foreach (var subpath in Regex.Split(pathData, "Z").Where(s => s.Trim().Length > 0))
        {
            var points = Regex.Matches(subpath, @"[ML] (?<x>[-0-9.eE]+) (?<y>[-0-9.eE]+)")
                .Select(m => (X: double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture), Y: double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture)))
                .ToArray();
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

        static double Cross((double X, double Y) a, (double X, double Y) b, (double X, double Y) p) =>
            (b.X - a.X) * (p.Y - a.Y) - (p.X - a.X) * (b.Y - a.Y);
    }

    [Fact]
    public void ACountryWithAHole_IsOnePath_NotOnePolygonForEachRing()
    {
        string svg = SouthAfricaOnPlateCarree();

        // The ocean is the first path; South Africa the second: its exterior and its one hole are the two subpaths.
        var country = PathData.Matches(svg)[1].Groups["d"].Value;
        Assert.Equal(2, PathData.Matches(svg).Count);
        Assert.Equal(2, Regex.Matches(country, "Z").Count);
    }

    [Fact]
    public void WhatTheHoleEncloses_IsNotFilled_AndTheRestOfTheCountryIs()
    {
        string svg = SouthAfricaOnPlateCarree();
        string path = PathData.Matches(svg)[1].Groups["d"].Value;

        // Lesotho lies at about 28.2 E, 29.6 S; the Northern Cape at about 21 E, 29 S.
        Assert.Equal(0, Winding(path, Pixel(svg, 28.2, -29.6)));
        Assert.NotEqual(0, Winding(path, Pixel(svg, 21, -29)));
    }

    [Fact]
    public void ACountryWithoutAHole_StillDrawsItsEveryPart()
    {
        var indonesia = NaturalEarth110m.Countries().Single(f => f.Properties["ADM0_A3"] == "IDN");
        string svg = Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .AddSeries(new GeoPolygonSeries([indonesia], GeoProjection.PlateCarree) { Color = Colors.Green })).ToSvg();

        Assert.Equal(indonesia.Geometry.Polygons.Count, PathData.Matches(svg).Count);   // no ocean here: only its parts
    }

    [Fact]
    public void ALineFeature_IsStillDrawnAsLines()
    {
        string svg = Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .Coastlines(Colors.Black)).ToSvg();

        Assert.Contains("<polyline", svg);
        Assert.DoesNotContain("<path d=", svg);
    }
}
