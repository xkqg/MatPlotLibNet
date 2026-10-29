// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
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
    /// <summary>The number of heights the outline stands at, matplotlib's <c>violinplot</c> <c>points=100</c>: evenly from the
    /// lowest value to the highest.</summary>
    internal const int Heights = 100;

    extension(double[] sorted)
    {
        /// <summary>The outline of the body of a violin, from the lowest value to the highest: the Gaussian kernel density of the
        /// sample at <see cref="Heights"/> heights, with Silverman's bandwidth (<see cref="GaussianKde"/>, the estimator the
        /// density curve and the ridgeline are drawn with), scaled so the widest point of the body is <paramref name="widths"/>.
        /// A sample without a spread (one observation, or every value the same) has no density: its body is a spike, the whole
        /// width at its value at every height, as matplotlib draws it.</summary>
        /// <param name="widths">The series' <see cref="ViolinSeries.Widths"/>: the half-width of the body at its widest.</param>
        /// <returns>The outline at <see cref="Heights"/> heights, lowest first.</returns>
        internal ViolinSlice[] Outline(double widths)
        {
            double min = sorted[0], max = sorted[^1];
            if (min == max) return [.. Enumerable.Repeat(new ViolinSlice(min, widths), Heights)];

            double[] heights = [.. Enumerable.Range(0, Heights).Select(i => min + (max - min) * i / (Heights - 1))];
            heights[^1] = max;
            double[] density = GaussianKde.DensityAt(sorted, GaussianKde.SilvermanBandwidth(sorted), heights);
            double peak = density.Max();
            return [.. heights.Select((y, i) => new ViolinSlice(y, density[i] / peak * widths))];
        }
    }
}
