// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.XY;

namespace MatPlotLibNet.Rendering.Downsampling;

/// <summary>
/// Stateless helper that slices a monotonic-X series to the visible viewport and optionally
/// downsamples the result via LTTB. Used by <see cref="SignalSeriesRenderer"/> and
/// <see cref="SignalXYSeriesRenderer"/> in place of the linear-scan <see cref="ViewportCuller"/>.
/// </summary>
public static class MonotonicViewportSlicer
{
    /// <summary>
    /// Returns the X/Y arrays for the points in <paramref name="src"/> that fall within
    /// [<paramref name="xMin"/>, <paramref name="xMax"/>], downsampled to
    /// <paramref name="maxPoints"/> via LTTB when the slice exceeds that limit.
    /// </summary>
    /// <typeparam name="T">Any <see cref="IMonotonicXY"/> implementor.</typeparam>
    public static XYData Slice<T>(T src, double xMin, double xMax, int? maxPoints)
        where T : IMonotonicXY
    {
        var range = src.IndexRangeFor(xMin, xMax);
        if (range.IsEmpty) return new([], []);

        int start = range.StartInclusive, end = range.EndExclusive;
        int len = range.Count;
        var x = new double[len];
        var y = new double[len];
        for (int i = 0; i < len; i++)
        {
            x[i] = src.XAt(start + i);
            y[i] = src.YAt(start + i);
        }

        if (maxPoints is null || len <= maxPoints.Value) return new(x, y);
        return new LttbDownsampler().Downsample(x, y, maxPoints.Value);
    }
}
