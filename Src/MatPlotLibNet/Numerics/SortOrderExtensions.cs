// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Orders of a sample, given as the indices to read it in, and the reading of an array in such an order, so a verb
/// can reorder several parallel arrays by one permutation: a Pareto chart its categories and counts, a dot plot its
/// categories and every group's values, a treemap its children; and the percentile of a sample in ascending order, the one
/// interpolation a box, a violin and <see cref="Vec.Percentile"/> read.</summary>
internal static class SortOrderExtensions
{
    extension(double[] values)
    {
        /// <summary>The order that puts these values from the largest to the smallest. Stable: equal values keep
        /// the order they were given in, so a tie never reshuffles between runs or platforms. A missing value (NaN) comes
        /// last, behind every number.</summary>
        /// <returns>A permutation of <c>0 .. n - 1</c>: <c>values[order[0]]</c> is the largest.</returns>
        /// <exception cref="ArgumentNullException">The values are <see langword="null"/>.</exception>
        internal int[] DescendingOrder()
        {
            ArgumentNullException.ThrowIfNull(values);

            var order = new int[values.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            order.AsSpan().Sort(new LargestFirst(values));
            return order;
        }
    }

    extension(int[] order)
    {
        /// <summary>The values read in this order: entry <c>k</c> is <c>values[order[k]]</c>, so every array parallel to the
        /// sample the order was made from (its names, the values of another group) is put in the same order.</summary>
        /// <typeparam name="T">The kind of value.</typeparam>
        /// <param name="values">The values, one per index the order holds.</param>
        /// <returns>A new array as long as the order.</returns>
        internal T[] Pick<T>(IReadOnlyList<T> values)
        {
            var picked = new T[order.Length];
            for (int k = 0; k < picked.Length; k++) picked[k] = values[order[k]];
            return picked;
        }
    }

    extension(double[] sorted)
    {
        /// <summary>Returns the <paramref name="p"/>-th percentile of a pre-sorted array via linear interpolation.</summary>
        /// <param name="p">Percentile in [0, 100].</param>
        internal double Percentile(double p)
        {
            double idx = (sorted.Length - 1) * p / 100.0;
            int lower = (int)Math.Floor(idx), upper = Math.Min(lower + 1, sorted.Length - 1);
            return sorted[lower] + (idx - lower) * (sorted[upper] - sorted[lower]);
        }
    }

    /// <summary>Ranks two indices of a sample by their values, the larger first, and two equal values by their index, so the
    /// order is the one a stable sort gives, whatever the sort does with ties. <see cref="double.CompareTo(double)"/> ranks
    /// NaN below every number, so a missing value comes last. A struct, so the sort calls it without a delegate or an
    /// allocation.</summary>
    /// <param name="values">The sample the indices read.</param>
    private readonly struct LargestFirst(double[] values) : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            int byValue = values[b].CompareTo(values[a]);
            return byValue != 0 ? byValue : a.CompareTo(b);
        }
    }
}
