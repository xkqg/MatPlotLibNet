// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Numerics;

/// <summary>The extent a hexagonal grid covers: the range of the points that can be placed, in data units, as matplotlib's
/// <c>hexbin</c> takes it when it is given no <c>extent</c>. A range of one value is widened as matplotlib widens it
/// (<c>transforms._nonsingular(v, v, expander=0.1)</c>): by a tenth of the value to each side, or to -0.1 .. 0.1 about zero.</summary>
/// <param name="XMin">The lowest X.</param>
/// <param name="XMax">The highest X.</param>
/// <param name="YMin">The lowest Y.</param>
/// <param name="YMax">The highest Y.</param>
internal readonly record struct HexExtent(double XMin, double XMax, double YMin, double YMax)
{
    /// <summary>The extent of the points that can be placed, or <see langword="null"/> when none can.</summary>
    /// <param name="x">The X of each point.</param>
    /// <param name="y">The Y of each point.</param>
    internal static HexExtent? Of(double[] x, double[] y)
    {
        double xMin = double.PositiveInfinity, xMax = double.NegativeInfinity;
        double yMin = double.PositiveInfinity, yMax = double.NegativeInfinity;
        int n = Math.Min(x.Length, y.Length);
        for (int i = 0; i < n; i++)
        {
            if (!new Point(x[i], y[i]).IsDrawable()) continue;
            xMin = Math.Min(xMin, x[i]);
            xMax = Math.Max(xMax, x[i]);
            yMin = Math.Min(yMin, y[i]);
            yMax = Math.Max(yMax, y[i]);
        }

        if (xMin > xMax) return null;
        return new HexExtent(LowEnd(xMin, xMax), HighEnd(xMin, xMax), LowEnd(yMin, yMax), HighEnd(yMin, yMax));
    }

    private static double LowEnd(double lo, double hi) => lo != hi ? lo : lo == 0 ? -0.1 : lo - 0.1 * Math.Abs(lo);

    private static double HighEnd(double lo, double hi) => lo != hi ? hi : hi == 0 ? 0.1 : hi + 0.1 * Math.Abs(hi);
}

/// <summary>The hexagonal grid of <c>hexbin</c>, which is matplotlib's: <c>nx</c> hexagons across the extent and <c>ny = int(nx /
/// sqrt(3))</c> rows, as two interleaved lattices of cells, <c>(nx + 1) * (ny + 1)</c> on the grid points
/// (<c>x = xmin + i * sx</c>, <c>y = ymin + j * sy</c>) and <c>nx * ny</c> between them (half a step over in both), each point
/// counted in the nearer of the two cells it lies between by the metric <c>dx^2 + 3 dy^2</c>, in the units of the steps. A hexagon
/// has a vertex up and down and a vertical edge on either side, <c>sx</c> wide and two thirds of <c>sy</c> tall. matplotlib 3.11.2,
/// <c>Axes.hexbin</c>.</summary>
internal readonly struct HexLattice
{
    private const double Sqrt3 = 1.7320508075688772; // Math.Sqrt(3)

    private readonly double _xMin;
    private readonly double _yMin;
    private readonly double _sx;
    private readonly double _sy;

    private HexLattice(double xMin, double yMin, double sx, double sy, int columns, int rows)
    {
        _xMin = xMin;
        _yMin = yMin;
        _sx = sx;
        _sy = sy;
        Columns = columns;
        Rows = rows;
    }

    /// <summary>The most columns a grid has: past it a hexagon is narrower than a hundredth of a pixel of a 20,000-pixel chart, and
    /// the cells, counted in an array, would run past what an array holds.</summary>
    internal const int MostColumns = 2048;

    /// <summary>The number of hexagons across the extent (<c>nx</c>): the grid size, at least 1 and at most <see cref="MostColumns"/>.</summary>
    internal int Columns { get; }

    /// <summary>The number of rows (<c>ny</c>): <c>int(nx / sqrt(3))</c>, at least 1.</summary>
    internal int Rows { get; }

    /// <summary>The number of cells: <c>(nx + 1) * (ny + 1)</c> on the grid and <c>nx * ny</c> between.</summary>
    internal int CellCount => (Columns + 1) * (Rows + 1) + Columns * Rows;

    /// <summary>The grid over an extent.</summary>
    /// <param name="extent">The extent of the data.</param>
    /// <param name="gridSize">The number of hexagons across it.</param>
    internal static HexLattice Over(HexExtent extent, int gridSize)
    {
        int columns = Math.Clamp(gridSize, 1, MostColumns);
        int rows = Math.Max(1, (int)(columns / Sqrt3));

        // The hexagons cover the extent in X exactly; matplotlib pads it by a billionth so that rounding cannot drop the last point.
        double padding = 1.0e-9 * (extent.XMax - extent.XMin);
        double xMin = extent.XMin - padding;
        double xMax = extent.XMax + padding;
        return new HexLattice(xMin, extent.YMin, (xMax - xMin) / columns, (extent.YMax - extent.YMin) / rows, columns, rows);
    }

    /// <summary>The centre of a cell, in data units. The cells run the grid lattice column by column, then the lattice between.</summary>
    /// <param name="cell">The cell, from 0 to <see cref="CellCount"/> - 1.</param>
    internal Point CentreOf(int cell)
    {
        int onGrid = (Columns + 1) * (Rows + 1);
        if (cell < onGrid)
        {
            int rowsOnGrid = Rows + 1;
            return new Point(_xMin + cell / rowsOnGrid * _sx, _yMin + cell % rowsOnGrid * _sy);
        }

        cell -= onGrid;
        return new Point(_xMin + (cell / Rows + 0.5) * _sx, _yMin + (cell % Rows + 0.5) * _sy);
    }

    /// <summary>The points of each cell: how many lie nearer to it than to the other lattice's cell they lie between. A point that
    /// cannot be placed, or lies outside the extent, is not counted.</summary>
    /// <param name="x">The X of each point.</param>
    /// <param name="y">The Y of each point.</param>
    /// <returns>A count for each cell, in the order of <see cref="CentreOf"/>.</returns>
    internal int[] Count(double[] x, double[] y)
    {
        int onGrid = (Columns + 1) * (Rows + 1);
        var counts = new int[CellCount];
        int n = Math.Min(x.Length, y.Length);
        for (int i = 0; i < n; i++)
        {
            if (!new Point(x[i], y[i]).IsDrawable()) continue;
            double ix = (x[i] - _xMin) / _sx;
            double iy = (y[i] - _yMin) / _sy;
            double ix1 = Math.Round(ix), iy1 = Math.Round(iy);   // the cell of the grid lattice nearest: halves go to the even one, as numpy's do
            double ix2 = Math.Floor(ix), iy2 = Math.Floor(iy);   // the cell of the lattice between whose corner is nearest

            double d1 = (ix - ix1) * (ix - ix1) + 3.0 * (iy - iy1) * (iy - iy1);
            double d2 = (ix - ix2 - 0.5) * (ix - ix2 - 0.5) + 3.0 * (iy - iy2 - 0.5) * (iy - iy2 - 0.5);
            if (d1 < d2)
            {
                if (ix1 >= 0 && ix1 <= Columns && iy1 >= 0 && iy1 <= Rows) counts[(int)ix1 * (Rows + 1) + (int)iy1]++;
            }
            else if (ix2 >= 0 && ix2 < Columns && iy2 >= 0 && iy2 < Rows)
            {
                counts[onGrid + (int)ix2 * Rows + (int)iy2]++;
            }
        }

        return counts;
    }

    /// <summary>Writes the six corners of the hexagon about a centre, in data units, from the lower right round to the bottom: a
    /// vertical edge each side and a vertex up and down (<c>[sx, sy / 3] * [[.5, -.5], [.5, .5], [0, 1], [-.5, .5], [-.5, -.5], [0, -1]]</c>).
    /// A grid draws hundreds of them, so the caller owns the six places they are written to.</summary>
    /// <param name="centre">The centre of the cell.</param>
    /// <param name="corners">Where the six corners are written; six places.</param>
    internal void OutlineAt(Point centre, Span<Point> corners)
    {
        double halfWidth = _sx / 2, sixth = _sy / 6, third = _sy / 3;
        corners[0] = new(centre.X + halfWidth, centre.Y - sixth);
        corners[1] = new(centre.X + halfWidth, centre.Y + sixth);
        corners[2] = new(centre.X, centre.Y + third);
        corners[3] = new(centre.X - halfWidth, centre.Y + sixth);
        corners[4] = new(centre.X - halfWidth, centre.Y - sixth);
        corners[5] = new(centre.X, centre.Y - third);
    }
}
