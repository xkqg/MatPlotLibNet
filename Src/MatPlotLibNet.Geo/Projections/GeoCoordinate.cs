// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>A geographic coordinate in degrees — the result of
/// <see cref="IGeoProjection.Inverse(double, double)"/>. This is geographic space
/// (latitude/longitude), distinct from the projected-plane <see cref="ProjectedPoint"/>
/// produced by <see cref="IGeoProjection.Forward(double, double)"/>.</summary>
/// <param name="Latitude">Latitude in degrees (-90 to 90).</param>
/// <param name="Longitude">Longitude in degrees (-180 to 180).</param>
public readonly record struct GeoCoordinate(double Latitude, double Longitude);
