// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Projections;

/// <summary>A projection whose map is not the rectangle of its bounds: what this package adds to <see cref="IGeoProjection"/>,
/// beside it. A projection that is not one has the rectangle of its bounds as its outline. The projection says only how its edge is
/// traced; the package traces it once for each projection and keeps it (<see cref="ProjectionOutlineExtensions.Outline"/>).</summary>
internal interface IProjectionOutline
{
    /// <summary>Traces the edge of the map in the projected plane, closed (the first point again at the end), as cartopy's
    /// <c>projection.boundary</c> is: X in <see cref="GeoPosition.Lon"/>, Y in <see cref="GeoPosition.Lat"/>.</summary>
    /// <returns>The edge.</returns>
    IReadOnlyList<GeoPosition> TraceOutline();
}
