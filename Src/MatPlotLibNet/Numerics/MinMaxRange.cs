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

/// <summary>Extension methods for computing color-bar data ranges from 2-D data arrays.</summary>
public static class MinMaxRangeExtensions
{
    extension(double[,] data)
    {
        /// <summary>Scans every element of this array and returns the inclusive
        /// <c>[min, max]</c> range of the values a colour scale can place: a missing value (NaN) and an
        /// infinity are left out, the range the renderer colours the cells over. Falls back to
        /// <c>[0, 1]</c> when all such values are equal, or there is none, so downstream normalizers
        /// never divide by zero.</summary>
        public MinMaxRange ScanColorBarRange() =>
            data.DrawableRange() is { } range && range.Lo < range.Hi ? new(range.Lo, range.Hi) : new(0, 1);
    }
}
