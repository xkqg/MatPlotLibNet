// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies which cells in a <see cref="GridSpec"/> an axes occupies (supports spanning).</summary>
/// <param name="RowStart">Starting row (0-based, inclusive).</param>
/// <param name="RowEnd">Ending row (0-based, exclusive).</param>
/// <param name="ColStart">Starting column (0-based, inclusive).</param>
/// <param name="ColEnd">Ending column (0-based, exclusive).</param>
public readonly record struct GridPosition(int RowStart, int RowEnd, int ColStart, int ColEnd)
{
    /// <summary>Creates a single-cell position at the given row and column.</summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="col">The zero-based column index.</param>
    public static GridPosition Single(int row, int col) => new(row, row + 1, col, col + 1);

}
