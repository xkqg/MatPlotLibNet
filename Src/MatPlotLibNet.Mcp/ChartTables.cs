// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Mcp;

/// <summary>One column of a tabulated chart, in the words the tool schema publishes: the header a reader sees,
/// what the column holds (<c>number</c>, <c>date</c> or <c>text</c>), and which axis its values are positions on
/// (<c>x</c>, <c>y</c>, or <c>none</c> for a value that is not a coordinate).</summary>
/// <param name="Header">The header text, as the reader sees it.</param>
/// <param name="Kind">What the column holds: <c>number</c>, <c>date</c> or <c>text</c>.</param>
/// <param name="Axis">The axis the values are positions on: <c>x</c>, <c>y</c> or <c>none</c>.</param>
public sealed record ChartTableColumn(string Header, string Kind, string Axis);

/// <summary>One table of a tabulated chart: its caption, its columns, and its rows as values. A cell is a number,
/// a date written out in full, a text, or null where the table is rectangular but there was no value.</summary>
/// <param name="Caption">The caption — the chart's own name — or null when it has none.</param>
/// <param name="Columns">The columns, in order.</param>
/// <param name="Rows">The rows; every row has exactly one cell per column.</param>
public sealed record ChartTableData(
    string? Caption,
    IReadOnlyList<ChartTableColumn> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows);

/// <summary>The data of a chart as values rather than as text — what <c>chart_data_table</c> returns beside its
/// markdown, so a model can add a column up without parsing a pipe table back into numbers first.
/// <para>The two halves say the same thing in two forms, and they differ deliberately in one place: markdown is
/// for reading, so a date is spelled the way a person reads it and only as far as its own precision goes, while
/// this is for a machine, so a date is always written out in full in the one order that sorts correctly.</para>
/// </summary>
/// <param name="Tables">One table per group of series that share an x — the same grouping the markdown uses.</param>
public sealed record ChartTables(IReadOnlyList<ChartTableData> Tables)
{
    /// <summary>The date format a machine reads: year to millisecond, sortable as text, never culture-dependent.</summary>
    private const string DateFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    /// <summary>The tables of a figure, projected onto the wire vocabulary.</summary>
    public static ChartTables From(IReadOnlyList<ChartDataTable> tables) =>
        new([.. tables.Select(Project)]);

    private static ChartTableData Project(ChartDataTable table) =>
        new(table.Caption,
            [.. table.Columns.Select(column => new ChartTableColumn(column.Header, Name(column.Kind), Name(column.Axis)))],
            [.. table.Rows.Select(row => (IReadOnlyList<object?>)
                [.. row.Select((cell, index) => Value(cell, table.Columns[index].Kind))])]);

    /// <summary>What one cell is worth: a number as a number, a date written out, a text as itself, and null for
    /// the slot that exists because the table is rectangular rather than because there was a value.</summary>
    private static object? Value(DataCell cell, DataColumnKind kind) => cell.Kind switch
    {
        DataCellKind.Text => cell.Text,
        DataCellKind.Number when kind == DataColumnKind.Date && IsWritableAsADate(cell.Number)
            => DateTime.FromOADate(cell.Number).ToString(DateFormat, CultureInfo.InvariantCulture),
        DataCellKind.Number => cell.Number,
        _ => null,
    };

    // The range a date can be written in is finite; outside it the number is reported as the number it is, rather
    // than throwing away a whole table over one cell. Same bounds the markdown uses.
    private static bool IsWritableAsADate(double value) =>
        double.IsFinite(value) && value >= -657435 && value < 2958466;

    private static string Name(DataColumnKind kind) => kind switch
    {
        DataColumnKind.Date => "date",
        DataColumnKind.Text => "text",
        _ => "number",
    };

    private static string Name(DataAxis axis) => axis switch
    {
        DataAxis.X => "x",
        DataAxis.Y => "y",
        _ => "none",
    };
}
