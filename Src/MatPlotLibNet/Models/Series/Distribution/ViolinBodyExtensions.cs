// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Models.Series;

/// <summary>One height of a violin's outline.</summary>
/// <param name="Y">The value the outline stands at.</param>
/// <param name="HalfWidth">How far the body reaches to either side of its position at that value, in data units, before the
/// series' <see cref="ViolinSeries.Side"/> decides which sides are drawn.</param>
internal readonly record struct ViolinSlice(double Y, double HalfWidth);

/// <summary>The outline of a violin's body: the one place its width at each height is decided, read by the renderer that
/// draws the body and by the series that ranges over it.</summary>
internal static class ViolinBodyExtensions
{
    /// <summary>The number of intervals the sample's extent is cut into: the outline stands at one more height than this.</summary>
    internal const int Intervals = 20;

    /// <summary>The outline of the body of a violin, from the lowest value to the highest. The width at a height is the
    /// share of the sample within a twentieth of the extent above and below it, times <paramref name="widths"/>; a sample
    /// whose values are all equal is given an extent of 1.</summary>
    /// <param name="sorted">The observations that can be drawn, ascending; at least one.</param>
    /// <param name="widths">The series' <see cref="ViolinSeries.Widths"/>.</param>
    /// <returns>The outline at <see cref="Intervals"/> + 1 heights, lowest first.</returns>
    internal static ViolinSlice[] Outline(this double[] sorted, double widths)
    {
        double min = sorted[0], range = sorted[^1] - min;
        if (range == 0) range = 1;
        double bandwidth = range / Intervals;
        var outline = new ViolinSlice[Intervals + 1];
        for (int s = 0; s <= Intervals; s++)
        {
            double y = min + range * s / Intervals;
            int lo = sorted.BisectLeft(y - bandwidth), hi = sorted.BisectRight(y + bandwidth);
            outline[s] = new ViolinSlice(y, (hi - lo) / (double)sorted.Length * widths);
        }

        return outline;
    }
}
