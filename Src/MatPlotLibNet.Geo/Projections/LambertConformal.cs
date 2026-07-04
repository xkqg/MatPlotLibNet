// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Lambert Conformal Conic projection — good for mid-latitude regions (US, Europe).
/// Preserves shapes locally. Two standard parallels control the cone.</summary>
public sealed class LambertConformal : IGeoProjection
{
    private readonly double _n, _f, _rho0, _lon0;

    /// <inheritdoc />
    public string Name => "LambertConformal";

    /// <summary>Creates a Lambert Conformal Conic projection.</summary>
    /// <param name="standardParallel1">First standard parallel in degrees. Default 33.</param>
    /// <param name="standardParallel2">Second standard parallel in degrees. Default 45.</param>
    /// <param name="centerLon">Central meridian in degrees. Default 0.</param>
    /// <param name="centerLat">Latitude of origin in degrees. Default 39 (US center).</param>
    public LambertConformal(double standardParallel1 = 33, double standardParallel2 = 45,
        double centerLon = 0, double centerLat = 39)
    {
        double phi1 = standardParallel1.ToRadians();
        double phi2 = standardParallel2.ToRadians();
        double phi0 = centerLat.ToRadians();
        _lon0 = centerLon.ToRadians();

        _n = Math.Log(Math.Cos(phi1) / Math.Cos(phi2)) /
             Math.Log(Math.Tan(Math.PI / 4 + phi2 / 2) / Math.Tan(Math.PI / 4 + phi1 / 2));
        _f = Math.Cos(phi1) * Math.Pow(Math.Tan(Math.PI / 4 + phi1 / 2), _n) / _n;
        _rho0 = _f / Math.Pow(Math.Tan(Math.PI / 4 + phi0 / 2), _n);
    }

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double lat = Math.Clamp(latitude, -89.99, 89.99).ToRadians();
        double lon = longitude.ToRadians();

        double rho = _f / Math.Pow(Math.Tan(Math.PI / 4 + lat / 2), _n);
        double theta = _n * (lon - _lon0);

        double x = rho * Math.Sin(theta);
        double y = _rho0 - rho * Math.Cos(theta);

        return new(x.ToDegrees(), y.ToDegrees()); // scale to degree-like units
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;

    /// <inheritdoc />
    public GeoBounds Bounds =>
        new(-200, 200, -200, 200); // approximate
}
