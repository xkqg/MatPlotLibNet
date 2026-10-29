// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo;

/// <summary>A quantity that moves from one place to another: a trade route, a migration, a flight. A map draws it as a line along
/// the great circle between the two places, as wide as the quantity.</summary>
/// <param name="From">Where it starts.</param>
/// <param name="To">Where it ends.</param>
/// <param name="Value">How much of it moves; a flow of nothing, or of no number, is not drawn.</param>
public readonly record struct GeoFlow(GeoCoordinate From, GeoCoordinate To, double Value);
