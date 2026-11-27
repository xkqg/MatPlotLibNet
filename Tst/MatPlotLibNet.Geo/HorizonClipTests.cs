// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Geo.Series;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A ring or a line on a globe is cut at its horizon, as cartopy cuts it: the part on the far side is left out, a polygon
/// is closed along the edge of the globe between where it leaves and where it comes back, a line ends on that edge. The numbers
/// are cartopy 0.26's (shapely 2.2), read with python on the same rings:
/// <code>
/// tgt = ccrs.Orthographic(central_longitude=0, central_latitude=0)
/// out = tgt.project_geometry(shapely.geometry.Polygon(ring), ccrs.PlateCarree())
/// out.area / 6378137.0**2
/// </code>
/// The rings are those a map has: a position every degree along each edge, wound counter-clockwise (the winding
/// <see cref="GeoPolygon.FromRings"/> gives an exterior).</summary>
public class HorizonClipTests
{
    /// <summary>The radius of the globe in the projected plane, which is measured in degrees of arc.</summary>
    private const double Radius = 180 / Math.PI;

    private static readonly Orthographic Globe = new(0, 0);

    /// <summary>The plane is the picture here: a point lands where the projection puts it.</summary>
    private static Point Plane(ProjectedPoint point) => new(point.X, point.Y);

    private static Point Placed(ProjectedPoint point) => Plane(point);

    private static List<List<Point>> All(RingPieces pieces) => [.. Enumerable.Range(0, pieces.Count).Select(i => pieces[i])];

    /// <summary>A ring through <paramref name="corners"/> with a position every degree along each edge, as the python reference
    /// lays it out.</summary>
    private static GeoPosition[] Dense(params (double Lon, double Lat)[] corners)
    {
        var ring = new List<GeoPosition>();
        for (int i = 0; i + 1 < corners.Length; i++)
        {
            var (x0, y0) = corners[i];
            var (x1, y1) = corners[i + 1];
            int steps = Math.Max(1, (int)Math.Round(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0))));
            for (int step = 0; step < steps; step++)
                ring.Add(new GeoPosition(x0 + (x1 - x0) * step / steps, y0 + (y1 - y0) * step / steps));
        }

        ring.Add(new GeoPosition(corners[^1].Lon, corners[^1].Lat));
        return [.. ring];
    }

    /// <summary>The area of the ring in the plane, in units of the globe's radius squared, counter-clockwise positive.</summary>
    private static double Area(IReadOnlyList<Point> ring)
    {
        double twice = 0;
        for (int i = 0; i < ring.Count; i++)
        {
            var p = ring[i];
            var q = ring[(i + 1) % ring.Count];
            twice += p.X * q.Y - q.X * p.Y;
        }

        return twice / 2 / (Radius * Radius);
    }

    private static double AreaOf(RingPieces pieces) => All(pieces).Sum(Area);

    private static double Radial(Point p) => Math.Sqrt(p.X * p.X + p.Y * p.Y);

    /// <summary>How far a point is from the edge of the globe as the map draws it, the polygon of its outline.</summary>
    private static double DistanceFromTheOutline(Point p)
    {
        var outline = Globe.Outline();
        double best = double.MaxValue;
        for (int i = 0; i + 1 < outline.Count; i++)
        {
            double ax = outline[i].Lon, ay = outline[i].Lat, bx = outline[i + 1].Lon, by = outline[i + 1].Lat;
            double dx = bx - ax, dy = by - ay;
            double t = Math.Clamp(((p.X - ax) * dx + (p.Y - ay) * dy) / (dx * dx + dy * dy), 0, 1);
            best = Math.Min(best, double.Hypot(p.X - (ax + t * dx), p.Y - (ay + t * dy)));
        }

        return best;
    }

    private static void AssertEveryPoint_IsOnTheGlobe(RingPieces pieces)
    {
        foreach (var point in All(pieces).SelectMany(piece => piece))
            Assert.True(Radial(point) <= Radius * (1 + 1e-9), $"({point.X}, {point.Y}) is beyond the horizon.");
    }

    private static GeoPixelMapping.HorizonClip Clip() => GeoPixelMapping.HorizonClip.Of(Globe, Plane)!;

    [Fact]
    public void OnlyAProjectionThatShowsOneSideOfTheGlobeHasAHorizon()
    {
        Assert.NotNull(GeoPixelMapping.HorizonClip.Of(new Orthographic(10, 20), Plane));
        Assert.Null(GeoPixelMapping.HorizonClip.Of(new PlateCarree(), Plane));
        Assert.Null(GeoPixelMapping.HorizonClip.Of(new Robinson(), Plane));
        Assert.Null(GeoPixelMapping.HorizonClip.Of(new Stereographic(), Plane));
    }

    [Fact]
    public void ARingOnTheNearSide_IsKeptWholeAndAsFlatAsItIs()
    {
        var ring = Dense((-20, -10), (20, -10), (20, 10), (-20, 10), (-20, -10));

        var pieces = Clip().Ring(ring);

        var piece = Assert.Single(All(pieces));
        Assert.Equal(ring.Select(p => Placed(Globe.Forward(p.Lat, p.Lon))), piece);
        // python: area = 0.236359 (cartopy densifies the edges, so the two differ in the fourth place)
        Assert.Equal(0.236359, Area(piece), 3);
    }

    [Fact]
    public void ARingOnTheFarSide_IsShownNowhere()
    {
        // python: project_geometry(...) is empty
        Assert.Equal(0, Clip().Ring(Dense((120, -10), (160, -10), (160, 10), (120, 10), (120, -10))).Count);
    }

    [Fact]
    public void ARingThatCrossesTheHorizon_IsCutAtItAndClosedAlongIt()
    {
        var ring = Dense((30, -20), (150, -20), (150, 40), (30, 40), (30, -20));

        var pieces = Clip().Ring(ring);

        // python: one polygon, area 0.464294, counter-clockwise, its points on the globe up to its boundary polygon
        var piece = Assert.Single(All(pieces));
        Assert.Equal(0.464294, Area(piece), 3);
        Assert.True(Area(piece) > 0);
        AssertEveryPoint_IsOnTheGlobe(pieces);
        // between where the ring leaves and where it returns (about 60 degrees of the globe's edge) it runs along the outline's corners
        // (a corner every 6 degrees), from one cut on the outline to the other
        var onTheOutline = piece.Where(p => DistanceFromTheOutline(p) < 1e-9).ToList();
        Assert.InRange(onTheOutline.Count, 10, 16);
    }

    [Fact]
    public void ARingThatLeavesAndReturns_IsOnePolygonWhenItsVisiblePartsMeetAlongTheEdge()
    {
        var ring = Dense((60, -20), (110, -20), (110, -10), (75, -10), (75, 10), (110, 10), (110, 20), (60, 20), (60, -20));

        var pieces = Clip().Ring(ring);

        // python: one polygon, area 0.077763
        var piece = Assert.Single(All(pieces));
        Assert.Equal(0.077763, Area(piece), 3);
        AssertEveryPoint_IsOnTheGlobe(pieces);
    }

    [Fact]
    public void ARingWhoseVisiblePartsAreApart_IsOnePolygonForEachPart()
    {
        var ring = Dense((70, -20), (160, -20), (160, 20), (70, 20), (70, 10), (100, 10), (100, -10), (70, -10), (70, -20));

        var pieces = Clip().Ring(ring);

        // python: two polygons, area 0.019309 together
        Assert.Equal(2, pieces.Count);
        Assert.Equal(0.019309, AreaOf(pieces), 3);
        Assert.All(All(pieces), piece => Assert.True(Area(piece) > 0));
        AssertEveryPoint_IsOnTheGlobe(pieces);
    }

    [Fact]
    public void ALineThatLeavesTheGlobe_EndsOnItsEdge()
    {
        var line = Dense((0, 0), (60, 10), (120, 20), (170, 30));

        var pieces = Clip().Line(line);

        // python: one line, its last point at 0.99866 of the radius (on the boundary polygon, which is inside the circle)
        var piece = Assert.Single(All(pieces));
        Assert.Equal(Placed(Globe.Forward(0, 0)), piece[0]);
        Assert.Equal(0.99866, Radial(piece[^1]) / Radius, 3);
        Assert.True(DistanceFromTheOutline(piece[^1]) < 1e-9);
        AssertEveryPoint_IsOnTheGlobe(pieces);
    }

    [Fact]
    public void ALineThatLeavesAndComesBack_IsTwoLines_EachStartingOrEndingOnTheEdge()
    {
        // no position stands exactly on the horizon (a position at 90 degrees from the centre is shown, at a radius of 1)
        var line = Dense((60.5, 0), (120.5, 0), (120.5, 20), (60.5, 20));

        var pieces = Clip().Line(line);

        // python: two lines of 0.12963 and 0.12051 of the radius; the first ends and the second starts on the edge
        Assert.Equal(2, pieces.Count);
        Assert.Equal(Placed(Globe.Forward(0, 60.5)), pieces[0][0]);
        Assert.Equal(Placed(Globe.Forward(20, 60.5)), pieces[1][^1]);
        Assert.True(DistanceFromTheOutline(pieces[0][^1]) < 1e-9);
        Assert.True(DistanceFromTheOutline(pieces[1][0]) < 1e-9);
        Assert.Equal(0.12963, Length(pieces[0]) / Radius, 0.001);
        Assert.Equal(0.12051, Length(pieces[1]) / Radius, 0.001);
    }

    [Fact]
    public void ALineOnTheNearSide_IsOneLineAsItIs()
    {
        var line = Dense((-30, 0), (30, 20));

        var piece = Assert.Single(All(Clip().Line(line)));

        Assert.Equal(line.Select(p => Placed(Globe.Forward(p.Lat, p.Lon))), piece);
    }

    [Fact]
    public void APositionWithNoNumber_IsLeftOutOfTheRing_ItDoesNotCutIt()
    {
        var ring = Dense((-20, -10), (20, -10), (20, 10), (-20, 10), (-20, -10)).ToList();
        ring.Insert(10, new GeoPosition(double.NaN, 0));

        var piece = Assert.Single(All(Clip().Ring(ring)));

        Assert.Equal(ring.Count - 1, piece.Count);
    }

    [Fact]
    public void AnEdgeThatCrossesTheAntimeridian_IsCutWhereItReachesTheEdge_NotSweptAcrossTheGlobe()
    {
        // From 170 east to 170 west is 20 degrees along the equator, through 180, not 340 through 0. On a globe centred on 95 east
        // the horizon at the equator is at 185 (175 west): the edge is cut there, on the equator, at the globe's east edge.
        var globe = new Orthographic(0, 95);

        var piece = Assert.Single(All(GeoPixelMapping.HorizonClip.Of(globe, Plane)!.Line([new GeoPosition(170, 0), new GeoPosition(-170, 0)])));

        Assert.Equal(2, piece.Count);
        Assert.Equal(Radius, piece[1].X, 6);
        Assert.Equal(0, piece[1].Y, 6);
    }

    private static double Length(IReadOnlyList<Point> piece)
    {
        double length = 0;
        for (int i = 0; i + 1 < piece.Count; i++) length += double.Hypot(piece[i + 1].X - piece[i].X, piece[i + 1].Y - piece[i].Y);
        return length;
    }
}
