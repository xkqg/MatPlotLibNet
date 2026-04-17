// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Azimuthal Equidistant projection — preserves distances from center point.
/// Used for polar maps and aviation route planning.</summary>
public sealed class AzimuthalEquidistant : IGeoProjection
{
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    private readonly double _phi0, _lam0, _cosPhi0, _sinPhi0;

    public string Name => "AzimuthalEquidistant";

    public AzimuthalEquidistant(double centerLat = 90, double centerLon = 0)
    {
        _phi0 = centerLat * DegToRad;
        _lam0 = centerLon * DegToRad;
        _cosPhi0 = Math.Cos(_phi0);
        _sinPhi0 = Math.Sin(_phi0);
    }

    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double phi = latitude * DegToRad, lam = longitude * DegToRad;
        double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);
        double cosC = _sinPhi0 * sinPhi + _cosPhi0 * cosPhi * Math.Cos(lam - _lam0);
        double c = Math.Acos(Math.Clamp(cosC, -1, 1));
        if (c < 1e-10) return (0, 0);
        double k = c / Math.Sin(c);
        double x = k * cosPhi * Math.Sin(lam - _lam0);
        double y = k * (_cosPhi0 * sinPhi - _sinPhi0 * cosPhi * Math.Cos(lam - _lam0));
        return (x * RadToDeg, y * RadToDeg);
    }

    public (double Lat, double Lon)? Inverse(double x, double y) => null;
    public (double XMin, double XMax, double YMin, double YMax) Bounds => (-180, 180, -180, 180);
}
