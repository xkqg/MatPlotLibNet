// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a data table rendered inside the plot area.</summary>
public sealed class TableSeries : ChartSeries
{
    /// <summary>Gets the 2D array of cell text values (rows × columns).</summary>
    public string[][] CellData { get; }

    /// <summary>Gets or sets optional column header labels.</summary>
    public string[]? ColumnHeaders { get; set; }

    /// <summary>Gets or sets optional row header labels.</summary>
    public string[]? RowHeaders { get; set; }

    /// <summary>Gets or sets the height of each cell in pixels.</summary>
    public double CellHeight { get; set; } = 25;

    /// <summary>Gets or sets the horizontal padding inside each cell in pixels.</summary>
    public double CellPadding { get; set; } = 4;

    /// <summary>Gets or sets the background color of header cells. If <see langword="null"/>, a default gray is used.</summary>
    public Color? HeaderColor { get; set; }

    /// <summary>Gets or sets the background color of data cells. If <see langword="null"/>, white is used.</summary>
    public Color? CellColor { get; set; }

    /// <summary>Gets or sets the border color. If <see langword="null"/>, a default dark gray is used.</summary>
    public Color? BorderColor { get; set; }

    /// <summary>Gets or sets the font size in points.</summary>
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

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
