// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Sinusoidal (Sanson-Flamsteed) projection — equal-area pseudo-cylindrical.
/// Simplest equal-area projection. Meridians are sinusoidal curves.</summary>
public sealed class Sinusoidal : IGeoProjection
{
    private const double DegToRad = Math.PI / 180.0;

    public string Name => "Sinusoidal";

    public (double X, double Y) Forward(double latitude, double longitude) =>
        (longitude * Math.Cos(latitude * DegToRad), latitude);

    public (double Lat, double Lon)? Inverse(double x, double y)
    {
        if (Math.Abs(y) > 90) return null;
        double cosLat = Math.Cos(y * DegToRad);
        return cosLat == 0 ? null : (y, x / cosLat);
    }

    public (double XMin, double XMax, double YMin, double YMax) Bounds => (-180, 180, -90, 90);
}
