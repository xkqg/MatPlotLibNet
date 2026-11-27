// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Azimuthal Equidistant projection — preserves distances from center point.
/// Used for polar maps and aviation route planning.</summary>
public sealed class AzimuthalEquidistant : IGeoProjection, IProjectionParameters, IProjectionOutline
{
    private readonly ProjectionParameterSet _parameters;
    private readonly CentredFrame _centre;

    /// <inheritdoc />
    public string Name => "AzimuthalEquidistant";

    /// <summary>Creates an Azimuthal Equidistant projection centred on the given point. The defaults centre it on the North Pole.</summary>
    /// <param name="centerLat">Latitude of the centre point in degrees. Default 90.</param>
    /// <param name="centerLon">Longitude of the centre point in degrees. Default 0.</param>
    public AzimuthalEquidistant(double centerLat = 90, double centerLon = 0)
        : this(ProjectionParameterSet.Centred(centerLat, centerLon))
    {
    }

    private AzimuthalEquidistant(ProjectionParameterSet parameters)
    {
        _parameters = parameters;
        _centre = CentredFrame.At(parameters.Lat0, parameters.Lon0);
    }

    IReadOnlyDictionary<string, double> IProjectionParameters.Parameters => _parameters.Numbers;

    /// <inheritdoc />
    IGeoProjection IProjectionParameters.Made(IReadOnlyDictionary<string, double> parameters) => new AzimuthalEquidistant(_parameters.With(parameters));

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        var place = _centre.Of(latitude, longitude);
        double c = Math.Acos(Math.Clamp(place.CosDistance, -1, 1));
        if (c < 1e-10) return new(0, 0);
        double k = c / Math.Sin(c);
        return new(place.EastBy(k).ToDegrees(), place.NorthBy(k).ToDegrees());
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;
    /// <inheritdoc />
    /// <remarks>The square round the circle of the antipode, half a turn of arc from the centre, as cartopy's limits of an azimuthal
    /// equidistant map are (<c>a * pi</c>).</remarks>
    public GeoBounds Bounds => GeoBounds.Symmetric(Math.PI.ToDegrees(), Math.PI.ToDegrees());

    /// <inheritdoc />
    /// <remarks>The circle of the antipode, 61 points as cartopy draws it.</remarks>
    IReadOnlyList<GeoJson.GeoPosition> IProjectionOutline.TraceOutline() => Bounds.InscribedCircle(61);
}
