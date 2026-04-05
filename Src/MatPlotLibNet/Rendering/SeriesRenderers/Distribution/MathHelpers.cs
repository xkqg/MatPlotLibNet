// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal static class MathHelpers
{
    internal static double Percentile(double[] sorted, double p)
    {
        double idx = (sorted.Length - 1) * p / 100.0;
        int lower = (int)Math.Floor(idx), upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + (idx - lower) * (sorted[upper] - sorted[lower]);
    }

    internal static int BisectLeft(double[] sorted, double value)
    {
        int lo = 0, hi = sorted.Length;
        while (lo < hi) { int mid = (lo + hi) / 2; if (sorted[mid] < value) lo = mid + 1; else hi = mid; }
        return lo;
    }

    internal static int BisectRight(double[] sorted, double value)
    {
        int lo = 0, hi = sorted.Length;
        while (lo < hi) { int mid = (lo + hi) / 2; if (sorted[mid] <= value) lo = mid + 1; else hi = mid; }
        return lo;
    }
}
