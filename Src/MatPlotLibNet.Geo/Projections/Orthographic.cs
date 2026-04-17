// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Orthographic projection — globe view from infinite distance. Shows one hemisphere
/// at a time. Points on the far side of the globe return null from <see cref="Forward"/>.</summary>
public sealed class Orthographic : IGeoProjection
{
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    /// <summary>Center latitude of the projection in degrees.</summary>
    public double CenterLat { get; }

    /// <summary>Center longitude of the projection in degrees.</summary>
    public double CenterLon { get; }

    /// <summary>Creates an orthographic projection centered on the specified point.</summary>
    public Orthographic(double centerLat = 0, double centerLon = 0)
    {
        CenterLat = centerLat;
        CenterLon = centerLon;
    }

    /// <inheritdoc />
    public string Name => "Orthographic";

    /// <inheritdoc />
    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double lat = latitude * DegToRad;
        double lon = longitude * DegToRad;
        double lat0 = CenterLat * DegToRad;
        double lon0 = CenterLon * DegToRad;

        double cosC = Math.Sin(lat0) * Math.Sin(lat) + Math.Cos(lat0) * Math.Cos(lat) * Math.Cos(lon - lon0);
        // If cosC < 0, point is on the far side of the globe
        if (cosC < 0) return (double.NaN, double.NaN);

        double x = Math.Cos(lat) * Math.Sin(lon - lon0) * RadToDeg;
        double y = (Math.Cos(lat0) * Math.Sin(lat) - Math.Sin(lat0) * Math.Cos(lat) * Math.Cos(lon - lon0)) * RadToDeg;

        return (x, y);
    }

    /// <inheritdoc />
    public (double Lat, double Lon)? Inverse(double x, double y) => null;

    /// <inheritdoc />
    public (double XMin, double XMax, double YMin, double YMax) Bounds =>
        (-90, 90, -90, 90);
}
