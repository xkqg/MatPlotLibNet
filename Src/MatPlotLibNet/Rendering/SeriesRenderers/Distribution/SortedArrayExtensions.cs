// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Percentile / bisection extensions on pre-sorted <c>double[]</c> arrays, used by
/// distribution-plot renderers (box / violin / ECDF).</summary>
internal static class SortedArrayExtensions
{
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

        /// <summary>Leftmost insertion index for <paramref name="value"/> in <paramref name="sorted"/>.</summary>
        internal int BisectLeft(double value)
        {
            int lo = 0, hi = sorted.Length;
            while (lo < hi) { int mid = (lo + hi) / 2; if (sorted[mid] < value) lo = mid + 1; else hi = mid; }
            return lo;
        }

        /// <summary>Rightmost insertion index for <paramref name="value"/> in <paramref name="sorted"/>.</summary>
        internal int BisectRight(double value)
        {
            int lo = 0, hi = sorted.Length;
            while (lo < hi) { int mid = (lo + hi) / 2; if (sorted[mid] <= value) lo = mid + 1; else hi = mid; }
            return lo;
        }
    }
}
