// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet;

/// <summary>A staircase as the points of ONE line: bin <c>i</c> runs from edge <c>i</c> to edge <c>i + 1</c> at its value, so it
/// contributes its two ends at one height, and the next bin begins where this one ends, which is the riser between the two
/// heights. The one owner of that expansion: <see cref="XYAxesExtensions.Stairs"/> draws the outline and the fill from the very
/// points built here, so the two cannot differ and nothing is expanded twice.
/// <para>A bin whose value or either edge cannot be drawn (NaN or an infinity) is a gap: both its points are kept at the
/// edges it was given and made missing in height, so a line or a fill is parted there and never bridged, and a missing edge
/// masks the two bins that meet at it without leaving a stub of either. A missing value is never a zero.</para></summary>
/// <param name="X">The x coordinate of each point, two for every bin: its left and its right edge.</param>
/// <param name="Y">The y coordinate of each point, two for every bin: its value twice, or a missing value twice.</param>
internal readonly record struct StaircasePath(double[] X, double[] Y)
{
    /// <summary>The staircase of <paramref name="values"/> in the bins between <paramref name="edges"/>.
    /// <para>The names a refusal carries are those of the arguments as the caller wrote them
    /// (<see cref="CallerArgumentExpressionAttribute"/>), so a verb that passes its own parameters is refused in its own
    /// words.</para></summary>
    /// <param name="values">The height of each bin.</param>
    /// <param name="edges">The edges of the bins: one more than there are values, bin <c>i</c> lying between edge <c>i</c> and
    /// edge <c>i + 1</c>.</param>
    /// <param name="valuesName">The caller's expression for <paramref name="values"/>; filled in by the compiler.</param>
    /// <param name="edgesName">The caller's expression for <paramref name="edges"/>; filled in by the compiler.</param>
    /// <returns>The points of the staircase, two for each bin, in the order of the bins; empty for no bins.</returns>
    /// <exception cref="ArgumentNullException">An array is <see langword="null"/>; the exception names it.</exception>
    /// <exception cref="ArgumentException">There are not exactly as many edges as values plus one; the exception names the
    /// edges.</exception>
    internal static StaircasePath Of(double[] values, double[] edges,
        [CallerArgumentExpression(nameof(values))] string valuesName = "values",
        [CallerArgumentExpression(nameof(edges))] string edgesName = "edges")
    {
        ArgumentNullException.ThrowIfNull(values, valuesName);
        ArgumentNullException.ThrowIfNull(edges, edgesName);
        if (edges.Length != values.Length + 1)
        {
            throw new ArgumentException(
                $"{edgesName} has {edges.Length} values and {valuesName} has {values.Length}; a bin lies between two edges, so give one edge more than there are values.",
                edgesName);
        }

        double[] x = new double[2 * values.Length];
        double[] y = new double[2 * values.Length];
        for (int bin = 0; bin < values.Length; bin++)
        {
            double left = edges[bin], right = edges[bin + 1];
            double height = left.IsDrawable() && right.IsDrawable() && values[bin].IsDrawable() ? values[bin] : double.NaN;
            x[2 * bin] = left;
            x[2 * bin + 1] = right;
            y[2 * bin] = height;
            y[2 * bin + 1] = height;
        }

        return new StaircasePath(x, y);
    }
}
