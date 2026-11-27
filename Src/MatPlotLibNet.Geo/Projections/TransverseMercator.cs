// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Transverse Mercator projection (spherical) — conformal cylindrical rotated 90°.
/// Foundation of UTM (Universal Transverse Mercator) grid system.</summary>
public sealed class TransverseMercator : IGeoProjection, IProjectionParameters
{
    private readonly double _lam0;
    private readonly ProjectionParameterSet _parameters;

    /// <inheritdoc />
    public string Name => "TransverseMercator";

    /// <summary>Creates a Transverse Mercator projection around the given central meridian.</summary>
    /// <param name="centerLon">Central meridian in degrees, the line along which scale is true. Default 0.</param>
    public TransverseMercator(double centerLon = 0)
        : this(ProjectionParameterSet.Meridian(centerLon))
    {
    }

    private TransverseMercator(ProjectionParameterSet parameters)
    {
        _parameters = parameters;
        _lam0 = parameters.Lon0.ToRadians();
    }

    IReadOnlyDictionary<string, double> IProjectionParameters.Parameters => _parameters.Numbers;

    /// <inheritdoc />
    IGeoProjection IProjectionParameters.Made(IReadOnlyDictionary<string, double> parameters) => new TransverseMercator(_parameters.With(parameters));

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double phi = latitude.ToRadians(), lam = longitude.ToRadians();
        double b = Math.Cos(phi) * Math.Sin(lam - _lam0);
        if (Math.Abs(b) >= 1) return ProjectedPoint.OffDomain;
        double x = 0.5 * Math.Log((1 + b) / (1 - b));
        // The latitude of origin is the equator: Y is the arc from it, the central meridian only turns the sphere.
        double y = Math.Atan2(Math.Tan(phi), Math.Cos(lam - _lam0));
        return new(x.ToDegrees(), y.ToDegrees());
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;
    /// <inheritdoc />
    /// <remarks>Half a turn of arc to each side and a quarter turn up and down, as cartopy's limits of a transverse Mercator map are
    /// (<c>x_limits = (-pi a, pi a)</c>, <c>y_limits = (-pi / 2 b, pi / 2 b)</c>).</remarks>
    public GeoBounds Bounds => GeoBounds.Symmetric(Math.PI.ToDegrees(), (Math.PI / 2).ToDegrees());
}
