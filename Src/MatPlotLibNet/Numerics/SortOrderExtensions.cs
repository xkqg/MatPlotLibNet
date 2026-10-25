// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Orders of a sample, given as the indices to read it in, so a verb can reorder several parallel arrays by one
/// permutation: a Pareto chart its categories and counts, a dot plot its categories and every group's values.</summary>
internal static class SortOrderExtensions
{
    extension(IReadOnlyList<double> values)
    {
        /// <summary>The order that puts this list from the largest to the smallest. Stable: equal values keep
        /// the order they were given in, so a tie never reshuffles between runs or platforms. A missing value (NaN) comes
        /// last, behind every number.</summary>
        /// <returns>A permutation of <c>0 .. n - 1</c>: <c>values[order[0]]</c> is the largest.</returns>
        /// <exception cref="ArgumentNullException">This list is <see langword="null"/>.</exception>
        internal int[] DescendingOrder()
        {
            ArgumentNullException.ThrowIfNull(values);

            // OrderByDescending is a stable sort; Comparer<double>.Default ranks NaN below every number.
            return [.. Enumerable.Range(0, values.Count).OrderByDescending(i => values[i])];
        }
    }
}
