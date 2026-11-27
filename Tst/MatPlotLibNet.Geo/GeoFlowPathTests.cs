// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The line of a flow in the plane of a map: its great circle cut into pieces, each piece projected, with a gap where the
/// circle crosses the antimeridian or a position cannot be placed. A flow of no quantity, or with an end the projection cannot
/// place, has no line. The line is held to the walk it was worked out by before the flow owned it, to the bit.</summary>
public class GeoFlowPathTests
{
    private const double NaN = double.NaN;

    private readonly record struct Line(double[] X, double[] Y);

    /// <summary>The line of a flow the way the axes worked it out, a place at a time, projecting every place of the circle.</summary>
    private static Line ByTheLongWay(IGeoProjection projection, GeoFlow flow)
    {
        var x = new List<double>();
        var y = new List<double>();
        double? previousLon = null;
        foreach (var place in GeoFlow.Between(flow.From, flow.To, 64))
        {
            var at = projection.Forward(place.Latitude, place.Longitude);
            bool crosses = previousLon is { } before && Math.Abs(place.Longitude - before) > 180;
            if ((crosses || double.IsNaN(at.X)) && x.Count > 0 && !double.IsNaN(x[^1]))
            {
                x.Add(NaN);
                y.Add(NaN);
            }

            if (!double.IsNaN(at.X) && !double.IsNaN(at.Y))
            {
                x.Add(at.X);
                y.Add(at.Y);
            }

            previousLon = place.Longitude;
        }

        return new Line([.. x], [.. y]);
    }

    private static readonly GeoFlow[] AllFlows =
    [
        new(new GeoCoordinate(52, 5), new GeoCoordinate(-33, 151), 3),            // Amsterdam to Sydney
        new(new GeoCoordinate(35.7, 139.7), new GeoCoordinate(34.0, -118.2), 1),  // Tokyo to Los Angeles, across the antimeridian
        new(new GeoCoordinate(52.4, 4.9), new GeoCoordinate(47.6, -122.3), 1),    // Amsterdam to Seattle, bent far north
        new(new GeoCoordinate(10, 20), new GeoCoordinate(10, 20), 2),             // two places that coincide
        new(new GeoCoordinate(0, 0), new GeoCoordinate(0, 180), 2),               // two places opposite each other
        new(new GeoCoordinate(48.85, 2.35), new GeoCoordinate(50.85, 4.35), 5),   // a short hop
    ];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void TheLine_IsTheOneTheOldWalkMade_OnEveryProjection(int which)
    {
        var flow = AllFlows[which];
        foreach (var projection in new[] { GeoProjection.PlateCarree, GeoProjection.Robinson, GeoProjection.Mollweide, GeoProjection.Mercator, GeoProjection.OrthographicAt(50, 10) })
        {
            var path = flow.PathOn(projection);

            bool bothEndsPlaced = !double.IsNaN(projection.Forward(flow.From.Latitude, flow.From.Longitude).X)
                && !double.IsNaN(projection.Forward(flow.To.Latitude, flow.To.Longitude).X);
            if (!bothEndsPlaced)
            {
                Assert.Null(path);
                continue;
            }

            var expected = ByTheLongWay(projection, flow);
            Assert.NotNull(path);
            Assert.Equal(expected.X, path.Value.X);
            Assert.Equal(expected.Y, path.Value.Y);
            Assert.Equal(flow.Value, path.Value.Value);
        }
    }

    /// <summary>The line holds the one form a line in runs is written in: a gap is never first, never last, never next to another gap
    /// (GapSeparatorFormTests in the core suite holds the same of every other writer).</summary>
    [Fact]
    public void TheLine_HoldsTheOneFormOfGap_OnEveryProjection()
    {
        foreach (var projection in new[] { GeoProjection.PlateCarree, GeoProjection.Robinson, GeoProjection.Mollweide, GeoProjection.Mercator, GeoProjection.OrthographicAt(50, 10) })
        {
            foreach (var flow in AllFlows)
            {
                if (flow.PathOn(projection) is not { } path)
                {
                    continue;
                }

                path.X.AssertGapsStandBetweenValues(projection.GetType().Name);
            }
        }
    }

    [Fact]
    public void ALineAcrossTheAntimeridian_HasAGapWhereItLeavesOneEdgeOfTheMap()
    {
        var flow = new GeoFlow(new GeoCoordinate(35.7, 139.7), new GeoCoordinate(34.0, -118.2), 1);

        var path = flow.PathOn(GeoProjection.PlateCarree)!.Value;

        Assert.Single(path.X, double.IsNaN);
        Assert.Equal(path.X.Length, path.Y.Length);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AFlowOfNoQuantity_HasNoLine(double quantity)
    {
        var flow = new GeoFlow(new GeoCoordinate(52, 5), new GeoCoordinate(-33, 151), quantity);

        Assert.Null(flow.PathOn(GeoProjection.PlateCarree));
    }

    [Fact]
    public void AFlowWithAnEndTheProjectionCannotPlace_HasNoLine()
    {
        var farSide = new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(0, 180), 1);

        Assert.Null(farSide.PathOn(GeoProjection.Orthographic));
        Assert.Null(new GeoFlow(new GeoCoordinate(0, 180), new GeoCoordinate(0, 0), 1).PathOn(GeoProjection.Orthographic));
    }

    [Fact]
    public void EachEnd_IsProjectedOnce()
    {
        var flow = new GeoFlow(new GeoCoordinate(52, 5), new GeoCoordinate(-33, 151), 3);
        var projection = new TestGeoProjection(GeoProjection.PlateCarree.Bounds);

        Assert.NotNull(flow.PathOn(projection));

        Assert.Equal(1, projection.Asked[flow.From]);
        Assert.Equal(1, projection.Asked[flow.To]);
        // The 63 places between the ends are asked once each: 65 places in all, the ends among them.
        Assert.Equal(65, projection.Asked.Count);
        Assert.All(projection.Asked.Values, times => Assert.Equal(1, times));
    }
}
