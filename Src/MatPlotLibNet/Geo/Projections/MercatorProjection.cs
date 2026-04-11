// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Web Mercator projection (EPSG:3857) mapping longitude/latitude to normalized screen coordinates.
/// Latitude is clamped to ±85.0511° to avoid infinity at the poles.</summary>
public sealed class MercatorProjection : IMapProjection
{
    // π (radians)
    private const double Pi = Math.PI;
    // Maximum safe Mercator latitude in degrees
    private const double MaxLat = 85.0511;

    private readonly double _centerLon;
    private readonly double _lonExtent;

    // Pre-computed Mercator y bounds for the clamped latitude extent
    private readonly double _yMin;
    private readonly double _yMax;

    /// <summary>Initializes a new Mercator projection.</summary>
    /// <param name="centerLon">Center meridian in degrees (default 0).</param>
    /// <param name="lonExtent">Total longitude span in degrees (default 360).</param>
    public MercatorProjection(double centerLon = 0, double lonExtent = 360)
    {
        _centerLon = centerLon;
        _lonExtent = lonExtent;
        _yMin = MercatorY(MaxLat);
        _yMax = MercatorY(-MaxLat);
    }

    /// <inheritdoc />
    public (double Nx, double Ny) Project(double lon, double lat)
    {
        double nx = (lon - _centerLon + _lonExtent / 2.0) / _lonExtent;
        double clampedLat = Math.Clamp(lat, -MaxLat, MaxLat);
        double y = MercatorY(clampedLat);
        double ny = 1.0 - (y - _yMax) / (_yMin - _yMax);
        return (nx, ny);
    }

    /// <inheritdoc />
    public (double LonMin, double LonMax, double LatMin, double LatMax) Bounds =>
        (_centerLon - _lonExtent / 2.0, _centerLon + _lonExtent / 2.0, -MaxLat, MaxLat);

    private static double MercatorY(double latDeg)
    {
        double latRad = latDeg * Pi / 180.0;
        return Math.Log(Math.Tan(Pi / 4.0 + latRad / 2.0));
    }
}
