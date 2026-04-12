// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series.XY;

/// <summary>
/// Provides fast viewport-range lookup for series whose X values are monotonically ascending.
/// Both <see cref="SignalSeries"/> (uniform sample rate, O(1)) and
/// <see cref="SignalXYSeries"/> (arbitrary ascending X, O(log n)) implement this interface.
/// The renderer uses <see cref="IndexRangeFor"/> to slice only the visible portion of a large
/// dataset before downsampling, avoiding a full-array scan.
/// </summary>
public interface IMonotonicXY
{
    /// <summary>Total number of data points.</summary>
    int Length { get; }

    /// <summary>
    /// Returns the half-open index range <c>[StartInclusive, EndExclusive)</c> of all points
    /// whose X value falls within <paramref name="xMin"/>…<paramref name="xMax"/> (inclusive).
    /// Callers must tolerate a result that includes one guard point on each side of the viewport.
    /// </summary>
    /// <remarks>Implementations must include one guard point on each side of the visible range
    /// to prevent clipping artifacts at viewport edges.</remarks>
    IndexRange IndexRangeFor(double xMin, double xMax);

    /// <summary>Returns the X value at index <paramref name="i"/>.</summary>
    double XAt(int i);

    /// <summary>Returns the Y value at index <paramref name="i"/>.</summary>
    double YAt(int i);
}
