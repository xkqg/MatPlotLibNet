// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Mercator projection — conformal cylindrical projection used by web maps.
/// Preserves angles and shapes locally but distorts area dramatically near poles.
/// Latitude is clamped to ±85° to avoid infinite Y values.</summary>
public sealed class Mercator : IGeoProjection
{
    private const double MaxLat = 85.051129;
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    /// <inheritdoc />
    public string Name => "Mercator";

    /// <inheritdoc />
    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double lat = Math.Clamp(latitude, -MaxLat, MaxLat);
        double x = longitude;
        double y = RadToDeg * Math.Log(Math.Tan(Math.PI / 4 + lat * DegToRad / 2));
        return (x, y);
    }

    /// <inheritdoc />
    public (double Lat, double Lon)? Inverse(double x, double y)
    {
        double lat = RadToDeg * (2 * Math.Atan(Math.Exp(y * DegToRad)) - Math.PI / 2);
        return Math.Abs(lat) <= MaxLat ? (lat, x) : null;
    }

    /// <inheritdoc />
    public (double XMin, double XMax, double YMin, double YMax) Bounds
    {
        get
        {
            var (_, yMax) = Forward(MaxLat, 0);
            return (-180, 180, -yMax, yMax);
        }
    }
}
