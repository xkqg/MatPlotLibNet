// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Albers Equal Area Conic projection — preserves area. Standard for US maps
/// (USGS uses standard parallels 29.5°N and 45.5°N).</summary>
public sealed class AlbersEqualArea : IGeoProjection
{
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    private readonly double _n, _c, _rho0, _lon0;

    public string Name => "AlbersEqualArea";

    public AlbersEqualArea(double sp1 = 29.5, double sp2 = 45.5, double centerLon = -96, double centerLat = 37.5)
    {
        double phi1 = sp1 * DegToRad, phi2 = sp2 * DegToRad, phi0 = centerLat * DegToRad;
        _lon0 = centerLon * DegToRad;
        _n = (Math.Sin(phi1) + Math.Sin(phi2)) / 2;
        _c = Math.Cos(phi1) * Math.Cos(phi1) + 2 * _n * Math.Sin(phi1);
        _rho0 = Math.Sqrt(_c - 2 * _n * Math.Sin(phi0)) / _n;
    }

    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double phi = Math.Clamp(latitude, -89.99, 89.99) * DegToRad;
        double lam = longitude * DegToRad;
        double rho = Math.Sqrt(_c - 2 * _n * Math.Sin(phi)) / _n;
        double theta = _n * (lam - _lon0);
        return (rho * Math.Sin(theta) * RadToDeg, (_rho0 - rho * Math.Cos(theta)) * RadToDeg);
    }

    public (double Lat, double Lon)? Inverse(double x, double y) => null;
    public (double XMin, double XMax, double YMin, double YMax) Bounds => (-200, 200, -200, 200);
}
