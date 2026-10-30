// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Transverse Mercator projection (spherical) — conformal cylindrical rotated 90°.
/// Foundation of UTM (Universal Transverse Mercator) grid system.</summary>
public sealed class TransverseMercator : IGeoProjection, IProjectionParameters
{
    private readonly double _lam0;
    private readonly Dictionary<string, double> _parameters;

    /// <inheritdoc />
    public string Name => "TransverseMercator";

    /// <summary>Creates a Transverse Mercator projection around the given central meridian.</summary>
    /// <param name="centerLon">Central meridian in degrees, the line along which scale is true. Default 0.</param>
    public TransverseMercator(double centerLon = 0)
    {
        _parameters = new() { ["lon_0"] = centerLon };
        _lam0 = centerLon.ToRadians();
    }

    IReadOnlyDictionary<string, double> IProjectionParameters.Parameters => _parameters;

    /// <inheritdoc />
    IGeoProjection IProjectionParameters.Made(IReadOnlyDictionary<string, double> parameters) =>
        new TransverseMercator(parameters.GetValueOrDefault("lon_0", _parameters["lon_0"]));

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double phi = latitude.ToRadians(), lam = longitude.ToRadians();
        double b = Math.Cos(phi) * Math.Sin(lam - _lam0);
        if (Math.Abs(b) >= 1) return new(double.NaN, double.NaN);
        double x = 0.5 * Math.Log((1 + b) / (1 - b));
        double y = Math.Atan2(Math.Tan(phi), Math.Cos(lam - _lam0)) - _lam0;
        return new(x.ToDegrees(), y.ToDegrees());
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;
    /// <inheritdoc />
    public GeoBounds Bounds => new(-180, 180, -90, 90);
}
