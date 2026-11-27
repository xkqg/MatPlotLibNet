// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>A grid that panels are wrapped into, row by row: how many rows and columns it has, and the cell the panel at an index
/// takes. A facet grid, small multiples, the tiles of an ops dashboard and the cells a subplot index names are all laid out by it.</summary>
/// <param name="Rows">How many rows the grid has.</param>
/// <param name="Columns">How many columns the grid has.</param>
internal readonly record struct PanelWrap(int Rows, int Columns)
{
    /// <summary>The grid that fills each row before the next: as many columns as there are panels, up to
    /// <paramref name="mostColumns"/>, and the rows they need (five panels at four a row read 4 + 1).</summary>
    /// <param name="count">How many panels there are; at least one.</param>
    /// <param name="mostColumns">How many panels a row holds at most; at least one.</param>
    internal static PanelWrap Filled(int count, int mostColumns)
    {
        int columns = Math.Min(mostColumns, count);
        return new(RowsOf(count, columns), columns);
    }

    /// <summary>The grid whose rows are as even as the panels allow: the rows <paramref name="mostColumns"/> a row need, then as few
    /// columns as those rows need (five panels at four a row read 3 + 2, never 4 + 1).</summary>
    /// <param name="count">How many panels there are; at least one.</param>
    /// <param name="mostColumns">How many panels a row holds at most; at least one.</param>
    internal static PanelWrap Balanced(int count, int mostColumns)
    {
        int rows = RowsOf(count, mostColumns);
        return new(rows, RowsOf(count, rows));
    }

    /// <summary>The cell the panel at <paramref name="index"/> takes, counted row by row from the top left.</summary>
    /// <param name="index">The panel's place, from 0.</param>
    internal GridPosition At(int index) => GridPosition.Single(index / Columns, index % Columns);

    /// <summary>How many groups of <paramref name="size"/> hold <paramref name="count"/> panels.</summary>
    private static int RowsOf(int count, int size) => ((count - 1) / size) + 1;
}
