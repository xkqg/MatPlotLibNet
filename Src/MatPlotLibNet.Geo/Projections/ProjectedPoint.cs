// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>A point in projected-plane space — the result of
/// <see cref="IGeoProjection.Forward(double, double)"/>. Coordinates are in the projection's
/// own planar units (aligned with <see cref="IGeoProjection.Bounds"/> and with clipping in
/// <see cref="GeoJson.GeoClipping"/>), not geographic degrees.</summary>
/// <param name="X">Projected X coordinate.</param>
/// <param name="Y">Projected Y coordinate.</param>
/// <remarks>Points outside the projection's domain (for example the far hemisphere of an
/// orthographic globe, or a Transverse Mercator singularity) are signalled as
/// <c>(<see cref="double.NaN"/>, <see cref="double.NaN"/>)</c>. Consumers detect off-domain
/// points with <see cref="double.IsNaN(double)"/> on <see cref="X"/> or <see cref="Y"/>
/// (matplotlib/cartopy convention: such points render as gaps, never throw).</remarks>
public readonly record struct ProjectedPoint(double X, double Y);
