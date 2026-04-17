// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Transverse Mercator projection (spherical) — conformal cylindrical rotated 90°.
/// Foundation of UTM (Universal Transverse Mercator) grid system.</summary>
public sealed class TransverseMercator : IGeoProjection
{
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    private readonly double _lam0;

    public string Name => "TransverseMercator";

    public TransverseMercator(double centerLon = 0) => _lam0 = centerLon * DegToRad;

    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double phi = latitude * DegToRad, lam = longitude * DegToRad;
        double b = Math.Cos(phi) * Math.Sin(lam - _lam0);
        if (Math.Abs(b) >= 1) return (double.NaN, double.NaN);
        double x = 0.5 * Math.Log((1 + b) / (1 - b));
        double y = Math.Atan2(Math.Tan(phi), Math.Cos(lam - _lam0)) - _lam0;
        return (x * RadToDeg, y * RadToDeg);
    }

    public (double Lat, double Lon)? Inverse(double x, double y) => null;
    public (double XMin, double XMax, double YMin, double YMax) Bounds => (-180, 180, -90, 90);
}
