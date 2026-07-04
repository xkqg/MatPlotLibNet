// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Stereographic projection — conformal azimuthal. Preserves angles and shapes locally.
/// Used for polar regions and small-area maps.</summary>
public sealed class Stereographic : IGeoProjection
{
    private readonly double _phi0, _lam0, _cosPhi0, _sinPhi0;

    public string Name => "Stereographic";

    public Stereographic(double centerLat = 90, double centerLon = 0)
    {
        _phi0 = centerLat.ToRadians();
        _lam0 = centerLon.ToRadians();
        _cosPhi0 = Math.Cos(_phi0);
        _sinPhi0 = Math.Sin(_phi0);
    }

    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double phi = latitude.ToRadians(), lam = longitude.ToRadians();
        double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);
        double k = 2.0 / (1 + _sinPhi0 * sinPhi + _cosPhi0 * cosPhi * Math.Cos(lam - _lam0));
        if (double.IsInfinity(k)) return new(double.NaN, double.NaN);
        double x = k * cosPhi * Math.Sin(lam - _lam0);
        double y = k * (_cosPhi0 * sinPhi - _sinPhi0 * cosPhi * Math.Cos(lam - _lam0));
        return new(x.ToDegrees(), y.ToDegrees());
    }

    public GeoCoordinate? Inverse(double x, double y) => null;
    public GeoBounds Bounds => new(-360, 360, -360, 360);
}
