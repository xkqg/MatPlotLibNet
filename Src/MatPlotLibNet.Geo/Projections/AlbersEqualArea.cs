// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Albers Equal Area Conic projection — preserves area. Standard for US maps
/// (USGS uses standard parallels 29.5°N and 45.5°N).</summary>
public sealed class AlbersEqualArea : IGeoProjection, IProjectionParameters
{
    private readonly double _n, _c, _rho0, _lon0;
    private readonly ProjectionParameterSet _parameters;
    private readonly GeoBounds _bounds;

    /// <inheritdoc />
    public string Name => "AlbersEqualArea";

    /// <summary>Creates an Albers Equal Area Conic projection. The defaults are the USGS parameters for a map of the contiguous United States.</summary>
    /// <param name="sp1">First standard parallel in degrees. Default 29.5.</param>
    /// <param name="sp2">Second standard parallel in degrees. Default 45.5.</param>
    /// <param name="centerLon">Central meridian in degrees. Default -96.</param>
    /// <param name="centerLat">Latitude of origin in degrees. Default 37.5.</param>
    public AlbersEqualArea(double sp1 = 29.5, double sp2 = 45.5, double centerLon = -96, double centerLat = 37.5)
        : this(ProjectionParameterSet.Conic(sp1, sp2, centerLon, centerLat))
    {
    }

    private AlbersEqualArea(ProjectionParameterSet parameters)
    {
        _parameters = parameters;
        double phi1 = parameters.Lat1.ToRadians(), phi2 = parameters.Lat2.ToRadians(), phi0 = parameters.Lat0.ToRadians();
        _lon0 = parameters.Lon0.ToRadians();
        _n = (Math.Sin(phi1) + Math.Sin(phi2)) / 2;
        _c = Math.Cos(phi1) * Math.Cos(phi1) + 2 * _n * Math.Sin(phi1);
        _rho0 = Math.Sqrt(_c - 2 * _n * Math.Sin(phi0)) / _n;
        _bounds = this.BoxRound(ConeEdge(parameters.Lon0));
    }

    /// <summary>cartopy's boundary of an Albers map: 103 longitudes from the central meridian less 180 to it plus 180, 1e-10 in from the
    /// side the meridian is off zero (<c>_determine_longitude_bounds</c>), along the north pole and along the south pole.</summary>
    /// <param name="centralMeridian">The central meridian, in degrees.</param>
    private static IEnumerable<GeoCoordinate> ConeEdge(double centralMeridian)
    {
        double from = -180 + centralMeridian, to = 180 + centralMeridian;
        if (centralMeridian > 0) to -= 1e-10;
        else if (centralMeridian < 0) from += 1e-10;

        foreach (double longitude in new EqualWidthBins(from, to, 102).Edges())
        {
            yield return new(90, longitude);
            yield return new(-90, longitude);
        }
    }

    IReadOnlyDictionary<string, double> IProjectionParameters.Parameters => _parameters.Numbers;

    /// <inheritdoc />
    IGeoProjection IProjectionParameters.Made(IReadOnlyDictionary<string, double> parameters) => new AlbersEqualArea(_parameters.With(parameters));

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double phi = Math.Clamp(latitude, -89.99, 89.99).ToRadians();
        double lam = longitude.ToRadians();
        double rho = Math.Sqrt(_c - 2 * _n * Math.Sin(phi)) / _n;
        double theta = _n * (lam - _lon0);
        return new((rho * Math.Sin(theta)).ToDegrees(), (_rho0 - rho * Math.Cos(theta)).ToDegrees());
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;
    /// <inheritdoc />
    /// <remarks>The box round cartopy's boundary of the map, worked out once.</remarks>
    public GeoBounds Bounds => _bounds;
}
