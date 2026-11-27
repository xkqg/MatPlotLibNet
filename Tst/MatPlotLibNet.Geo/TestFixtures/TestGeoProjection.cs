// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A projection from outside the package, as a map test hands one to a map: the plane of longitude and latitude over
/// <paramref name="bounds"/> (or the point <paramref name="forward"/> gives), counting every place it was asked to put down. The package
/// knows nothing of its numbers. A test that also needs an outline or a horizon derives from it.</summary>
/// <param name="bounds">The projected extent.</param>
/// <param name="forward">Where a latitude and a longitude go; left out, to the longitude and the latitude.</param>
internal class TestGeoProjection(GeoBounds bounds, Func<double, double, ProjectedPoint>? forward = null) : IGeoProjection
{
    public string Name => "Test";
    public GeoBounds Bounds => bounds;

    /// <summary>How many times each place was put down.</summary>
    public Dictionary<GeoCoordinate, int> Asked { get; } = [];

    public ProjectedPoint Forward(double latitude, double longitude)
    {
        var place = new GeoCoordinate(latitude, longitude);
        Asked[place] = Asked.GetValueOrDefault(place) + 1;
        return forward?.Invoke(latitude, longitude) ?? new ProjectedPoint(longitude, latitude);
    }

    public GeoCoordinate? Inverse(double x, double y) => new GeoCoordinate(y, x);
}
