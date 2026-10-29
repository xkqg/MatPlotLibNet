// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Percentile extension on pre-sorted <c>double[]</c> arrays, used by the box, violin and point-plot renderers.</summary>
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
    }
}
