// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>A projection that shows one side of the globe, as an orthographic one does: <see cref="IGeoProjection.Forward"/> answers
/// <c>(NaN, NaN)</c> for a place on the other side, and the outline of the map (<see cref="ProjectionOutlineExtensions.Outline"/>) is its
/// horizon, the edge where the near side ends. What a map draws of a ring that crosses the horizon is cut there
/// (<see cref="Geo.Series.GeoPixelMapping.HorizonClip"/>), as cartopy cuts it; what this package adds to <see cref="IGeoProjection"/>, beside it.</summary>
internal interface IProjectionHorizon : IProjectionOutline
{
}
