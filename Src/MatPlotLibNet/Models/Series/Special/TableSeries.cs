// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a data table rendered inside the plot area.</summary>
public sealed class TableSeries : ChartSeries
{
    public string[][] CellData { get; }

    public string[]? ColumnHeaders { get; set; }

    public string[]? RowHeaders { get; set; }

    public double CellHeight { get; set; } = 25;

    public double CellPadding { get; set; } = 4;

    public Color? HeaderColor { get; set; }

    public Color? CellColor { get; set; }

    public Color? BorderColor { get; set; }

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
        RowHeaders = RowHeaders
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
