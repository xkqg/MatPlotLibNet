// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Models;

/// <summary>The data of a chart, in a form a reader who cannot see the picture can read: columns with a kind,
/// rows of cells, and a caption that is the figure's accessible name. It is the DATA — every point the series
/// holds, at full resolution — never the picture: a viewport window or a downsampled line draws fewer points
/// than the series has, and the table says what the series has.
/// <para>Three textual forms. <see cref="ToHtml"/> and <see cref="ToMarkdown"/> are for reading, so they cap
/// the rows (<see cref="DefaultMaxRows"/>) and say how many were left out; <see cref="ToCsv"/> is for a
/// machine, so it never caps. All three spell a cell the same way: numbers in full and culture-invariant, dates
/// as dates, texts as they are.</para></summary>
public sealed class ChartDataTable
{
    /// <summary>The row cap the readable forms apply when none is given.</summary>
    public const int DefaultMaxRows = 1000;

    /// <summary>Creates a table.</summary>
    /// <param name="caption">The caption — the figure's accessible name — or null for none.</param>
    /// <param name="columns">The columns, at least one.</param>
    /// <param name="rows">The rows; every row has exactly one cell per column.</param>
    /// <exception cref="ArgumentException">No columns, or a row whose cell count differs from the column count.</exception>
    public ChartDataTable(string? caption, IReadOnlyList<ChartDataColumn> columns, IReadOnlyList<IReadOnlyList<DataCell>> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);
        columns.ThrowIfEmpty("A table needs at least one column.");

        rows.Select(row => row.Count).ThrowIfNotAllEqual(nameof(rows), expected: columns.Count);

        Caption = caption;
        Columns = columns;
        Rows = rows;
    }

    /// <summary>The caption, or null when the figure has no accessible name.</summary>
    public string? Caption { get; }

    /// <summary>The columns.</summary>
    public IReadOnlyList<ChartDataColumn> Columns { get; }

    /// <summary>The rows — all of them.</summary>
    public IReadOnlyList<IReadOnlyList<DataCell>> Rows { get; }

    /// <summary>How many rows the table holds.</summary>
    public int RowCount => Rows.Count;

    /// <summary>The long form of a grid of <paramref name="rows"/> by <paramref name="columns"/>: a row of the table per cell, the rows of
    /// the grid in order and the cells of each row in order, each row what <paramref name="rowOf"/> says of its cell. The one walk over the
    /// cells of a grid every series that holds a grid (a heat map, an image, a mesh, a contour, a field, a surface) tabulates by; a wide grid
    /// would make its column headers indices, which say nothing a reader can use.</summary>
    /// <param name="rows">The number of rows of the grid.</param>
    /// <param name="columns">The number of columns of the grid.</param>
    /// <param name="headers">The columns of the table.</param>
    /// <param name="rowOf">The cells of the table's row for the grid's cell at a row and a column.</param>
    /// <returns>The table, with no caption.</returns>
    internal static ChartDataTable PerCell(int rows, int columns, IReadOnlyList<ChartDataColumn> headers,
        Func<int, int, IReadOnlyList<DataCell>> rowOf)
    {
        var cells = new List<IReadOnlyList<DataCell>>(rows * columns);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                cells.Add(rowOf(r, c));
            }
        }

        return new ChartDataTable(null, headers, cells);
    }

    /// <summary>The long form of the values of a grid placed at <paramref name="x"/> and <paramref name="y"/> (<see cref="PerCell"/>): each
    /// cell's x and y, the index of its column or its row where the grid names no coordinate, then its value in each of
    /// <paramref name="values"/>, under each one's header. The grid's size is the first values'.</summary>
    /// <param name="x">The X coordinate of each column.</param>
    /// <param name="y">The Y coordinate of each row.</param>
    /// <param name="values">The grids of values at those places, each with its header.</param>
    /// <returns>The table.</returns>
    internal static ChartDataTable OverGrid(double[] x, double[] y, params GridValues[] values)
    {
        ChartDataColumn[] headers = [ChartDataColumn.Position(DataAxis.X), ChartDataColumn.Position(DataAxis.Y), .. values.Select(v => new ChartDataColumn(v.Header))];
        return PerCell(values[0].Values.GetLength(0), values[0].Values.GetLength(1), headers, (r, c) =>
        {
            var row = new DataCell[values.Length + 2];
            row[0] = DataCell.FromNumber(x.ElementOr(c, c));
            row[1] = DataCell.FromNumber(y.ElementOr(r, r));
            for (int k = 0; k < values.Length; k++) row[k + 2] = DataCell.FromNumber(values[k].Values[r, c]);
            return row;
        });
    }

    /// <summary>A table from parallel numeric columns — the shape most series have: N columns read by index, one
    /// row per index. The row count is the SHORTEST column, so a series whose arrays disagree in length (the
    /// models allow it; only the builder validates) reports the pairs it has instead of throwing.</summary>
    internal static ChartDataTable FromNumberColumns(
        IReadOnlyList<ChartDataColumn> columns, IReadOnlyList<IReadOnlyList<double>> values)
    {
        int count = values.Count == 0 ? 0 : values.Min(column => column.Count);
        var rows = new IReadOnlyList<DataCell>[count];
        for (int r = 0; r < count; r++)
        {
            var cells = new DataCell[values.Count];
            for (int c = 0; c < values.Count; c++)
            {
                cells[c] = DataCell.FromNumber(values[c][r]);
            }

            rows[r] = cells;
        }

        return new ChartDataTable(null, columns, rows);
    }

    /// <summary>The table of a series whose point is its x and its y, and whose other columns are numbers read by index: <c>x</c>, then
    /// the y under <paramref name="yName"/>, then each of <paramref name="more"/> that is given, one row per index. The row count is the
    /// shortest column's (<see cref="FromNumberColumns"/>).</summary>
    /// <param name="yName">What the series calls its y; <see langword="null"/> for the bare header (<see cref="ChartDataColumn.Position"/>).</param>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values.</param>
    /// <param name="more">The columns after the point; one that is <see langword="null"/> is left out.</param>
    /// <returns>The table, with no caption.</returns>
    internal static ChartDataTable XY(string? yName, IReadOnlyList<double> x, IReadOnlyList<double> y, params ReadOnlySpan<NumberColumn?> more)
    {
        var columns = new List<ChartDataColumn>(2 + more.Length) { ChartDataColumn.Position(DataAxis.X), ChartDataColumn.Position(DataAxis.Y, yName) };
        var values = new List<IReadOnlyList<double>>(2 + more.Length) { x, y };
        foreach (var column in more)
        {
            if (column is not { } given) continue;
            columns.Add(given.Column);
            values.Add(given.Values);
        }

        return FromNumberColumns(columns, values);
    }

    /// <summary>The table of points that stand over a plane and carry a height: <c>x</c>, <c>y</c>, then the height under
    /// <paramref name="heightName"/>, then each of <paramref name="more"/> that is given, one row per index (<see cref="XY"/>).</summary>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values.</param>
    /// <param name="z">The heights.</param>
    /// <param name="heightName">What the series calls its heights (<c>z</c> when it calls them nothing else).</param>
    /// <param name="more">The columns after the height; one that is <see langword="null"/> is left out.</param>
    /// <returns>The table, with no caption.</returns>
    internal static ChartDataTable XYZ(IReadOnlyList<double> x, IReadOnlyList<double> y, IReadOnlyList<double> z, string heightName, params ReadOnlySpan<NumberColumn?> more) =>
        XY(null, x, y, [new NumberColumn(heightName, z), .. more]);

    /// <summary>A table of one sequence of values read by its position: an <c>index</c> column, then <paramref name="valueColumn"/>,
    /// one row per value (the samples a curve is estimated from, the points of a sparkline).</summary>
    /// <param name="valueColumn">The column of the values.</param>
    /// <param name="values">The values, in order.</param>
    /// <returns>The table.</returns>
    internal static ChartDataTable FromIndexedValues(ChartDataColumn valueColumn, double[] values)
    {
        var rows = new IReadOnlyList<DataCell>[values.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = [DataCell.FromNumber(i), DataCell.FromNumber(values[i])];
        }

        return new ChartDataTable(null, [new("index"), valueColumn], rows);
    }

    /// <summary>The table of a series that stands on categories: each category as text under the one header <c>category</c>, then each of
    /// <paramref name="values"/> under its own header, one row for each category the shortest column reaches.</summary>
    /// <param name="categories">The name of each category.</param>
    /// <param name="values">The number columns after the category, each read by index.</param>
    /// <returns>The table, with no caption.</returns>
    internal static ChartDataTable ByCategory(IReadOnlyList<string> categories, params ReadOnlySpan<NumberColumn> values)
    {
        int count = categories.Count;
        foreach (var column in values) count = Math.Min(count, column.Values.Count);
        var rows = new IReadOnlyList<DataCell>[count];
        for (int r = 0; r < count; r++)
        {
            var cells = new DataCell[1 + values.Length];
            cells[0] = DataCell.FromText(categories[r]);
            for (int c = 0; c < values.Length; c++) cells[c + 1] = DataCell.FromNumber(values[c].Values[r]);
            rows[r] = cells;
        }

        var headers = new ChartDataColumn[1 + values.Length];
        headers[0] = new("category", DataColumnKind.Text);
        for (int c = 0; c < values.Length; c++) headers[c + 1] = values[c].Column;
        return new ChartDataTable(null, headers, rows);
    }

    /// <summary>The long form of samples that belong to groups (the data sets of a point plot, the variables of a pair grid, the rows of
    /// a broken bar): a row for each sample, the name of its group as text first, then the cells the caller makes of the sample.</summary>
    /// <typeparam name="TItem">The kind of sample.</typeparam>
    /// <param name="headers">The columns of the table, the group's first.</param>
    /// <param name="names">The name of each group, or <see langword="null"/>.</param>
    /// <param name="word">What a group that was given no name is called by, before its place counted from 1 (<see cref="ArrayElementExtensions"/>).</param>
    /// <param name="groups">The samples of each group.</param>
    /// <param name="cellsOf">The cells of a row after the group's name, for one sample.</param>
    /// <returns>The table, with no caption.</returns>
    internal static ChartDataTable OfGroups<TItem>(IReadOnlyList<ChartDataColumn> headers, IReadOnlyList<string>? names, string word,
        IReadOnlyList<IReadOnlyList<TItem>> groups, Func<TItem, DataCell[]> cellsOf)
    {
        var rows = new List<IReadOnlyList<DataCell>>();
        for (int g = 0; g < groups.Count; g++)
        {
            var label = DataCell.FromText(names.NameOr(g, word));
            foreach (var item in groups[g]) rows.Add([label, .. cellsOf(item)]);
        }

        return new ChartDataTable(null, headers, rows);
    }

    /// <summary>The table as an HTML <c>&lt;table class="mpl-data-table"&gt;</c>: a <c>&lt;caption&gt;</c> when there is
    /// one, column headers as <c>&lt;th scope="col"&gt;</c>, the first cell of every row as <c>&lt;th scope="row"&gt;</c>.
    /// Headers and cells are text nodes, never attributes. Past <paramref name="maxRows"/> rows a last row says
    /// how many were left out. The host places it beside the picture and supplies the CSS.</summary>
    /// <param name="maxRows">The most rows written, at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRows"/> is below 1.</exception>
    public string ToHtml(int maxRows = DefaultMaxRows)
    {
        maxRows.ThrowIfBelow(1, nameof(maxRows));
        var sb = new StringBuilder();
        sb.Append("<table class=\"mpl-data-table\">");
        if (Caption is not null)
        {
            sb.Append("<caption>").Append(Caption.EscapeForXml()).Append("</caption>");
        }

        sb.Append("<thead><tr>");
        foreach (var column in Columns)
        {
            sb.Append("<th scope=\"col\">").Append(column.Header.EscapeForXml()).Append("</th>");
        }

        sb.Append("</tr></thead><tbody>");
        int written = 0;
        foreach (var row in Rows)
        {
            if (written == maxRows)
            {
                break;
            }

            sb.Append("<tr>");
            for (int c = 0; c < row.Count; c++)
            {
                string text = CellText(row[c], Columns[c].Kind).EscapeForXml();
                sb.Append(c == 0 ? "<th scope=\"row\">" : "<td>").Append(text).Append(c == 0 ? "</th>" : "</td>");
            }

            sb.Append("</tr>");
            written++;
        }

        if (written < RowCount)
        {
            sb.Append("<tr><td colspan=\"").Append(Columns.Count.InvariantText())
              .Append("\">").Append(LeftOut(RowCount - written)).Append("</td></tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    /// <summary>The table as a Markdown pipe table under a bold caption line when there is one; a pipe inside a
    /// header or a cell is escaped. Past <paramref name="maxRows"/> rows a last row says how many were left out.
    /// Lines are joined with <c>\n</c> and there is no trailing newline.</summary>
    /// <param name="maxRows">The most rows written, at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRows"/> is below 1.</exception>
    public string ToMarkdown(int maxRows = DefaultMaxRows)
    {
        maxRows.ThrowIfBelow(1, nameof(maxRows));
        var lines = new List<string>();
        if (Caption is not null)
        {
            lines.Add("**" + Caption + "**");
            lines.Add("");
        }

        lines.Add("| " + string.Join(" | ", Columns.Select(c => MarkdownCell(c.Header))) + " |");
        lines.Add("|" + string.Concat(Enumerable.Repeat("---|", Columns.Count)));
        int written = 0;
        foreach (var row in Rows)
        {
            if (written == maxRows)
            {
                break;
            }

            lines.Add("| " + string.Join(" | ", row.Select((cell, c) => MarkdownCell(CellText(cell, Columns[c].Kind)))) + " |");
            written++;
        }

        if (written < RowCount)
        {
            lines.Add("| " + LeftOut(RowCount - written) + string.Concat(Enumerable.Repeat(" |", Columns.Count)));
        }

        return string.Join('\n', lines);
    }

    /// <summary>The table as RFC 4180 CSV: a header record, one record per row, <c>CRLF</c> line ends, a field
    /// quoted when it carries a comma, a quote or a line break (with quotes doubled). UTF-8 without a byte-order
    /// mark when written to a file; never capped — a machine reads it.</summary>
    public string ToCsv()
    {
        var sb = new StringBuilder();
        AppendCsvRecord(sb, Columns.Select(c => c.Header));
        foreach (var row in Rows)
        {
            AppendCsvRecord(sb, row.Select((cell, c) => CellText(cell, Columns[c].Kind)));
        }

        return sb.ToString();
    }

    private static void AppendCsvRecord(StringBuilder sb, IEnumerable<string> fields)
    {
        sb.Append(string.Join(',', fields.Select(CsvField))).Append("\r\n");
    }

    private static string CsvField(string field) =>
        field.AsSpan().IndexOfAny(",\"\r\n") < 0 ? field : "\"" + field.Replace("\"", "\"\"") + "\"";

    private static string MarkdownCell(string text) => text.Replace("|", "\\|");

    private static string LeftOut(int count) => count == 1 ? "… 1 more row" : $"… {count.InvariantText()} more rows";

    /// <summary>How a cell is spelled, in every form. A number prints in full ("G", invariant: NaN as
    /// <c>NaN</c>, the infinities as <c>Infinity</c>/<c>-Infinity</c>); in a <see cref="DataColumnKind.Date"/>
    /// column a finite number inside the OLE Automation range prints as a date - to the minute, second or
    /// millisecond, whichever its own value carries - anything else as the number.</summary>
    internal static string CellText(DataCell cell, DataColumnKind kind)
    {
        switch (cell.Kind)
        {
            case DataCellKind.Text:
                return cell.Text!;
            case DataCellKind.Number:
                double value = cell.Number;
                if (kind == DataColumnKind.Date && value.IsOleDate)
                {
                    // As far into the time as the value goes and no further: a midnight is a date, a whole
                    // minute is a minute. Cutting at the minute looked tidy and threw away exactly what told a
                    // control room's 30-second samples apart.
                    var when = DateTime.FromOADate(value);
                    string format = when.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd"
                        : when.Second == 0 && when.Millisecond == 0 ? "yyyy-MM-dd HH:mm"
                        : when.Millisecond == 0 ? "yyyy-MM-dd HH:mm:ss"
                        : "yyyy-MM-dd HH:mm:ss.fff";
                    return when.InvariantText(format);
                }

                return value.InvariantText();
            default:
                return "";
        }
    }
}
