// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a heatmap series that renders a 2D data matrix as colored cells.</summary>
public sealed class HeatmapSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable, ILabelable,
    ITableNamedByAxes
{
    public double[,] Data { get; }

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc cref="ILabelable.ShowLabels"/>
    public bool ShowLabels { get; set; }

    /// <inheritdoc cref="ILabelable.LabelFormat"/>
    /// <remarks>Defaults to <c>"F2"</c> (two decimal places) when <see langword="null"/>.</remarks>
    public string? LabelFormat { get; set; }

    /// <summary>Mask mode that hides redundant cells in symmetric matrices (e.g. correlation matrices).</summary>
    public HeatmapMaskMode MaskMode { get; set; } = HeatmapMaskMode.None;

    /// <summary>Optional explicit cell-annotation colour. When null the renderer auto-picks black or white per cell to maximise contrast against the fill.</summary>
    public Color? CellValueColor { get; set; }

    /// <summary>The date of the cell in row 0, column 0, when the grid is a calendar: the days of a week run down a column and
    /// a week is a column, so the cell in row <c>r</c> and column <c>c</c> is the day <c>7 · c + r</c> days after it. A calendar
    /// heatmap sets it (<c>CalendarHeatmap</c>); the data table then gives each cell its date. <see langword="null"/> (the
    /// default) for a grid that is no calendar, whose table has no date column.</summary>
    public DateTime? FirstCellDate { get; set; }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() => Data.ScanColorBarRange();


    /// <summary>Initializes a new instance of <see cref="HeatmapSeries"/> with the specified 2D data.</summary>
    /// <param name="data">The two-dimensional data matrix to render.</param>
    public HeatmapSeries(double[,] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    /// <remarks>Heatmaps fill their plot rectangle exactly (cells of <c>cols Ã— rows</c>).
    /// Report the grid extent with sticky edges on all four sides so the axes show
    /// meaningful row/column indices instead of the default <c>[0, 1]</c> fallback, and
    /// so the 5 % axis margin never introduces whitespace between the cells and the spines.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        int rows = Data.GetLength(0);
        int cols = Data.GetLength(1);
        if (rows == 0 || cols == 0) return new(null, null, null, null);
        return new(0, cols, 0, rows,
            StickyXMin: 0, StickyXMax: cols, StickyYMin: 0, StickyYMax: rows);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "heatmap",
        HeatmapData = Data.To2DList(),
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        ShowLabels = ShowLabels ? true : null,
        LabelFormat = LabelFormat,
        MaskMode = MaskMode != HeatmapMaskMode.None ? MaskMode.ToString() : null,
        CellValueColor = CellValueColor,
        FirstCellDate = FirstCellDate,
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs a <see cref="HeatmapSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static HeatmapSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var hs = axes.Heatmap(dto.HeatmapData.From2DList());
        hs.RestoreColorMap(dto);
        if (dto.ShowLabels.HasValue)
        {
            hs.ShowLabels = dto.ShowLabels.Value;
        }
        if (dto.LabelFormat is not null)
        {
            hs.LabelFormat = dto.LabelFormat;
        }
        if (dto.MaskMode is not null && Enum.TryParse<Models.Series.HeatmapMaskMode>(dto.MaskMode, true, out var mm))
        {
            hs.MaskMode = mm;
        }
        if (dto.CellValueColor.HasValue)
        {
            hs.CellValueColor = dto.CellValueColor.Value;
        }
        hs.FirstCellDate = dto.FirstCellDate;
        hs.RestoreNormalizer(dto);
        return hs;
    }


    /// <summary>The stretch of the Y axis row <paramref name="row"/> covers: row 0 at the top of an axis that counts upward,
    /// so row <c>r</c> of <c>n</c> runs from <c>n - r - 1</c> to <c>n - r</c>. The renderer draws the row there and the
    /// data table reads the name the Y axis shows at its centre.</summary>
    /// <param name="row">The row, from 0 (the top).</param>
    /// <returns>The row's slot along the Y axis.</returns>
    internal CategorySlot RowSlot(int row) => CategorySlot.Of(Data.GetLength(0) - 1 - row);

    /// <summary>The stretch of the X axis column <paramref name="column"/> covers: from <c>column</c> to
    /// <c>column + 1</c>, the first column at the left.</summary>
    /// <param name="column">The column, from 0.</param>
    /// <returns>The column's slot along the X axis.</returns>
    internal static CategorySlot ColumnSlot(int column) => CategorySlot.Of(column);

    /// <inheritdoc />
    /// <remarks>Long form: one row per cell. A wide grid would make the column headers indices, which say
    /// nothing a reader can use; row and column as numbers are readable at any size. A calendar
    /// (<see cref="FirstCellDate"/>) adds the date of each cell.</remarks>
    public override ChartDataTable? ToDataTable() => Table(null, null);

    /// <inheritdoc />
    /// <remarks>A row or column the axis names at its centre (a calendar's weekdays, the names of a correlation matrix)
    /// is written as that name; one the axis does not name in full (a calendar's weeks, of which only the columns that
    /// hold a month's 1st are named) keeps its number.</remarks>
    ChartDataTable? ITableNamedByAxes.ToDataTable(AxisNames names) => Table(
        names.Along(DataAxis.Y, [.. Enumerable.Range(0, Data.GetLength(0)).Select(r => RowSlot(r).Centre)]),
        names.Along(DataAxis.X, [.. Enumerable.Range(0, Data.GetLength(1)).Select(c => ColumnSlot(c).Centre)]));

    /// <summary>The long-form table, with each row and column written as its name when <paramref name="rowNames"/> or
    /// <paramref name="columnNames"/> gives the names, as its number otherwise.</summary>
    private ChartDataTable Table(string?[]? rowNames, string?[]? columnNames)
    {
        int rows = Data.GetLength(0), cols = Data.GetLength(1);
        bool dated = FirstCellDate is not null;
        var out_ = new List<IReadOnlyList<DataCell>>(rows * cols);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cells = new List<DataCell>(4) { Place(r, rowNames), Place(c, columnNames) };
                if (dated) cells.Add(DateOf(r, c));
                cells.Add(DataCell.FromNumber(Data[r, c]));
                out_.Add(cells);
            }
        }

        var columns = new List<ChartDataColumn>
        {
            new("row", rowNames is null ? DataColumnKind.Number : DataColumnKind.Text),
            new("column", columnNames is null ? DataColumnKind.Number : DataColumnKind.Text),
        };
        if (dated) columns.Add(new("date", DataColumnKind.Date));
        columns.Add(new(Label ?? "value"));
        return new ChartDataTable(null, columns, out_);
    }

    /// <summary>The first day an OLE Automation date can name; the table spells a date as one, and a day before it is not named.</summary>
    private static readonly long FirstNameableDay = new DateTime(100, 1, 1).Ticks;

    /// <summary>The date of the cell in row <paramref name="row"/> and column <paramref name="column"/> of a calendar: the
    /// first cell's plus 7 days for each column and one for each row. A day the table cannot spell (before the year 100, or
    /// past the last day a <see cref="DateTime"/> holds, as the padding of the last week of the calendar can be) is an
    /// empty cell.</summary>
    private DataCell DateOf(int row, int column)
    {
        long ticks = FirstCellDate!.Value.Date.Ticks + (7L * column + row) * TimeSpan.TicksPerDay;
        return ticks >= FirstNameableDay && ticks <= DateTime.MaxValue.Ticks
            ? DataCell.FromNumber(new DateTime(ticks).ToOADate())
            : DataCell.Empty;
    }

    /// <summary>A row or column: its name when the axis names it (a slot centre always can be placed, so a name is
    /// there), its number otherwise.</summary>
    private static DataCell Place(int index, string?[]? names) =>
        names is null ? DataCell.FromNumber(index) : DataCell.FromText(names[index]!);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
