// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series.XY;

/// <summary>Half-open index range <c>[StartInclusive, EndExclusive)</c> returned by
/// <see cref="IMonotonicXY.IndexRangeFor"/>.</summary>
public readonly record struct IndexRange(int StartInclusive, int EndExclusive)
{
    /// <summary>Number of elements in the range.</summary>
    public int Count => EndExclusive - StartInclusive;

    /// <summary><see langword="true"/> when the range contains no elements.</summary>
    public bool IsEmpty => Count <= 0;
}
