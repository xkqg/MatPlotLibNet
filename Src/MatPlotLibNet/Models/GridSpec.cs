// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Defines a subplot grid with optional unequal row/column ratios.</summary>
public sealed record GridSpec
{
    /// <summary>Gets the number of rows in the grid.</summary>
    public int Rows { get; init; }

    /// <summary>Gets the number of columns in the grid.</summary>
    public int Cols { get; init; }

    /// <summary>Gets the relative height ratios for each row, or null for equal heights. Length must equal <see cref="Rows"/>.</summary>
    public double[]? HeightRatios { get; init; }

    /// <summary>Gets the relative width ratios for each column, or null for equal widths. Length must equal <see cref="Cols"/>.</summary>
    public double[]? WidthRatios { get; init; }
}

/// <summary>Specifies which cells in a <see cref="GridSpec"/> an axes occupies (supports spanning).</summary>
/// <param name="RowStart">Starting row (0-based, inclusive).</param>
/// <param name="RowEnd">Ending row (0-based, exclusive).</param>
/// <param name="ColStart">Starting column (0-based, inclusive).</param>
/// <param name="ColEnd">Ending column (0-based, exclusive).</param>
public readonly record struct GridPosition(int RowStart, int RowEnd, int ColStart, int ColEnd)
{
    /// <summary>Creates a single-cell position at the given row and column.</summary>
    public static GridPosition Single(int row, int col) => new(row, row + 1, col, col + 1);

    /// <summary>Creates a multi-cell spanning position.</summary>
    public static GridPosition Span(int rowStart, int rowEnd, int colStart, int colEnd) =>
        new(rowStart, rowEnd, colStart, colEnd);
}
