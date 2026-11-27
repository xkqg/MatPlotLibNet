// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>One row of a <see cref="TreeGridSeries"/>: a name at some depth, its numbers, and where it leads.</summary>
/// <param name="Label">The row's name, drawn left and indented by <see cref="Depth"/>.</param>
/// <param name="Cells">The row's values, one per column, drawn RIGHT-aligned so digits line up.</param>
public readonly record struct TreeGridRow(string Label, IReadOnlyList<string> Cells)
{
    /// <summary>How deep the row sits: 0 is the root. Each level is one indent step, and the row reports it as
    /// <c>aria-level</c> so the grid is navigable rather than merely visible.</summary>
    public int Depth { get; init; }

    /// <summary>Where the row leads — an SVG <c>&lt;a href&gt;</c> around it, so expanding a subtree is a URL and
    /// needs no script. Null makes the row plain text.</summary>
    public string? Url { get; init; }

    /// <summary>Whether the subtree under this row is shown: the chevron points ▾ when true, ▸ when false, and
    /// the row reports <c>aria-expanded</c>. Null = a leaf, which discloses nothing.</summary>
    public bool? Expanded { get; init; }

    /// <summary>An accent for the row's ink — colour ONLY on deviation, as everywhere else on a wall.</summary>
    public Color? Accent { get; init; }
}

/// <summary>
/// A TREE GRID: indented rows with right-aligned numeric columns — htop's process tree, and the shape the ARIA
/// spec actually names (<c>treegrid</c>; a treemap has no role at all).
///
/// <para><b>Why beside the treemap and not instead of it.</b> A treemap answers "who is big" pre-attentively and
/// stops there: NN/g puts its useful depth at two or three levels and says a rectangle too small for its label
/// must fall back to a tooltip. Measured on one running system: two lanes of twenty-three carried every
/// message in an hour — as area that is two rectangles and twenty-one slivers, while as rows it is a column of
/// numbers you can compare exactly. Composition in the map, comparison in the grid.</para>
///
/// <para>The grid carries no data of its own and contributes no axes range: it fills the region it is given.</para>
/// </summary>
public sealed class TreeGridSeries : ChartSeries, ISeriesKind<TreeGridSeries>, IGivenTextSize
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The rows, in the order they are drawn — the caller owns the tree's flattening, because only the
    /// caller knows which subtrees are open.</summary>
    public IReadOnlyList<TreeGridRow> Rows { get; }

    /// <summary>The column titles over <see cref="TreeGridRow.Cells"/>, or null for a headerless grid.</summary>
    public string[]? ColumnHeaders { get; set => Change(ref field, value); }

    private const double RestingRowHeight = 22;
    /// <summary>The height of one row.</summary>
    public double RowHeight { get; set => Change(ref field, value); } = RestingRowHeight;

    private const double RestingIndentWidth = 18;
    /// <summary>How far one level of depth indents.</summary>
    public double IndentWidth { get; set => Change(ref field, value); } = RestingIndentWidth;

    private const double RestingColumnWidth = 120;
    /// <summary>The width of one value column; the name column takes whatever is left.</summary>
    public double ColumnWidth { get; set => Change(ref field, value); } = RestingColumnWidth;

    /// <summary>The size of the row text, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the rows are
    /// the size of the theme's text under the matplotlib themes (<c>TextLook.SeriesTextAtFontSize</c>: a chart's text is one
    /// size there) and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The row size a new grid holds under a theme that names none.</summary>
    private const double RestingFontSize = 12;

    /// <summary>The row size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <summary>Creates a tree grid over <paramref name="rows"/>.</summary>
    public TreeGridSeries(IReadOnlyList<TreeGridRow> rows) => Rows = rows;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<TreeGridSeries>.Kind => "treegrid";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        ColumnHeaders = ColumnHeaders,
        RowHeight = RowHeight.OrNullAtRest(RestingRowHeight),
        IndentWidth = IndentWidth.OrNullAtRest(RestingIndentWidth),
        ColumnWidth = ColumnWidth.OrNullAtRest(RestingColumnWidth),
        FontSize = GivenFontSize,
        TreeGridRows = [.. Rows.Select(r => new TreeGridRowDto
        {
            Label = r.Label,
            Cells = [.. r.Cells],
            Depth = r.Depth,
            Url = r.Url,
            Expanded = r.Expanded,
            Accent = r.Accent,
        })],
    };

    /// <summary>The reader of a <see cref="TreeGridSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static TreeGridSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var rows = (dto.TreeGridRows ?? []).Select(r => new TreeGridRow(r.Label, r.Cells ?? [])
        {
            Depth = r.Depth,
            Url = r.Url,
            Expanded = r.Expanded,
            Accent = r.Accent,
        }).ToList();
        var s = axes.TreeGrid(rows);
        s.ColumnHeaders = dto.ColumnHeaders;
        if (dto.RowHeight is { } rowHeight) s.RowHeight = rowHeight;
        if (dto.IndentWidth is { } indentWidth) s.IndentWidth = indentWidth;
        if (dto.ColumnWidth is { } columnWidth) s.ColumnWidth = columnWidth;
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One row per tree row, its depth kept as a column: an indent is a picture of a hierarchy, a depth
    /// number is the hierarchy itself.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int width = Rows.Count == 0 ? 0 : Rows.Max(row => row.Cells.Count);
        width = Math.Max(width, ColumnHeaders?.Length ?? 0);

        var columns = new List<ChartDataColumn> { new("label", DataColumnKind.Text), new("depth") };
        for (int c = 0; c < width; c++)
        {
            string header = ColumnHeaders.NameOr(c, "column ");
            columns.Add(new(header, DataColumnKind.Text));
        }

        var rows = new List<IReadOnlyList<DataCell>>(Rows.Count);
        foreach (var row in Rows)
        {
            var cells = new List<DataCell>(columns.Count)
            {
                DataCell.FromText(row.Label),
                DataCell.FromNumber(row.Depth),
            };
            for (int c = 0; c < width; c++)
            {
                cells.Add(DataCell.FromText(row.Cells.ElementOr(c, "")));
            }

            rows.Add(cells);
        }

        return new ChartDataTable(null, columns, rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
