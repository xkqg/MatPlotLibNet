// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The shortest path between two places: the ends exactly as given and the places between them evenly spread in angle,
/// and only the ends when there is no one shortest path (the same place twice, or two places opposite each other, which every
/// great circle through them joins by the same length).</summary>
public class GreatCircleTests
{
    [Fact]
    public void ThePath_HasOnePlaceMoreThanItHasSegments_AndTheEndsExactlyAsGiven()
    {
        var from = new GeoCoordinate(10, 20);
        var to = new GeoCoordinate(40, 80);

        var path = GreatCircle.Between(from, to, 8);

        Assert.Equal(9, path.Count);
        Assert.Equal(from, path[0]);
        Assert.Equal(to, path[^1]);
    }

    [Fact]
    public void AlongTheEquator_ThePlacesBetween_AreEvenlySpreadInLongitude()
    {
        // A quarter turn from 0 to 90 degrees east in three segments: 0, 30, 60 and 90.
        var path = GreatCircle.Between(new GeoCoordinate(0, 0), new GeoCoordinate(0, 90), 3);

        Assert.Equal(30, path[1].Longitude, 6);
        Assert.Equal(60, path[2].Longitude, 6);
        Assert.All(path, place => Assert.Equal(0, place.Latitude, 6));
    }

    [Fact]
    public void TheSamePlaceTwice_IsTheTwoEndsAlone()
    {
        var place = new GeoCoordinate(12, 34);

        Assert.Equal([place, place], GreatCircle.Between(place, place, 64));
    }

    [Fact]
    public void TwoPlacesOppositeEachOther_AreTheTwoEndsAlone()
    {
        var from = new GeoCoordinate(0, 0);
        var to = new GeoCoordinate(0, 180);

        Assert.Equal([from, to], GreatCircle.Between(from, to, 64));
    }
}
