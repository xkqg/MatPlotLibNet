// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Interface for geographic map projections that transform latitude/longitude
/// coordinates to planar (x, y) coordinates and vice versa.</summary>
public interface IGeoProjection
{
    /// <summary>Display name of the projection (e.g. "Robinson", "Mercator").</summary>
    string Name { get; }

    /// <summary>Transforms geographic coordinates to planar coordinates.</summary>
    /// <param name="latitude">Latitude in degrees (-90 to 90).</param>
    /// <param name="longitude">Longitude in degrees (-180 to 180).</param>
    /// <returns>Projected (X, Y) coordinates.</returns>
    (double X, double Y) Forward(double latitude, double longitude);

    /// <summary>Transforms planar coordinates back to geographic coordinates.</summary>
    /// <param name="x">Projected X coordinate.</param>
    /// <param name="y">Projected Y coordinate.</param>
    /// <returns>Geographic (Latitude, Longitude) or null if the point is outside the projection domain.</returns>
    (double Lat, double Lon)? Inverse(double x, double y);

    /// <summary>Bounding box of the projection in projected coordinates.</summary>
    (double XMin, double XMax, double YMin, double YMax) Bounds { get; }
}
