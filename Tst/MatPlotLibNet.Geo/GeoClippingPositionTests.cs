// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A ring that crosses the antimeridian is cut where its edges cross it, read as positions (<see cref="GeoPosition"/>),
/// the shape a geometry keeps its rings in: each piece reaches ±180 and keeps every position of its side, and a ring round a pole is
/// closed along the pole.</summary>
public class GeoClippingPositionTests
{
    private static readonly GeoPosition[] AcrossThePacific =
        [new(170, 0), new(175, 0), new(-175, 0), new(-170, 0), new(-170, 5), new(-175, 5), new(175, 5), new(170, 5)];

    [Fact]
    public void ARingThatDoesNotCross_IsHandedBackAsItIs()
    {
        GeoPosition[] ring = [new(0, 0), new(10, 5), new(20, 10), new(0, 0)];

        Assert.Same(ring, Assert.Single(GeoClipping.SplitAtDateline(ring)));
    }

    [Fact]
    public void AnEmptyRing_IsHandedBackAsItIs()
    {
        GeoPosition[] ring = [];

        Assert.Same(ring, Assert.Single(GeoClipping.SplitAtDateline(ring)));
    }

    [Fact]
    public void ARingThatCrosses_IsOneRingForEachHemisphere()
    {
        var split = GeoClipping.SplitAtDateline(AcrossThePacific);

        Assert.Equal(2, split.Count);
        Assert.All(split[0], p => Assert.True(p.Lon < 0));
        Assert.All(split[1], p => Assert.True(p.Lon >= 0));
        Assert.Equal(new GeoPosition(-180, 0), split[0][0]);
        Assert.Equal(new GeoPosition(180, 0), split[1][^1]);
    }

    [Fact]
    public void ARingThatCrosses_IsCutWhereItsEdgesCrossTheAntimeridian()
    {
        // Two positions on each side: the ring used to come back whole, and drawn whole it is a band around the world.
        GeoPosition[] ring = [new(170, 0), new(-170, 0), new(-170, 10), new(170, 10)];

        var split = GeoClipping.SplitAtDateline(ring);

        Assert.Equal(2, split.Count);
        Assert.Equal([new GeoPosition(-180, 0), new GeoPosition(-170, 0), new GeoPosition(-170, 10), new GeoPosition(-180, 10)], split[0]);
        Assert.Equal([new GeoPosition(180, 10), new GeoPosition(170, 10), new GeoPosition(170, 0), new GeoPosition(180, 0)], split[1]);
    }

    [Fact]
    public void TheMeridianIsReached_AtTheLatitudeWhereTheEdgeCrossesIt()
    {
        // The edge from (175, 0) to (-165, 20) runs 20 degrees east; it reaches 180 a quarter of the way, at latitude 5.
        GeoPosition[] ring = [new(175, 0), new(-165, 20), new(-170, 30), new(170, 30)];

        var split = GeoClipping.SplitAtDateline(ring);

        Assert.Equal(new GeoPosition(-180, 5), split[0][0]);
        Assert.Equal(new GeoPosition(180, 5), split[1][^1]);
    }

    [Fact]
    public void ARingWithOnePositionOnASide_KeepsThatSide_AsAPieceThatReachesTheMeridian()
    {
        // Every position is drawn: a side with one position is a triangle against the meridian, not left out.
        GeoPosition[] ring = [new(170, 0), new(-170, 0), new(175, 5), new(179, 10)];

        var split = GeoClipping.SplitAtDateline(ring);

        Assert.Equal(2, split.Count);
        Assert.Contains(new GeoPosition(-170, 0), split[0]);
        Assert.All(split[0], p => Assert.True(p.Lon < 0));
        Assert.Equal(3, split[0].Count);
        Assert.Equal([new GeoPosition(175, 5), new GeoPosition(179, 10), new GeoPosition(170, 0)],
            split[1].Where(p => Math.Abs(p.Lon) != 180));
    }

    [Fact]
    public void ARingAroundAPole_IsOnePiece_ClosedAlongThePole()
    {
        // It crosses the antimeridian once: the piece runs from -180 to 180 and is closed over the pole it goes round.
        GeoPosition[] ring = [new(-120, 80), new(0, 80), new(120, 80)];

        var piece = Assert.Single(GeoClipping.SplitAtDateline(ring));

        Assert.Equal(
            [new GeoPosition(-180, 80), new GeoPosition(-120, 80), new GeoPosition(0, 80), new GeoPosition(120, 80), new GeoPosition(180, 80),
                new GeoPosition(180, 90), new GeoPosition(-180, 90)],
            piece);
    }

    [Fact]
    public void ARingAroundTheSouthPole_IsClosedAlongTheSouthPole()
    {
        GeoPosition[] ring = [new(120, -80), new(0, -80), new(-120, -80)];

        var piece = Assert.Single(GeoClipping.SplitAtDateline(ring));

        Assert.Equal([new GeoPosition(-180, -90), new GeoPosition(180, -90)], piece.Where(p => p.Lat == -90).OrderBy(p => p.Lon));
        Assert.All(piece, p => Assert.InRange(p.Lon, -180, 180));
    }

    [Fact]
    public void AClosedRing_GivesClosedPieces()
    {
        GeoPosition[] ring = [new(170, 0), new(-170, 0), new(-170, 10), new(170, 10), new(170, 0)];

        var split = GeoClipping.SplitAtDateline(ring);

        Assert.Equal(2, split.Count);
        Assert.All(split, piece => Assert.Equal(piece[0], piece[^1]));
        Assert.Equal([new GeoPosition(-180, 0), new GeoPosition(-170, 0), new GeoPosition(-170, 10), new GeoPosition(-180, 10), new GeoPosition(-180, 0)], split[0]);
    }
}
