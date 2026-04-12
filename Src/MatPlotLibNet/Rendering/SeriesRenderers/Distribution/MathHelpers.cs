// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Lightweight statistical helper methods used by distribution-plot renderers.</summary>
internal static class MathHelpers
{
    /// <summary>Returns the <paramref name="p"/>-th percentile of a pre-sorted array using linear interpolation.</summary>
    /// <param name="sorted">Array of values sorted in ascending order.</param>
    /// <param name="p">Percentile to compute, in the range [0, 100].</param>
    internal static double Percentile(double[] sorted, double p)
    {
        double idx = (sorted.Length - 1) * p / 100.0;
        int lower = (int)Math.Floor(idx), upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + (idx - lower) * (sorted[upper] - sorted[lower]);
    }

    /// <summary>Returns the leftmost index where <paramref name="value"/> can be inserted into <paramref name="sorted"/> while maintaining order.</summary>
    /// <param name="sorted">A sorted array of doubles.</param>
    /// <param name="value">The value to locate.</param>
    internal static int BisectLeft(double[] sorted, double value)
    {
        int lo = 0, hi = sorted.Length;
        while (lo < hi) { int mid = (lo + hi) / 2; if (sorted[mid] < value) lo = mid + 1; else hi = mid; }
        return lo;
    }

    /// <summary>Returns the rightmost index where <paramref name="value"/> can be inserted into <paramref name="sorted"/> while maintaining order.</summary>
    /// <param name="sorted">A sorted array of doubles.</param>
    /// <param name="value">The value to locate.</param>
    internal static int BisectRight(double[] sorted, double value)
    {
        int lo = 0, hi = sorted.Length;
        while (lo < hi) { int mid = (lo + hi) / 2; if (sorted[mid] <= value) lo = mid + 1; else hi = mid; }
        return lo;
    }
}
