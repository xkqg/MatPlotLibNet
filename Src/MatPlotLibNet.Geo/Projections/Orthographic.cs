// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Orthographic projection — globe view from infinite distance. Shows one hemisphere
/// at a time. Points on the far side of the globe are signalled as <c>(NaN, NaN)</c> by
/// <see cref="Forward"/> (see <see cref="ProjectedPoint"/>).</summary>
public sealed class Orthographic : IGeoProjection
{
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
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double lat = latitude.ToRadians();
        double lon = longitude.ToRadians();
        double lat0 = CenterLat.ToRadians();
        double lon0 = CenterLon.ToRadians();

        double cosC = Math.Sin(lat0) * Math.Sin(lat) + Math.Cos(lat0) * Math.Cos(lat) * Math.Cos(lon - lon0);
        // If cosC < 0, point is on the far side of the globe
        if (cosC < 0) return new(double.NaN, double.NaN);

        double x = (Math.Cos(lat) * Math.Sin(lon - lon0)).ToDegrees();
        double y = (Math.Cos(lat0) * Math.Sin(lat) - Math.Sin(lat0) * Math.Cos(lat) * Math.Cos(lon - lon0)).ToDegrees();

        return new(x, y);
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;

    /// <inheritdoc />
    public GeoBounds Bounds => new(-90, 90, -90, 90);
}
