// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Geo.Series;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The cut at a horizon holds for any outline, not the circle of an orthographic globe: here a world that is a plane showing
/// the square from -10 to 10 (a place is its own longitude and latitude), which places a place up to 10.05 from the centre and none
/// beyond. The outline is a polygon a little inside the horizon, as the globe's is a polygon inside its circle, and a place between
/// the two is placed but outside the map.</summary>
public class HorizonClipOutlineTests
{
    /// <summary>A plane that shows a square.</summary>
    /// <param name="clockwise">Whether the outline is given clockwise, as the globe's is, or counter-clockwise.</param>
    private sealed class SquareWorld(bool clockwise) : TestGeoProjection(new GeoBounds(-10, 10, -10, 10), (latitude, longitude) =>
        Math.Abs(longitude) > 10.05 || Math.Abs(latitude) > 10.05 ? new(double.NaN, double.NaN) : new(longitude, latitude)), IProjectionHorizon
    {
        IReadOnlyList<GeoPosition> IProjectionOutline.TraceOutline() => clockwise
            ? [new(-10, -10), new(-10, 10), new(10, 10), new(10, -10), new(-10, -10)]
            : [new(-10, -10), new(10, -10), new(10, 10), new(-10, 10), new(-10, -10)];
    }

    private static GeoPixelMapping.HorizonClip Clip(bool clockwise = false) => GeoPixelMapping.HorizonClip.Of(new SquareWorld(clockwise), point => new Point(point.X, point.Y))!;

    private static List<List<Point>> All(RingPieces pieces) => [.. Enumerable.Range(0, pieces.Count).Select(i => pieces[i])];

    private static double Area(IReadOnlyList<Point> ring)
    {
        double twice = 0;
        for (int i = 0; i < ring.Count; i++)
        {
            var (p, q) = (ring[i], ring[(i + 1) % ring.Count]);
            twice += p.X * q.Y - q.X * p.Y;
        }

        return twice / 2;
    }

    /// <summary>A rectangle through the corners, counter-clockwise, a position every unit along each side, closed.</summary>
    private static GeoPosition[] Rectangle(double left, double bottom, double right, double top)
    {
        var ring = new List<GeoPosition>();
        for (double x = left; x < right; x++) ring.Add(new(x, bottom));
        for (double y = bottom; y < top; y++) ring.Add(new(right, y));
        for (double x = right; x > left; x--) ring.Add(new(x, top));
        for (double y = top; y > bottom; y--) ring.Add(new(left, y));
        ring.Add(ring[0]);
        return [.. ring];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARingAcrossTheEdgeOfASquare_IsClosedAlongTheSquare_WhicheverWayItsOutlineRuns(bool clockwise)
    {
        // 5.5 to 20.5 wide and 10 high; what the square shows is 5.5 to 10 across, 4.5 wide
        var pieces = Clip(clockwise).Ring(Rectangle(5.5, -5, 20.5, 5));

        var piece = Assert.Single(All(pieces));
        Assert.Equal(45, Area(piece), 0.5);
        Assert.True(Area(piece) > 0);
        // the cuts stand on the square, on its right side
        Assert.Contains(piece, p => Math.Abs(p.X - 10) < 1e-9 && p.Y < -4);
        Assert.Contains(piece, p => Math.Abs(p.X - 10) < 1e-9 && p.Y > 4);
    }

    [Fact]
    public void ARingThatCrossesTheCornerOfTheSquare_IsClosedAroundTheCorner()
    {
        var pieces = Clip().Ring(Rectangle(5.5, 5.5, 20.5, 20.5));

        var piece = Assert.Single(All(pieces));
        // a rectangle from 5.5 to 10 each way, and the corner of the square (10, 10) is a corner of the piece
        Assert.Equal(4.5 * 4.5, Area(piece), 0.5);
        Assert.Contains(new Point(10, 10), piece);
    }

    [Fact]
    public void ARingWithNoRepeatedFirstPosition_IsCutAsOneThatRepeatsIt()
    {
        var closed = Rectangle(5.5, -5, 20.5, 5);

        var open = Clip().Ring(closed[..^1]);

        Assert.Equal(45, Area(Assert.Single(All(open))), 0.5);
    }

    [Fact]
    public void AnOpenLineThatStartsBeyondTheEdge_StartsOnTheOutline()
    {
        var piece = Assert.Single(All(Clip().Line([new(20, 0), new(5, 0)])));

        Assert.Equal(2, piece.Count);
        Assert.Equal(10, piece[0].X, 6);
        Assert.Equal(0, piece[0].Y, 6);
        Assert.Equal(new Point(5, 0), piece[1]);
    }

    [Fact]
    public void ALineBeyondTheEdge_IsShownNowhere() =>
        Assert.Equal(0, Clip().Line([new(20, 0), new(30, 5)]).Count);

    [Fact]
    public void APlacedPositionBetweenTheEdgeAndWhereTheProjectionStops_IsOutsideTheMap()
    {
        // (10.02, 0) is placed (the world shows up to 10.05) but is outside the square of 10: the line ends where it reaches 10
        var ends = Assert.Single(All(Clip().Line([new(5, 0), new(10.02, 0)])));

        Assert.Equal(2, ends.Count);
        Assert.Equal(10, ends[1].X, 6);
        Assert.Equal(0, ends[1].Y, 6);
        // and one that goes out and comes back is two lines
        Assert.Equal(2, Clip().Line([new(5, 0), new(10.02, 0), new(5, 5)]).Count);
    }

    [Fact]
    public void APositionNearTheCornerBeyondTheCircleOfTheCorners_IsOutsideTheMap()
    {
        // (10.04, 10.04) is placed, 14.2 from the centre: further than any corner of the square (14.14)
        var pieces = Clip().Line([new(5, 5), new(10.04, 10.04), new(5, 6)]);

        Assert.Equal(2, pieces.Count);
    }

    [Fact]
    public void ARingThatDipsBetweenTheEdgeAndTheLimitOfTheProjection_LeavesAndReturnsAtOnePoint()
    {
        // two positions on one ray from the centre, the second a hair to the other side of the first: the ring leaves and returns at
        // the same point of the outline, which is no lap of the square
        GeoPosition[] ring =
        [
            new(5, -3), new(9.5, -3), new(9.5, -1), new(10.02, 1e-13), new(10.02, 0), new(9.5, 1), new(9.5, 3), new(5, 3), new(5, -3),
        ];

        var pieces = Clip().Ring(ring);

        var piece = Assert.Single(All(pieces));
        // the ring is a rectangle 4.5 wide and 6 high with a point of 0.5 out of its right side: its area is about 27.5
        Assert.InRange(Area(piece), 26, 28);
    }

    [Fact]
    public void ARingThatRunsTheOtherWay_AndDipsAtOnePoint_IsNotALapOfTheSquareEither()
    {
        // a hole: the same ring wound clockwise, which leaves the square going down the right side, where the first leaves going up
        GeoPosition[] ring =
        [
            new(5, -3), new(5, 3), new(9.5, 3), new(9.5, 1), new(10.02, 0), new(10.02, 1e-13), new(9.5, -1), new(9.5, -3), new(5, -3),
        ];

        var piece = Assert.Single(All(Clip().Ring(ring)));

        Assert.InRange(Math.Abs(Area(piece)), 26, 28);
        Assert.True(Area(piece) < 0);
    }

    [Fact]
    public void ARingThatCrossesItselfAtTheEdge_IsCutIntoPolygonsAndEndsTheWalk()
    {
        // leaves and returns twice, the second return before the first leaving: no walk along the outline finds each exit its own entry
        GeoPosition[] ring =
        [
            new(12, -6), new(8, -6), new(8, 2), new(12, 2), new(12, -2), new(6, -2), new(6, 6), new(12, 6), new(12, -6),
        ];

        var pieces = Clip().Ring(ring);

        Assert.Equal(2, pieces.Count);
    }

    [Fact]
    public void APositionWithNoNumber_IsLeftOutWhereverItStands()
    {
        GeoPosition[] ring = [new(5, -3), new(double.NaN, 0), new(20, -3), new(20, 3), new(5, 3), new(5, -3)];

        var piece = Assert.Single(All(Clip().Ring(ring)));

        Assert.True(Area(piece) > 0);
    }
}
