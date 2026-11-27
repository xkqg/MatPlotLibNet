// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Lambert Conformal Conic projection — good for mid-latitude regions (US, Europe).
/// Preserves shapes locally. Two standard parallels control the cone.</summary>
public sealed class LambertConformal : IGeoProjection, IProjectionParameters
{
    private readonly double _n, _f, _rho0, _lon0;
    private readonly ProjectionParameterSet _parameters;
    private readonly GeoBounds _bounds;

    /// <summary>The latitude cartopy cuts a Lambert map at, its default <c>cutoff</c>.</summary>
    private const double Cutoff = -30;

    /// <inheritdoc />
    public string Name => "LambertConformal";

    /// <summary>Creates a Lambert Conformal Conic projection.</summary>
    /// <param name="standardParallel1">First standard parallel in degrees. Default 33.</param>
    /// <param name="standardParallel2">Second standard parallel in degrees. Default 45.</param>
    /// <param name="centerLon">Central meridian in degrees. Default 0.</param>
    /// <param name="centerLat">Latitude of origin in degrees. Default 39 (US center).</param>
    public LambertConformal(double standardParallel1 = 33, double standardParallel2 = 45,
        double centerLon = 0, double centerLat = 39)
        : this(ProjectionParameterSet.Conic(standardParallel1, standardParallel2, centerLon, centerLat))
    {
    }

    private LambertConformal(ProjectionParameterSet parameters)
    {
        _parameters = parameters;
        double phi1 = parameters.Lat1.ToRadians();
        double phi2 = parameters.Lat2.ToRadians();
        double phi0 = parameters.Lat0.ToRadians();
        _lon0 = parameters.Lon0.ToRadians();

        _n = Math.Log(Math.Cos(phi1) / Math.Cos(phi2)) /
             Math.Log(Math.Tan(Math.PI / 4 + phi2 / 2) / Math.Tan(Math.PI / 4 + phi1 / 2));
        _f = Math.Cos(phi1) * Math.Pow(Math.Tan(Math.PI / 4 + phi1 / 2), _n) / _n;
        _rho0 = _f / Math.Pow(Math.Tan(Math.PI / 4 + phi0 / 2), _n);
        _bounds = this.BoxRound(ConeEdge(parameters));
    }

    /// <summary>cartopy's boundary of a Lambert map: the pole the cone converges to (north when the standard parallel further from the
    /// equator is north of it) at longitude 0, then 91 longitudes along the cutoff latitude from the central meridian plus 180 less 0.001
    /// to it less 180 plus 0.001 (the other way round for a southern cone), and the pole again.</summary>
    /// <param name="parameters">The numbers of the projection.</param>
    private static IEnumerable<GeoCoordinate> ConeEdge(ProjectionParameterSet parameters)
    {
        double poliest = Math.Abs(parameters.Lat1) > Math.Abs(parameters.Lat2) ? parameters.Lat1 : parameters.Lat2;
        double pole = poliest > 0 ? 90 : -90, east = parameters.Lon0 + 180 - 0.001, west = parameters.Lon0 - 180 + 0.001;

        yield return new(pole, 0);
        foreach (double longitude in pole == 90 ? new EqualWidthBins(east, west, 90).Edges() : new EqualWidthBins(west, east, 90).Edges())
            yield return new(Cutoff, longitude);
    }

    IReadOnlyDictionary<string, double> IProjectionParameters.Parameters => _parameters.Numbers;

    /// <inheritdoc />
    IGeoProjection IProjectionParameters.Made(IReadOnlyDictionary<string, double> parameters) => new LambertConformal(_parameters.With(parameters));

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
    /// <remarks>The box round cartopy's boundary of the map, worked out once.</remarks>
    public GeoBounds Bounds => _bounds;
}
