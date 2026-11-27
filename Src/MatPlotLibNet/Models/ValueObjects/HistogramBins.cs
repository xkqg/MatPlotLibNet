// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Computed histogram bin data.</summary>
/// <param name="Min">Minimum data value.</param>
/// <param name="BinWidth">Width of each bin.</param>
/// <param name="Counts">Count of values in each bin.</param>
public readonly record struct HistogramBins(double Min, double BinWidth, int[] Counts)
{
    /// <summary>The edge <paramref name="index"/> of the bins drawn: the first bin's low edge stepped by <paramref name="index"/> widths
    /// (numpy's linspace edge, the step times the index plus the low end). A bar and a step outline stand on these edges, so the right
    /// edge of one bin is the left edge of the next.</summary>
    /// <param name="index">The edge, from 0 (the low edge of the first bin) to the number of bins.</param>
    /// <returns>The edge.</returns>
    internal double EdgeAt(int index) => Min + index * BinWidth;
}
