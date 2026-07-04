// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Mercator projection — conformal cylindrical projection used by web maps.
/// Preserves angles and shapes locally but distorts area dramatically near poles.
/// Latitude is clamped to ±85° to avoid infinite Y values.</summary>
public sealed class Mercator : IGeoProjection
{
    private const double MaxLat = 85.051129;

    /// <inheritdoc />
    public string Name => "Mercator";

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double lat = Math.Clamp(latitude, -MaxLat, MaxLat);
        double x = longitude;
        double y = Math.Log(Math.Tan(Math.PI / 4 + lat.ToRadians() / 2)).ToDegrees();
        return new(x, y);
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y)
    {
        double lat = (2 * Math.Atan(Math.Exp(y.ToRadians())) - Math.PI / 2).ToDegrees();
        return Math.Abs(lat) <= MaxLat ? new GeoCoordinate(lat, x) : null;
    }

    /// <inheritdoc />
    public GeoBounds Bounds
    {
        get
        {
            var (_, yMax) = Forward(MaxLat, 0);
            return new(-180, 180, -yMax, yMax);
        }
    }
}
