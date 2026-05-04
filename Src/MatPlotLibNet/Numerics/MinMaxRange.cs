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
    /// <summary>Scans every element of <paramref name="data"/> and returns the inclusive
    /// <c>[min, max]</c> range. Falls back to <c>[0, 1]</c> when all values are equal so
    /// downstream normalizers never divide by zero.</summary>
    public static MinMaxRange ScanColorBarRange(this double[,] data)
    {
        double min = double.MaxValue, max = double.MinValue;
        foreach (double v in data)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
        return min < max ? new(min, max) : new(0, 1);
    }
}
