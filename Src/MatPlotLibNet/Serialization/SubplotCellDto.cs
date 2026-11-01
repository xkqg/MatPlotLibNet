// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of the cell a subplot was added to with <c>AddSubPlot(rows, cols, index)</c>
/// (<see cref="Axes.GridRows"/>, <see cref="Axes.GridCols"/>, <see cref="Axes.GridIndex"/>): the <c>subplot</c> field of a
/// subplot, named as matplotlib's <c>add_subplot(rows, cols, index)</c> names it. Both layouts read the cell (the default one
/// places the subplot by it, the constrained one measures which edges of the figure it touches by it), so without it a figure
/// read back laid its subplots out in one row. A figure whose one subplot stands in the one cell of a 1 by 1 grid draws the same
/// without it, so it writes none and keeps the bytes it wrote before the cell reached the JSON; a subplot placed another way (by
/// a grid specification, or not at all) writes none either. A 1.28.0 reader skips the field.</summary>
internal sealed record SubplotCellDto
{
    /// <summary>The number of rows of the grid.</summary>
    public int Rows { get; init; }

    /// <summary>The number of columns of the grid.</summary>
    public int Cols { get; init; }

    /// <summary>The one-based cell the subplot stands in, counted along the rows from the top left.</summary>
    public int Index { get; init; }

    /// <summary>The cell of <paramref name="axes"/>, one of <paramref name="subPlots"/> subplots of its figure, or
    /// <see langword="null"/> when there is nothing to write: the axes names no cell (it was placed another way), or it is the
    /// figure's only subplot and stands in the one cell of a 1 by 1 grid, which is where a subplot that names no cell stands.
    /// Two subplots in that one cell stand over each other, where two that name none stand side by side, so they write it.</summary>
    /// <param name="axes">The subplot.</param>
    /// <param name="subPlots">How many subplots its figure has.</param>
    internal static SubplotCellDto? Of(Axes axes, int subPlots) =>
        axes.GridRows == 0 || (subPlots == 1 && axes.GridRows * axes.GridCols == 1)
            ? null
            : new() { Rows = axes.GridRows, Cols = axes.GridCols, Index = axes.GridIndex };

    /// <summary>Adds a subplot standing in this cell to <paramref name="figure"/> and returns it.</summary>
    /// <param name="figure">The figure being read.</param>
    internal Axes AddTo(Figure figure) => figure.AddSubPlot(Rows, Cols, Index);
}
