// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Equirectangular (Plate Carrée) projection — identity mapping where
/// longitude maps to X and latitude maps to Y. Simplest projection, no distortion
/// at the equator but severe stretching at poles.</summary>
public sealed class PlateCarree : IGeoProjection
{
    /// <inheritdoc />
    public string Name => "PlateCarree";

    /// <inheritdoc />
    public ProjectedPoint Forward(double latitude, double longitude) =>
        new(longitude, latitude);

    /// <inheritdoc />
    public GeoCoordinate? Inverse(double x, double y) =>
        Math.Abs(y) <= 90 && Math.Abs(x) <= 180 ? new GeoCoordinate(y, x) : null;

    /// <inheritdoc />
    public GeoBounds Bounds =>
        new(-180, 180, -90, 90);
}
