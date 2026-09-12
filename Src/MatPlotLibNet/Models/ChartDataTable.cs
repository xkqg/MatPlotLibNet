// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
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
        if (columns.Count == 0)
        {
            throw new ArgumentException("A table needs at least one column.", nameof(columns));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count != columns.Count)
            {
                throw new ArgumentException(
                    $"Row {i} has {rows[i].Count} cells; the table has {columns.Count} columns.", nameof(rows));
            }
        }

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

    /// <summary>The table as an HTML <c>&lt;table class="mpl-data-table"&gt;</c>: a <c>&lt;caption&gt;</c> when there is
    /// one, column headers as <c>&lt;th scope="col"&gt;</c>, the first cell of every row as <c>&lt;th scope="row"&gt;</c>.
    /// Headers and cells are text nodes, never attributes. Past <paramref name="maxRows"/> rows a last row says
    /// how many were left out. The host places it beside the picture and supplies the CSS.</summary>
    /// <param name="maxRows">The most rows written, at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRows"/> is below 1.</exception>
    public string ToHtml(int maxRows = DefaultMaxRows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);
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
            sb.Append("<tr><td colspan=\"").Append(Columns.Count.ToString(CultureInfo.InvariantCulture))
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
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);
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

    private static string LeftOut(int count) => count == 1 ? "… 1 more row" : $"… {count.ToString(CultureInfo.InvariantCulture)} more rows";

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
                if (kind == DataColumnKind.Date && IsOleDate(value))
                {
                    // As far into the time as the value goes and no further: a midnight is a date, a whole
                    // minute is a minute. Cutting at the minute looked tidy and threw away exactly what told a
                    // control room's 30-second samples apart.
                    var when = DateTime.FromOADate(value);
                    string format = when.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd"
                        : when.Second == 0 && when.Millisecond == 0 ? "yyyy-MM-dd HH:mm"
                        : when.Millisecond == 0 ? "yyyy-MM-dd HH:mm:ss"
                        : "yyyy-MM-dd HH:mm:ss.fff";
                    return when.ToString(format, CultureInfo.InvariantCulture);
                }

                return value.ToString("G", CultureInfo.InvariantCulture);
            default:
                return "";
        }
    }

    // DateTime.FromOADate accepts [-657435, 2958466): anything else throws, and a table does not die on one cell.
    private static bool IsOleDate(double value) => double.IsFinite(value) && value >= -657435 && value < 2958466;
}
