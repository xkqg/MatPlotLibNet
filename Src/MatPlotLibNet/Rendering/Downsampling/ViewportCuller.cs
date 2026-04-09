// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Downsampling;

/// <summary>
/// Filters a dataset to points relevant to a given X viewport, keeping one point before and after
/// the visible range so that line segments correctly clip at the axis boundary.
/// </summary>
public static class ViewportCuller
{
    /// <summary>
    /// Returns points with X in [<paramref name="xMin"/>, <paramref name="xMax"/>], plus the
    /// immediately adjacent out-of-range points on each side (for correct line clipping).
    /// </summary>
    /// <param name="x">The X coordinate array (must be sorted ascending).</param>
    /// <param name="y">The Y coordinate array, parallel to <paramref name="x"/>.</param>
    /// <param name="xMin">The minimum visible X value (inclusive).</param>
    /// <param name="xMax">The maximum visible X value (inclusive).</param>
    /// <returns>An <see cref="XYData"/> containing the culled X and Y values.</returns>
    public static XYData Cull(double[] x, double[] y, double xMin, double xMax)
    {
        int n = x.Length;
        if (n == 0) return new([], []);

        // Find first index inside range
        int first = Array.FindIndex(x, v => v >= xMin);
        // Find last index inside range
        int last = Array.FindLastIndex(x, v => v <= xMax);

        if (first < 0 || last < 0 || first > last)
            return new([], []);

        // Include one point before (for line clipping)
        int start = first > 0 ? first - 1 : first;
        // Include one point after (for line clipping)
        int end = last < n - 1 ? last + 1 : last;

        int len = end - start + 1;
        double[] outX = new double[len];
        double[] outY = new double[len];
        Array.Copy(x, start, outX, 0, len);
        Array.Copy(y, start, outY, 0, len);
        return new(outX, outY);
    }
}
