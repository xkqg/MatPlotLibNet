// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Sinusoidal (Sanson-Flamsteed) projection — equal-area pseudo-cylindrical.
/// Simplest equal-area projection. Meridians are sinusoidal curves.</summary>
public sealed class Sinusoidal : IGeoProjection
{
    public string Name => "Sinusoidal";

    public ProjectedPoint Forward(double latitude, double longitude) =>
        new(longitude * Math.Cos(latitude.ToRadians()), latitude);

    public GeoCoordinate? Inverse(double x, double y)
    {
        if (Math.Abs(y) > 90) return null;
        double cosLat = Math.Cos(y.ToRadians());
        return Math.Abs(cosLat) < 1e-10 ? null : new GeoCoordinate(y, x / cosLat);
    }

    public GeoBounds Bounds => new(-180, 180, -90, 90);
}
