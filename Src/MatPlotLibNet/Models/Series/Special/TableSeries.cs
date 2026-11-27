// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a data table: in the plot area under the library's own themes, below the axes as matplotlib's <c>ax.table</c>
/// stands one under the matplotlib themes (<see cref="Location"/>).</summary>
public sealed class TableSeries : ChartSeries, IEdgedPatch, ISeriesKind<TableSeries>, IGivenTextSize
{
    /// <summary>Every cell is ruled, whatever the theme: the rule is an edge given to every cell, which the patch edge rule draws as wide
    /// as its theme draws a patch's edge; its colour is the one <see cref="BorderColor"/> gives, else the theme's table look's
    /// (<see cref="TableLook.BorderOver"/>, which the renderer draws).</summary>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(TableLook.Library.BorderOver(BorderColor), null);

    /// <summary>Under the library's own themes a cell is ruled one pixel wide.</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.Pixel(fill);

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>table</c>, which is never cut off at the axes.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Table;

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <inheritdoc />
    /// <remarks>A table stands outside the plot where it stands below the axes (<see cref="TableLocation.BelowAxes"/>: where its
    /// <see cref="Location"/> puts it, else where its theme's table look does).</remarks>
    internal override bool StandsOutsideThePlot(Theme theme) => theme.Looks.Table.LocationOver(Location) == TableLocation.BelowAxes;

    /// <summary>How many data columns the table has: the length of its longest row.</summary>
    internal int ColumnCount => CellData.Length == 0 ? 0 : CellData.Max(row => row.Length);

    /// <summary>The text of the data cells, as an array of rows each holding an array of cell texts. A row shorter than
    /// the longest one leaves its missing cells empty.</summary>
    public string[][] CellData { get; }

    /// <summary>Where the table stands: in the plot area from its top (<see cref="TableLocation.InPlot"/>), or below the axes as
    /// matplotlib's <c>ax.table</c> stands it (<see cref="TableLocation.BelowAxes"/>). <see langword="null"/> (the default) stands it where
    /// its theme does: in the plot under the library's own themes, below the axes under the matplotlib themes.</summary>
    public TableLocation? Location { get; set => Change(ref field, value); }

    /// <summary>The text of the header cell above each data column, by position; null draws no header row. A column
    /// past the end of the array gets no header cell.</summary>
    public string[]? ColumnHeaders { get; set => Change(ref field, value); }

    /// <summary>The text of the header cell at the start of each row, by position; null draws no row-header column.
    /// A row past the end of the array has an empty header cell.</summary>
    public string[]? RowHeaders { get; set => Change(ref field, value); }

    /// <summary>How the text of each data column is aligned in its cell, by position; <see langword="null"/> (the default), or a
    /// column past the end of the array, is aligned as its theme aligns the data: left under the library's own themes, right under the
    /// matplotlib themes (matplotlib's <c>cellLoc='right'</c>). A left aligned text starts <see cref="CellPadding"/> in from the left
    /// edge of its cell, a right aligned one ends that far from the right edge, a centred one is centred. The headers of the
    /// columns stay centred; the row headers are centred under the library's own themes and left aligned under the matplotlib themes
    /// (<c>rowLoc='left'</c>).</summary>
    public TextAlignment[]? ColumnAlignments { get; set => Change(ref field, value); }

    private double[]? _columnWidths;

    /// <summary>The relative width of each displayed column, by position. When the table has row headers the first width is that of the
    /// row-header column and the data columns follow. In the plot (<see cref="TableLocation.InPlot"/>) the displayed columns share the
    /// width of the plot in these proportions; below the axes (<see cref="TableLocation.BelowAxes"/>) the data columns share it and the
    /// row-header column, left of the plot, is as wide as its weight says against theirs. <see langword="null"/> (the default) makes every
    /// column as wide as the others, except a row-header column below the axes, which is as wide as its widest text needs (matplotlib's
    /// <c>auto_set_column_width</c>); a column past the end of the array counts for 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A width is not a finite number above zero.</exception>
    public double[]? ColumnWidths
    {
        get => _columnWidths;
        set
        {
            static string Width(int at) => string.Invariant($"ColumnWidths[{at}]");
            Func<int, string> width = Width;
            for (int i = 0; i < (value?.Length ?? 0); i++)
            {
                value![i].ThrowIfNotFinite(nameof(ColumnWidths), i, width).ThrowIfNotAbove(0, nameof(ColumnWidths), i, width);
            }

            Change(ref _columnWidths, value);
        }
    }

    /// <summary>The height of every row, header row included, in pixels. Given none, a row is as high as its theme makes one:
    /// the library's own row height under the library's own themes (the value this reads), <c>Table.FONTSIZE</c> times 1.2 points
    /// (16.67 at 100 dpi) under the matplotlib themes; a height that is given is drawn as given under every theme.</summary>
    public double CellHeight { get => TableLook.Library.CellHeightOver(GivenCellHeight, 1); set => GivenCellHeight = value; }

    /// <summary>The value of <see cref="CellHeight"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenCellHeight { get; private set => Change(ref field, value); }

    /// <summary>The space, in pixels, between a left or right aligned text and the edge of its cell; a centred text ignores it. Given none,
    /// it is the theme's: the library's own space under the library's own themes (the value this reads), a tenth of the cell's
    /// width under the matplotlib themes (matplotlib's <c>Cell.PAD</c>); a space that is given is drawn as given under every theme.</summary>
    public double CellPadding { get => TableLook.Library.PadOver(GivenCellPadding, 0); set => GivenCellPadding = value; }

    /// <summary>The value of <see cref="CellPadding"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenCellPadding { get; private set => Change(ref field, value); }

    /// <summary>The fill of the column-header and row-header cells; null draws them as the theme fills a header: light grey under the
    /// library's own themes, white under the matplotlib themes.</summary>
    public Color? HeaderColor { get; set => Change(ref field, value); }

    /// <summary>The fill of the data cells; null draws them white.</summary>
    public Color? CellColor { get; set => Change(ref field, value); }

    /// <summary>The colour of the lines around every cell; null draws them as the theme rules a table: dark grey under the library's
    /// own themes, black under the matplotlib themes.</summary>
    public Color? BorderColor { get; set => Change(ref field, value); }

    /// <summary>The size of the text in every cell, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the
    /// cells are the size of the theme's text under the matplotlib themes, as matplotlib's table cells take <c>font.size</c> (<c>TextLook.SeriesTextAtFontSize</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The cell size a new table holds under a theme that names none.</summary>
    private const double RestingFontSize = 11;

    /// <summary>The cell size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <summary>Initializes a new instance of <see cref="TableSeries"/> with the specified cell data.</summary>
    /// <param name="cellData">2D array of cell text values (rows × columns).</param>
    public TableSeries(string[][] cellData)
    {
        CellData = cellData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
        => default;

    static string ISeriesKind<TableSeries>.Kind => "table";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        TableCellData = CellData,
        ColumnHeaders = ColumnHeaders,
        RowHeaders = RowHeaders,
        CellHeight = GivenCellHeight,
        CellPadding = GivenCellPadding,
        ColumnAlignments = ColumnAlignments,
        ColumnWidths = ColumnWidths,
        HeaderColor = HeaderColor,
        FillColor = CellColor,
        EdgeColor = BorderColor,
        FontSize = GivenFontSize,
        TableLocation = Location,
    };

    /// <summary>The reader of a <see cref="TableSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static TableSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Table(dto.TableCellData ?? []);
        if (dto.ColumnHeaders is { } columnHeaders) s.ColumnHeaders = columnHeaders;
        if (dto.RowHeaders is { } rowHeaders) s.RowHeaders = rowHeaders;
        if (dto.CellHeight is { } cellHeight) s.CellHeight = cellHeight;
        if (dto.CellPadding is { } cellPadding) s.CellPadding = cellPadding;
        s.ColumnAlignments = dto.ColumnAlignments;
        s.ColumnWidths = dto.ColumnWidths;
        s.HeaderColor = dto.HeaderColor;
        s.CellColor = dto.FillColor;
        s.BorderColor = dto.EdgeColor;
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        s.Location = dto.TableLocation;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>A table series is already a table; it hands over its own cells and headers unchanged. Its cells
    /// are pre-formatted strings, so every column is text. A row-header column keeps its place at the front under
    /// an empty header - the shape an HTML table wants.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int width = Math.Max(ColumnCount, ColumnHeaders?.Length ?? 0);
        bool rowHeaders = RowHeaders is { Length: > 0 };

        var columns = new List<ChartDataColumn>(width + 1);
        if (rowHeaders)
        {
            columns.Add(new("", DataColumnKind.Text));
        }

        for (int c = 0; c < width; c++)
        {
            string header = ColumnHeaders.NameOr(c, "column ");
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
                cells.Add(DataCell.FromText(RowHeaders.ElementOr(r, "")));
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
