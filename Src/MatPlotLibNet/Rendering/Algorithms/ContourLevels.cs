// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Algorithms;

/// <summary>Which levels a contour draws. This is the one place that says: the levels a user names
/// (<c>LevelValues</c> of a contour or a filled contour), and the two even spacings a count of levels
/// (<c>Levels</c>) stands for. A line contour and a triangle contour draw their lines at the levels it hands them, and a
/// filled contour fills the bands between them.</summary>
internal static class ContourLevels
{
    extension(double[]? levelValues)
    {
        /// <summary>The levels a user named, as a contour draws them: ascending, each once, and only those that can be drawn
        /// (a level that is not a number, or infinite, has no place on any grid). Naming no level at all (<see langword="null"/>
        /// or an empty list) means the user left it to the count, which is <see langword="null"/> here; naming levels of which
        /// none can be drawn is an empty list, so the contour draws nothing instead of falling back to a count it was not
        /// asked for. A level outside the range of the grid is kept: it simply crosses no cell.</summary>
        /// <returns>A new array of the levels to draw, or <see langword="null"/> when none were named.</returns>
        internal double[]? ExplicitLevels() =>
            levelValues is null || levelValues.Length == 0
                ? null
                : [.. levelValues.Where(level => level.IsDrawable()).Distinct().Order()];
    }

    /// <summary>The levels of a line contour that names none: <paramref name="count"/> levels spread evenly strictly
    /// between the ends of the range (a line at the lowest value would find every cell above it).</summary>
    /// <param name="lo">The lowest value the grid holds.</param>
    /// <param name="hi">The highest value the grid holds.</param>
    /// <param name="count">How many levels to give.</param>
    /// <returns>The levels, ascending.</returns>
    internal static double[] Interior(double lo, double hi, int count)
    {
        var levels = new double[count];
        for (int i = 0; i < count; i++)
        {
            levels[i] = lo + (hi - lo) * (i + 1) / (count + 1);
        }

        return levels;
    }

    /// <summary>The levels of a filled contour or a triangle contour that names none: <paramref name="count"/> levels
    /// spread evenly from the lowest value to the highest, both ends included, so a filled contour has
    /// <c><paramref name="count"/> - 1</c> bands that together cover the range.</summary>
    /// <param name="lo">The lowest value of the range.</param>
    /// <param name="hi">The highest value of the range.</param>
    /// <param name="count">How many levels to give; at least 2.</param>
    /// <returns>The levels, ascending, the first <paramref name="lo"/>.</returns>
    internal static double[] Spanning(double lo, double hi, int count)
    {
        var levels = new double[count];
        for (int i = 0; i < count; i++)
        {
            levels[i] = lo + i * (hi - lo) / (count - 1);
        }

        return levels;
    }
}
