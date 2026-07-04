// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Interface for geographic map projections that transform latitude/longitude
/// coordinates to planar (x, y) coordinates and vice versa.</summary>
public interface IGeoProjection
{
    /// <summary>Display name of the projection (e.g. "Robinson", "Mercator").</summary>
    string Name { get; }

    /// <summary>Transforms geographic coordinates to projected-plane coordinates.</summary>
    /// <param name="latitude">Latitude in degrees (-90 to 90).</param>
    /// <param name="longitude">Longitude in degrees (-180 to 180).</param>
    /// <returns>The projected point. Off-domain points are signalled as
    /// <c>(<see cref="double.NaN"/>, <see cref="double.NaN"/>)</c> — see <see cref="ProjectedPoint"/>.</returns>
    ProjectedPoint Forward(double latitude, double longitude);

    /// <summary>Transforms projected-plane coordinates back to geographic coordinates.</summary>
    /// <param name="x">Projected X coordinate.</param>
    /// <param name="y">Projected Y coordinate.</param>
    /// <returns>The geographic coordinate, or <see langword="null"/> if the point is outside
    /// the projection domain.</returns>
    GeoCoordinate? Inverse(double x, double y);

    /// <summary>Bounding box of the projection in projected-plane coordinates.</summary>
    GeoBounds Bounds { get; }
}
