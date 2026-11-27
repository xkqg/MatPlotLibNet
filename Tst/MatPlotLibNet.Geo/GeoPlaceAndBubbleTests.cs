// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Where a place stands on a map (the one rule for a place a projection cannot place) and how big a bubble is (the one
/// rule for the area of a bubble from its value).</summary>
public class GeoPlaceAndBubbleTests
{
    private const double NaN = double.NaN;

    /// <summary>A projection that answers the point it is given.</summary>
    private static TestGeoProjection Fixed(ProjectedPoint answer) => new(default, (_, _) => answer);

    [Fact]
    public void APlace_IsWhereTheProjectionPutsIt()
    {
        var projection = GeoProjection.Robinson;
        var place = new GeoCoordinate(10, 20);

        var at = projection.Place(place);

        Assert.Equal(projection.Forward(10, 20), at);
    }

    [Fact]
    public void APlaceOnTheFarSideOfAGlobe_HasNoPlace()
    {
        Assert.Null(GeoProjection.Orthographic.Place(new GeoCoordinate(0, 180)));
    }

    [Theory]
    [InlineData(NaN, 2.0)]
    [InlineData(1.0, NaN)]
    [InlineData(NaN, NaN)]
    public void APlaceWithAMissingCoordinate_HasNoPlace(double x, double y)
    {
        Assert.Null(Fixed(new ProjectedPoint(x, y)).Place(new GeoCoordinate(0, 0)));
    }

    [Fact]
    public void AnInfiniteCoordinate_IsStillAPlace_AsTheProjectionAnswered()
    {
        var at = Fixed(new ProjectedPoint(double.PositiveInfinity, 1)).Place(new GeoCoordinate(90, 0));

        Assert.Equal(new ProjectedPoint(double.PositiveInfinity, 1), at);
    }

    [Fact]
    public void TheLargestBubble_HasTheDefaultArea_AndTheOthersAreInProportion()
    {
        var bubbles = GeoAxesExtensions.BubblesAt(new[] { 1.0, 2.0, 4.0 }, [10.0, 20.0, 30.0], [1.0, 2.0, 3.0]);

        Assert.Equal(400.0, GeoAxesExtensions.LargestBubbleArea);
        Assert.Equal([100.0, 200.0, 400.0], bubbles.Areas);
        Assert.Equal([10.0, 20.0, 30.0], bubbles.X);
        Assert.Equal([1.0, 2.0, 3.0], bubbles.Y);
    }

    [Fact]
    public void AValueOfNoQuantity_HasNoBubble_AndTakesNothingFromTheOthers()
    {
        var bubbles = GeoAxesExtensions.BubblesAt(new[] { 3.0, 0.0, NaN, 6.0, -1.0, double.PositiveInfinity }, [0.0, 1.0, 2.0, 3.0, 4.0, 5.0], [5.0, 4.0, 3.0, 2.0, 1.0, 0.0]);

        Assert.Equal([0.0, 3.0], bubbles.X);
        Assert.Equal([5.0, 2.0], bubbles.Y);
        Assert.Equal([200.0, 400.0], bubbles.Areas);
    }

    [Fact]
    public void ValuesOfNoQuantityAtAll_AreNoBubbles()
    {
        var bubbles = GeoAxesExtensions.BubblesAt(new[] { 0.0, NaN }, [0.0, 1.0], [0.0, 1.0]);

        Assert.Empty(bubbles.X);
        Assert.Empty(bubbles.Y);
        Assert.Empty(bubbles.Areas);
    }
}
