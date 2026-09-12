// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>How crowded each point's neighbourhood is, for colouring a scatter by density.
/// <para>Counted on a grid rather than estimated with a kernel. A two-dimensional kernel estimate walks every
/// other point for every point, so it costs the square of the sample size — and a density-coloured scatter is
/// only worth drawing when there are enough points that the markers overlap, which is exactly where that cost
/// becomes unpayable. A grid costs one pass over the points and one lookup each.</para></summary>
internal static class PointDensity
{
    /// <summary>How many cells to a side when the caller does not choose. It aims at roughly ten points per
    /// cell: far fewer and every point sits alone in its own cell, so the colours say something about the
    /// sampling rather than about the shape; far more and the whole cloud comes out one colour.</summary>
    /// <param name="count">How many points there are.</param>
    /// <returns>A grid size between 4 and 128.</returns>
    internal static int BinsFor(int count) =>
        (int)Math.Clamp(Math.Sqrt(count / 10.0), 4, 128);

    /// <summary>How many points share each point's cell, one number per point, in the order they were given.
    /// A point is always counted in its own cell, so the smallest answer is 1.</summary>
    /// <param name="x">The X coordinates.</param>
    /// <param name="y">The Y coordinates, one per X.</param>
    /// <param name="bins">Cells to a side.</param>
    /// <returns>The count of points sharing each point's cell.</returns>
    /// <exception cref="ArgumentException">The two axes carry different numbers of points.</exception>
    internal static double[] PerPoint(double[] x, double[] y, int bins)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Length != y.Length)
        {
            throw new ArgumentException($"There are {x.Length} x values and {y.Length} y values.", nameof(y));
        }

        if (x.Length == 0)
        {
            return [];
        }

        int cells = Math.Max(1, bins);
        double xMin = x.Min(), xMax = x.Max(), yMin = y.Min(), yMax = y.Max();

        // A cloud with no width in one direction is a line, and every point on it still belongs somewhere: the
        // span becomes 1 so the whole line falls in the first column, rather than dividing by zero.
        double xSpan = xMax > xMin ? xMax - xMin : 1.0;
        double ySpan = yMax > yMin ? yMax - yMin : 1.0;

        var counts = new int[cells * cells];
        var cellOf = new int[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            int cx = Math.Clamp((int)((x[i] - xMin) / xSpan * cells), 0, cells - 1);
            int cy = Math.Clamp((int)((y[i] - yMin) / ySpan * cells), 0, cells - 1);
            int cell = (cy * cells) + cx;
            cellOf[i] = cell;
            counts[cell]++;
        }

        var density = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            density[i] = counts[cellOf[i]];
        }

        return density;
    }
}
