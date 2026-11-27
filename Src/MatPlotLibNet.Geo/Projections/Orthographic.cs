// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Orthographic projection — globe view from infinite distance. Shows one hemisphere
/// at a time. Points on the far side of the globe are signalled as <c>(NaN, NaN)</c> by
/// <see cref="Forward"/> (see <see cref="ProjectedPoint"/>); a map cuts what it draws at the globe's edge, as cartopy does.</summary>
public sealed class Orthographic : IGeoProjection, IProjectionParameters, IProjectionHorizon
{
    private readonly ProjectionParameterSet _parameters;

    /// <summary>The centre of the globe, worked out once: a map projects every place of its rings through <see cref="Forward"/>.</summary>
    private readonly CentredFrame _centre;

    /// <summary>Center latitude of the projection in degrees.</summary>
    public double CenterLat => _parameters.Lat0;

    /// <summary>Center longitude of the projection in degrees.</summary>
    public double CenterLon => _parameters.Lon0;

    /// <summary>Creates an orthographic projection centered on the specified point.</summary>
    /// <param name="centerLat">The latitude of the centre of the globe, in degrees.</param>
    /// <param name="centerLon">The longitude of the centre of the globe, in degrees.</param>
    public Orthographic(double centerLat = 0, double centerLon = 0)
        : this(ProjectionParameterSet.Centred(centerLat, centerLon))
    {
    }

    private Orthographic(ProjectionParameterSet parameters)
    {
        _parameters = parameters;
        _centre = CentredFrame.At(parameters.Lat0, parameters.Lon0);
    }

    /// <inheritdoc />
    public string Name => "Orthographic";

    IReadOnlyDictionary<string, double> IProjectionParameters.Parameters => _parameters.Numbers;

    /// <inheritdoc />
    IGeoProjection IProjectionParameters.Made(IReadOnlyDictionary<string, double> parameters) => new Orthographic(_parameters.With(parameters));

    /// <inheritdoc />
    /// <remarks>The globe seen from infinitely far: a place at the scale 1, and a place on the far side (the cosine of its angle from
    /// the centre below 0) off the map.</remarks>
    public ProjectedPoint Forward(double latitude, double longitude)
    {
        var place = _centre.Of(latitude, longitude);
        if (place.CosDistance < 0) return ProjectedPoint.OffDomain;
        return new(place.EastBy(1).ToDegrees(), place.NorthBy(1).ToDegrees());
    }

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) => null;

    /// <summary>The globe's radius in the projected plane: a sphere of radius 1, measured in degrees of arc.</summary>
    private static readonly double Radius = 1.0.ToDegrees();

    /// <inheritdoc />
    /// <remarks>The square round the globe, ± its radius (180/π), as cartopy's limits of an orthographic map are.</remarks>
    public GeoBounds Bounds => GeoBounds.Symmetric(Radius, Radius);

    /// <inheritdoc />
    /// <remarks>The globe's edge, a circle of 61 points as cartopy draws it.</remarks>
    IReadOnlyList<GeoJson.GeoPosition> IProjectionOutline.TraceOutline() => Bounds.InscribedCircle(61);
}
