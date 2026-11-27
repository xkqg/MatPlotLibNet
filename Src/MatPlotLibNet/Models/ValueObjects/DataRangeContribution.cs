// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A series' contribution to the overall axes data range. Null values indicate no contribution to that axis.</summary>
/// <param name="XMin">Minimum X value contributed, or null.</param>
/// <param name="XMax">Maximum X value contributed, or null.</param>
/// <param name="YMin">Minimum Y value contributed, or null.</param>
/// <param name="YMax">Maximum Y value contributed, or null.</param>
/// <param name="StickyXMin">Sticky-edge X minimum — if set, `axes.xmargin` expansion will not push the computed xMin below this value. Mirrors matplotlib's <c>Artist.sticky_edges.x</c>.</param>
/// <param name="StickyXMax">Sticky-edge X maximum. Analogous to <paramref name="StickyXMin"/>.</param>
/// <param name="StickyYMin">Sticky-edge Y minimum — matplotlib's <c>BarContainer</c> sets this to <c>0</c> so the y-axis never pads below the bar baseline.</param>
/// <param name="StickyYMax">Sticky-edge Y maximum.</param>
/// <param name="StickyZMin">Sticky-edge Z minimum (3-D only): the cube's Z range is not padded below it. No built-in series sets one (matplotlib's 3-D axes knows no sticky edge); one of your own may.</param>
/// <param name="StickyZMax">Sticky-edge Z maximum (3-D only).</param>
/// <param name="ZMin">Minimum Z value contributed (3-D only), or null.</param>
/// <param name="ZMax">Maximum Z value contributed (3-D only), or null.</param>
public readonly record struct DataRangeContribution(
    double? XMin, double? XMax, double? YMin, double? YMax,
    double? StickyXMin = null, double? StickyXMax = null,
    double? StickyYMin = null, double? StickyYMax = null,
    double? StickyZMin = null, double? StickyZMax = null,
    double? ZMin = null, double? ZMax = null)
{
    /// <summary>The range of data that fill it to every edge (a grid, a mesh, a field, a chart of columns), sticky at each of its four
    /// edges, so no margin reaches past the data (matplotlib's <c>sticky_edges</c> of a <c>QuadMesh</c>, a <c>ContourSet</c>, a
    /// streamplot's collection). The one place the four sticky edges are set to the range.</summary>
    /// <param name="x">The range along X; <see langword="null"/> when nothing is drawable there, which adds and pins nothing.</param>
    /// <param name="y">The range along Y; <see langword="null"/> likewise.</param>
    /// <returns>The pinned range.</returns>
    internal static DataRangeContribution Pinned(Rendering.Range1D? x, Rendering.Range1D? y) =>
        new(x?.Lo, x?.Hi, y?.Lo, y?.Hi, StickyXMin: x?.Lo, StickyXMax: x?.Hi, StickyYMin: y?.Lo, StickyYMax: y?.Hi);

    /// <summary>The range of the points a surface fills to every edge (a mesh, a triangulation, the grid of a field or of a contour), pinned
    /// (<see cref="Pinned"/>) to their drawable extent along each axis: a missing value (NaN) and an infinity are left out, and an axis with
    /// no drawable point adds and pins nothing. The one place a surface's points are made its range.</summary>
    /// <param name="x">The points' X coordinates.</param>
    /// <param name="y">The points' Y coordinates.</param>
    /// <returns>The pinned range.</returns>
    internal static DataRangeContribution PinnedOver(double[] x, double[] y) => Pinned(x.DrawableRange(), y.DrawableRange());

    /// <summary>The range spanned along X and Y (and Z in 3-D) with no sticky edge, so the axis pads it as any range: the twin of
    /// <see cref="Pinned"/> for data that fill no edge (a line, scattered points, a field of arrows), and what a series that pins some of
    /// its edges adds them to. With <see cref="Pinned"/>, the one place the ends of the ranges are made a contribution.</summary>
    /// <param name="x">The range along X; <see langword="null"/> when nothing is drawable there, which adds nothing.</param>
    /// <param name="y">The range along Y; <see langword="null"/> likewise.</param>
    /// <param name="z">The range along Z in 3-D; <see langword="null"/> likewise, and in 2-D.</param>
    /// <returns>The range.</returns>
    internal static DataRangeContribution Spanning(Rendering.Range1D? x, Rendering.Range1D? y, Rendering.Range1D? z = null) =>
        new(x?.Lo, x?.Hi, y?.Lo, y?.Hi, ZMin: z?.Lo, ZMax: z?.Hi);

    /// <summary>The range of points that fill no edge, spanned (<see cref="Spanning"/>) over their drawable extent along each axis: a
    /// missing value (NaN) and an infinity are left out. The twin of <see cref="PinnedOver"/>.</summary>
    /// <param name="x">The points' X coordinates.</param>
    /// <param name="y">The points' Y coordinates.</param>
    /// <returns>The range.</returns>
    internal static DataRangeContribution Over(double[] x, double[] y) => Spanning(x.DrawableRange(), y.DrawableRange());

    /// <summary>The range of data that fill no edge (the nodes of a graph, the tails of a rotation chart), each axis opened by half a
    /// unit on each side when it is flat (<see cref="Rendering.Range1D.OpenedWhenFlat"/>), so a single point still has a span; no edge is
    /// sticky, so the axis pads it as any range.</summary>
    /// <param name="x">The range along X.</param>
    /// <param name="y">The range along Y.</param>
    /// <returns>The range.</returns>
    internal static DataRangeContribution Opened(Rendering.Range1D x, Rendering.Range1D y)
    {
        var (xLo, xHi) = x.OpenedWhenFlat();
        var (yLo, yHi) = y.OpenedWhenFlat();
        return new(xLo, xHi, yLo, yHi);
    }

    /// <summary>The range of the cells of a matrix, one unit each from 0: pinned (<see cref="Pinned"/>) from 0 to the number of columns
    /// along X and to the number of rows along Y; a matrix with no row or no column adds nothing.</summary>
    /// <param name="cells">The matrix, indexed [row, column].</param>
    /// <returns>The range.</returns>
    internal static DataRangeContribution OfCells(double[,] cells)
    {
        int rows = cells.GetLength(0), cols = cells.GetLength(1);
        return rows == 0 || cols == 0 ? default : Pinned(new(0, cols), new(0, rows));
    }
}
