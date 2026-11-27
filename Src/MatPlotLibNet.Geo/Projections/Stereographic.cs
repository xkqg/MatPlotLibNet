// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Stereographic projection — conformal azimuthal. Preserves angles and shapes locally.
/// Used for polar regions and small-area maps.</summary>
public sealed class Stereographic : IGeoProjection, IProjectionParameters, IProjectionOutline
{
    private readonly ProjectionParameterSet _parameters;
    private readonly CentredFrame _centre;

    /// <inheritdoc />
    public string Name => "Stereographic";

    /// <summary>Creates a Stereographic projection centred on the given point. The defaults centre it on the North Pole.</summary>
    /// <param name="centerLat">Latitude of the centre point in degrees. Default 90.</param>
    /// <param name="centerLon">Longitude of the centre point in degrees. Default 0.</param>
    public Stereographic(double centerLat = 90, double centerLon = 0)
        : this(ProjectionParameterSet.Centred(centerLat, centerLon))
    {
    }

    private Stereographic(ProjectionParameterSet parameters)
    {
        _parameters = parameters;
        _centre = CentredFrame.At(parameters.Lat0, parameters.Lon0);
    }

    IReadOnlyDictionary<string, double> IProjectionParameters.Parameters => _parameters.Numbers;

    /// <inheritdoc />
    IGeoProjection IProjectionParameters.Made(IReadOnlyDictionary<string, double> parameters) => new Stereographic(_parameters.With(parameters));

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        var place = _centre.Of(latitude, longitude);
        double k = 2.0 / place.CosDistanceAfter(1);
        if (double.IsInfinity(k)) return ProjectedPoint.OffDomain;
        return new(place.EastBy(k).ToDegrees(), place.NorthBy(k).ToDegrees());
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;
    /// <summary>How far the map reaches from its centre, in degrees of arc: cartopy's square of 5e7 m on the default globe
    /// (<c>x_axis_offset = 5e7 / WGS84_SEMIMAJOR_AXIS</c>), 5e7 / 6378137 radians of the semimajor axis.</summary>
    private static readonly double Reach = (5e7 / 6378137.0).ToDegrees();

    /// <inheritdoc />
    /// <remarks>The square cartopy's limits of a stereographic map are, wherever the map is centred.</remarks>
    public GeoBounds Bounds => GeoBounds.Symmetric(Reach, Reach);

    /// <inheritdoc />
    /// <remarks>The circle its bounds are the square of, 91 points as cartopy draws it.</remarks>
    IReadOnlyList<GeoJson.GeoPosition> IProjectionOutline.TraceOutline() => Bounds.InscribedCircle(91);
}
