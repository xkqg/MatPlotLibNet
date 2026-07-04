// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>The bounding box of a projection in projected-plane space — the value of
/// <see cref="IGeoProjection.Bounds"/>. Edges are expressed in the same planar units as
/// <see cref="ProjectedPoint"/>.</summary>
/// <param name="XMin">Minimum projected X coordinate (left edge).</param>
/// <param name="XMax">Maximum projected X coordinate (right edge).</param>
/// <param name="YMin">Minimum projected Y coordinate (bottom edge).</param>
/// <param name="YMax">Maximum projected Y coordinate (top edge).</param>
public readonly record struct GeoBounds(double XMin, double XMax, double YMin, double YMax);
