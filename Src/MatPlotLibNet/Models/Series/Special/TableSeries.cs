// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a data table rendered inside the plot area.</summary>
public sealed class TableSeries : ChartSeries
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The text of the data cells, as an array of rows each holding an array of cell texts. A row shorter than
    /// the longest one leaves its missing cells empty.</summary>
    public string[][] CellData { get; }

    /// <summary>The text of the header cell above each data column, by position; null draws no header row. A column
    /// past the end of the array gets no header cell.</summary>
    public string[]? ColumnHeaders { get; set; }

    /// <summary>The text of the header cell at the start of each row, by position; null draws no row-header column.
    /// A row past the end of the array has an empty header cell.</summary>
    public string[]? RowHeaders { get; set; }

    /// <summary>How the text of each data column is aligned in its cell, by position; <see langword="null"/> (the default), or a
    /// column past the end of the array, is left aligned. A left aligned text starts <see cref="CellPadding"/> in from the left
    /// edge of its cell, a right aligned one ends that far from the right edge, a centred one is centred. The headers of the
    /// columns and of the rows stay centred.</summary>
    public TextAlignment[]? ColumnAlignments { get; set; }

    private double[]? _columnWidths;

    /// <summary>The relative width of each displayed column, by position: the columns share the width of the plot in these
    /// proportions. When the table has row headers the first width is that of the row-header column and the data columns follow.
    /// <see langword="null"/> (the default) makes every column as wide as the others, and a column past the end of the array
    /// counts for 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A width is not a finite number above zero.</exception>
    public double[]? ColumnWidths
    {
        get => _columnWidths;
        set
        {
            if (value is not null && value.Any(width => !(double.IsFinite(width) && width > 0)))
            {
                throw new ArgumentOutOfRangeException(nameof(ColumnWidths), value, "A column's relative width is a finite number above zero.");
            }

            _columnWidths = value;
        }
    }

    /// <summary>The height of every row, header row included, in pixels. Default 25.</summary>
    public double CellHeight { get; set; } = 25;

    /// <summary>The space, in pixels, between a left or right aligned text and the edge of its cell. Default 4; a
    /// centred text ignores it.</summary>
    public double CellPadding { get; set; } = 4;

    /// <summary>The fill of the column-header and row-header cells; null draws them light grey.</summary>
    public Color? HeaderColor { get; set; }

    /// <summary>The fill of the data cells; null draws them white.</summary>
    public Color? CellColor { get; set; }

    /// <summary>The colour of the lines around every cell; null draws them dark grey.</summary>
    public Color? BorderColor { get; set; }

    /// <summary>The size of the text in every cell, in points. Default 11.</summary>
    public double FontSize { get; set; } = 11;

    /// <summary>Initializes a new instance of <see cref="TableSeries"/> with the specified cell data.</summary>
    /// <param name="cellData">2D array of cell text values (rows × columns).</param>
    public TableSeries(string[][] cellData)
    {
        CellData = cellData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
        => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "table",
        TableCellData = CellData,
        ColumnHeaders = ColumnHeaders,
        RowHeaders = RowHeaders,
        CellHeight = CellHeight.OrNullAtRest(25.0),
        CellPadding = CellPadding.OrNullAtRest(4.0),
        ColumnAlignments = ColumnAlignments,
        ColumnWidths = ColumnWidths,
        HeaderColor = HeaderColor,
        FillColor = CellColor,
        EdgeColor = BorderColor,
        FontSize = FontSize.OrNullAtRest(11.0),
    };

    /// <summary>Reconstructs a <see cref="TableSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static TableSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Table(dto.TableCellData ?? []);
        if (dto.ColumnHeaders is not null)
        {
            s.ColumnHeaders = dto.ColumnHeaders;
        }
        if (dto.RowHeaders is not null)
        {
            s.RowHeaders = dto.RowHeaders;
        }
        if (dto.CellHeight.HasValue)
        {
            s.CellHeight = dto.CellHeight.Value;
        }
        if (dto.CellPadding.HasValue)
        {
            s.CellPadding = dto.CellPadding.Value;
        }
        s.ColumnAlignments = dto.ColumnAlignments;
        s.ColumnWidths = dto.ColumnWidths;
        s.HeaderColor = dto.HeaderColor;
        s.CellColor = dto.FillColor;
        s.BorderColor = dto.EdgeColor;
        if (dto.FontSize.HasValue)
        {
            s.FontSize = dto.FontSize.Value;
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>A table series is already a table; it hands over its own cells and headers unchanged. Its cells
    /// are pre-formatted strings, so every column is text. A row-header column keeps its place at the front under
    /// an empty header - the shape an HTML table wants.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int width = CellData.Length == 0 ? 0 : CellData.Max(row => row.Length);
        width = Math.Max(width, ColumnHeaders?.Length ?? 0);
        bool rowHeaders = RowHeaders is { Length: > 0 };

        var columns = new List<ChartDataColumn>(width + 1);
        if (rowHeaders)
        {
            columns.Add(new("", DataColumnKind.Text));
        }

        for (int c = 0; c < width; c++)
        {
            string header = ColumnHeaders is { } headers && c < headers.Length
                ? headers[c]
                : "column " + (c + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            columns.Add(new(header, DataColumnKind.Text));
        }

        if (columns.Count == 0)
        {
            columns.Add(new("", DataColumnKind.Text));
        }

        var rows = new List<IReadOnlyList<DataCell>>(CellData.Length);
        for (int r = 0; r < CellData.Length; r++)
        {
            var cells = new List<DataCell>(columns.Count);
            if (rowHeaders)
            {
                cells.Add(DataCell.FromText(r < RowHeaders!.Length ? RowHeaders[r] : ""));
            }

            for (int c = 0; c < width; c++)
            {
                cells.Add(DataCell.FromText(c < CellData[r].Length ? CellData[r][c] : ""));
            }

            rows.Add(cells);
        }

        return new ChartDataTable(null, columns, rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
