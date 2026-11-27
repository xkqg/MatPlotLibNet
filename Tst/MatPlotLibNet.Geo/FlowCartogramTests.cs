// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The fast flow-based cartogram of Gastner, Seguy and More over regions of the plane: each region ends with an area in
/// proportion to its value, a region with no value keeps its area, the regions together keep theirs, and a border two regions
/// share stays one line.</summary>
public class FlowCartogramTests
{
    private static IReadOnlyList<ProjectedPoint> Ring(params (double X, double Y)[] corners) =>
        [.. corners.Select(c => new ProjectedPoint(c.X, c.Y)), new ProjectedPoint(corners[0].X, corners[0].Y)];

    private static IReadOnlyList<GeoAxesExtensions.ProjectedPolygon> Square(double x, double y, double side = 1) => Box(x, y, side, side);

    private static IReadOnlyList<GeoAxesExtensions.ProjectedPolygon> Box(double x, double y, double width, double height) =>
        [new GeoAxesExtensions.ProjectedPolygon(Ring((x, y), (x + width, y), (x + width, y + height), (x, y + height)), [])];

    private static double RingArea(IReadOnlyList<GeoPosition> ring)
    {
        double twice = 0;
        for (int i = 0; i < ring.Count; i++)
        {
            var p = ring[i];
            var q = ring[(i + 1) % ring.Count];
            twice += p.Lon * q.Lat - q.Lon * p.Lat;
        }

        return Math.Abs(twice) / 2;
    }

    private static double AreaOf(GeoGeometry geometry) =>
        geometry.Polygons.Sum(p => RingArea(p.Exterior) - p.Holes.Sum(RingArea));

    [Fact]
    public void EachRegion_EndsWithAnAreaInProportionToItsValue()
    {
        var resized = new GeoAxesExtensions.FlowCartogram([Square(0, 0), Square(1, 0)], [1.0, 3.0]).Equalized();

        Assert.Equal(3.0, AreaOf(resized[1]) / AreaOf(resized[0]), 0.03);
    }

    [Fact]
    public void AMapTallerThanItIsWide_IsResizedTheSameWay()
    {
        var resized = new GeoAxesExtensions.FlowCartogram([Square(0, 0), Square(0, 1)], [1.0, 3.0]).Equalized();

        Assert.Equal(3.0, AreaOf(resized[1]) / AreaOf(resized[0]), 0.03);
    }

    [Fact]
    public void ARegionThatTakesAlmostTheWholeMap_TakesItsShare()
    {
        // A heavy corner of a 3 x 3 map: each light square is raised to max(0.025 / 9, 0.00025) of the values (it owns a ninth of
        // the perimeter), 27.8 against 10,000, so the corner takes about 8.8 of the 9 square units, 360 times a light square.
        var regions = new List<IReadOnlyList<GeoAxesExtensions.ProjectedPolygon>>();
        for (int i = 0; i < 9; i++) regions.Add(Square(i % 3, i / 3));
        double[] values = [1e4, 1, 1, 1, 1, 1, 1, 1, 1];

        var resized = new GeoAxesExtensions.FlowCartogram(regions, values).Equalized();

        double light = 0.025 / 9 * (1e4 + 8);
        Assert.Equal(1e4 / light, AreaOf(resized[0]) / AreaOf(resized[8]), 1e4 / light * 0.03);
    }

    [Fact]
    public void TheRegions_KeepTheAreaTheyHadTogether()
    {
        var resized = new GeoAxesExtensions.FlowCartogram([Square(0, 0), Square(1, 0), Square(0, 1)], [1.0, 3.0, 2.0]).Equalized();

        Assert.Equal(3.0, resized.Sum(AreaOf), 1e-9);
    }

    [Fact]
    public void ABorderTwoRegionsShare_StaysOneLine()
    {
        // The squares share the edge x = 1; every vertex the flow puts on it in one region is a vertex of the other.
        var resized = new GeoAxesExtensions.FlowCartogram([Square(0, 0), Square(1, 0)], [1.0, 4.0]).Equalized();

        var left = resized[0].Polygons[0].Exterior.ToHashSet();
        var right = resized[1].Polygons[0].Exterior.ToHashSet();
        int shared = left.Count(right.Contains);

        Assert.True(shared > 10, $"{shared} vertices shared");
        Assert.Equal(2.0, AreaOf(resized[0]) + AreaOf(resized[1]), 1e-9);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-2.0)]
    public void ARegionOfNoValue_KeepsItsArea(double noValue)
    {
        var resized = new GeoAxesExtensions.FlowCartogram([Square(0, 0), Square(1, 0), Square(2, 0)], [1.0, 3.0, noValue]).Equalized();

        Assert.Equal(1.0, AreaOf(resized[2]), 0.02);
        Assert.Equal(3.0, AreaOf(resized[1]) / AreaOf(resized[0]), 0.03);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1e-6)]
    public void ARegionOfATinyShare_TakesTheShareItsOutlineAsksFor(double tiny)
    {
        // As the method's own program does (go_cart, fill_with_density1): a region whose share of the values is below
        // max(0.025 * its share of the perimeter, 0.00025) takes that share. Three unit squares have a third of the perimeter
        // each, so the first takes 0.025 / 3 of the values 1 + 2 = 3 before the shares are made: 0.025 against 1.
        var resized = new GeoAxesExtensions.FlowCartogram([Square(0, 0), Square(1, 0), Square(0, 1)], [tiny, 1.0, 2.0]).Equalized();

        Assert.Equal(0.025, AreaOf(resized[0]) / AreaOf(resized[1]), 0.0005);
    }

    [Fact]
    public void RegionsOfNoValueAtAll_AreLeftAsTheyAre()
    {
        var regions = new[] { Square(0, 0), Square(1, 0) };

        var resized = new GeoAxesExtensions.FlowCartogram(regions, [double.NaN, 0.0]).Equalized();

        Assert.Equal(regions[0][0].Exterior.Select(p => new GeoPosition(p.X, p.Y)), resized[0].Polygons[0].Exterior);
        Assert.Equal("Polygon", resized[1].Type);
    }

    [Fact]
    public void ARegionWithAHole_KeepsItsHole_AndTheIslandInItFlowsWithIt()
    {
        // A square with a square hole, the hole filled by a second region, and a third square beside it.
        IReadOnlyList<GeoAxesExtensions.ProjectedPolygon> withHole =
            [new GeoAxesExtensions.ProjectedPolygon(Ring((0, 0), (3, 0), (3, 3), (0, 3)), [Ring((1, 1), (2, 1), (2, 2), (1, 2))])];

        var resized = new GeoAxesExtensions.FlowCartogram([withHole, Square(1, 1), Square(3, 0, 3)], [8.0, 2.0, 9.0]).Equalized();

        var polygon = Assert.Single(resized[0].Polygons);
        var hole = Assert.Single(polygon.Holes);
        Assert.Equal(hole.ToHashSet(), resized[1].Polygons[0].Exterior.ToHashSet());
        Assert.Equal(8.0 / 2.0, AreaOf(resized[0]) / AreaOf(resized[1]), 0.08);
    }

    [Fact]
    public void ARegionOfSeveralPolygons_IsAMultiPolygon()
    {
        IReadOnlyList<GeoAxesExtensions.ProjectedPolygon> two = [Square(0, 0)[0], Square(2, 0)[0]];

        var resized = new GeoAxesExtensions.FlowCartogram([two, Square(1, 0)], [2.0, 1.0]).Equalized();

        Assert.Equal("MultiPolygon", resized[0].Type);
        Assert.Equal(2, resized[0].Polygons.Count);
        Assert.Equal(2.0, AreaOf(resized[0]) / AreaOf(resized[1]), 0.03);
    }

    /// <summary>A map that is a strip: the lattice is one cell across it, so every vertex stands within half a cell of a wall of the
    /// lattice, where the flux across the wall is nothing and the density is read from the cell beside it, and a step that would carry
    /// a vertex out of the lattice is taken again shorter.</summary>
    [Theory]
    [InlineData(100.0, 0.1)]
    [InlineData(0.1, 100.0)]
    public void AMapThatIsAStripOneCellAcross_IsResizedAlongItsLength(double width, double height)
    {
        bool wide = width > height;
        var regions = wide
            ? new[] { Box(0, 0, width / 2, height), Box(width / 2, 0, width / 2, height) }
            : new[] { Box(0, 0, width, height / 2), Box(0, height / 2, width, height / 2) };

        var resized = new GeoAxesExtensions.FlowCartogram(regions, [1.0, 3.0]).Equalized();

        // The flows stop once no region is more than 1 % off the area its value asks for (a quarter and three quarters).
        Assert.InRange(AreaOf(resized[0]) / (width * height / 4), 0.99, 1.01);
        Assert.InRange(AreaOf(resized[1]) / (width * height * 3 / 4), 0.99, 1.01);
        Assert.Equal(width * height, resized.Sum(AreaOf), 1e-9);
    }

    [Fact]
    public void ARegionWithNoAreaOnTheMap_IsLeftOutOfTheCartogram()
    {
        static GeoFeature Region(string name, params (double Lon, double Lat)[] ring) =>
            new(new GeoGeometry { Type = "Polygon", PositionRings = [[.. ring.Select(p => new GeoPosition(p.Lon, p.Lat))]] },
                new Dictionary<string, string> { ["name"] = name });
        var flat = Region("flat", (0, 0), (5, 0), (10, 0), (0, 0));
        var square = Region("square", (0, 10), (5, 10), (5, 15), (0, 15), (0, 10));

        var flowed = GeoAxesExtensions.FlowedOn([flat, square], GeoProjection.PlateCarree, _ => 1);

        Assert.Equal(["square"], flowed.Select(feature => feature.Properties["name"]));
    }

    /// <summary>A step of a vertex holds when its Euler landing and its midpoint landing lie within the tolerance (squared) of each
    /// other and the midpoint lies inside the lattice, its walls included; anything else (a landing off by more, a point beyond any
    /// of the four walls, a landing that is no number) is taken again shorter.</summary>
    [Theory]
    [InlineData(1.0, 1.0, 1.0, 1.0, true)]
    [InlineData(0.0, 0.0, 0.0, 0.0, true)]
    [InlineData(4.0, 4.0, 4.0, 4.0, true)]
    [InlineData(1.0, 1.0, 1.0005, 1.0, true)]
    [InlineData(1.0, 1.0, 1.1, 1.0, false)]
    [InlineData(-0.1, 1.0, -0.1, 1.0, false)]
    [InlineData(4.1, 1.0, 4.1, 1.0, false)]
    [InlineData(1.0, -0.1, 1.0, -0.1, false)]
    [InlineData(1.0, 4.1, 1.0, 4.1, false)]
    [InlineData(1.0, 1.0, double.NaN, 1.0, false)]
    public void AStep_HoldsWhenItsTwoLandingsAgree_AndItsMidpointStaysInTheLattice(double eulerX, double eulerY, double midX, double midY, bool holds) =>
        Assert.Equal(holds, GeoAxesExtensions.FlowCartogram.Holds(
            new Rendering.Point(eulerX, eulerY), new Rendering.Point(midX, midY), tolerance: 1e-6, nx: 4, ny: 4));
}
