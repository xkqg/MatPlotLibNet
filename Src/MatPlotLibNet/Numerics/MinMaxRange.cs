// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Inclusive numeric interval <c>[Min, Max]</c>. Returned by color-bar data-range
/// providers (<see cref="Models.Series.IColorBarDataProvider.GetColorBarRange"/>), axis
/// nice-bound expansion (<see cref="Rendering.TickLocators.AutoLocator.ExpandToNiceBounds"/>),
/// and the axis-break compressed-range computation.</summary>
/// <param name="Min">Lower bound (inclusive).</param>
/// <param name="Max">Upper bound (inclusive).</param>
public readonly record struct MinMaxRange(double Min, double Max);
