// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A waffle chart: a grid of square cells, ten by ten unless said otherwise, filled by category in proportion to its
/// share of the total, so "37 of every 100" is thirty-seven squares a reader can count. Each category takes the whole number of
/// cells its share comes to, the cells left over go to the largest remainders (ties to the category that comes first), and
/// the cells fill from the bottom-left corner, row by row from the left, category by category in the order given. A category
/// that has no cell still has its entry in the legend and its row in the table.</summary>
public sealed class WaffleSeries : ChartSeries, ISeriesKind<WaffleSeries>
{
    private int _rows = RestingRows;
    private int _columns = RestingColumns;
    private double _cellGap = RestingCellGap;

    private const int RestingRows = 10;
    private const int RestingColumns = 10;
    private const double RestingCellGap = 0.2;

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, Categories.Length);

    /// <summary>The categories' names, one for each value.</summary>
    public string[] Categories { get; }

    /// <summary>The size of each category: its count, amount or share; only the proportions matter. A value that is not a
    /// finite number above zero is a category with no cells.</summary>
    public double[] Values { get; }

    /// <summary>The number of rows of cells; <see cref="RestingRows"/> unless said otherwise.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int Rows
    {
        get => _rows;
        set => Change(ref _rows, value.ThrowIfBelow(1, nameof(Rows), "The rows of cells of a waffle chart"));
    }

    /// <summary>The number of columns of cells; <see cref="RestingColumns"/> unless said otherwise.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int Columns
    {
        get => _columns;
        set => Change(ref _columns, value.ThrowIfBelow(1, nameof(Columns), "The columns of cells of a waffle chart"));
    }

    /// <summary>The space between neighbouring cells, as a share of a cell's side: 0.2 (a fifth, pywaffle's default) leaves
    /// a tenth of a side clear on every edge of every cell. 0 makes the cells touch.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0 or not below 1 (the cell would vanish).</exception>
    public double CellGap
    {
        get => _cellGap;
        set => Change(ref _cellGap, value.ThrowIfNot(value >= 0 && value < 1, nameof(CellGap), "must be 0 or more and below 1, a share of a cell's side"));
    }

    /// <summary>The colour of each category, by position; <see langword="null"/> (the default) names none. A category past the
    /// end of the array, or every category when it is <see langword="null"/>, wears what the figure's theme hands out at its
    /// own position, as the slices of a pie do.</summary>
    public Color[]? Colors { get; set => Change(ref field, value); }

    /// <summary>Initializes a waffle chart of the given categories and their sizes.</summary>
    /// <param name="categories">The categories' names.</param>
    /// <param name="values">The size of each category, one per category.</param>
    public WaffleSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <summary>How many cells each category takes: the cells (<see cref="Rows"/> times <see cref="Columns"/>) divided among
    /// the categories by the largest remainder method. All zero when no category has a size.</summary>
    /// <returns>One whole number of cells for each category.</returns>
    public int[] CellCounts() => Values.Apportion(Rows * Columns);

    /// <inheritdoc />
    /// <remarks>An entry for every category that has a name, in the colour its cells wear, including a category with no cell.</remarks>
    internal override IReadOnlyList<LegendPart> LegendPartsUnder(Theme theme) => Categories.AsLegendParts(Colors);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<WaffleSeries>.Kind => "waffle";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Categories = Categories, Values = Values,
        Colors = Colors?.ToList(),
        WaffleRows = Rows.OrNullAtRest(RestingRows),
        WaffleColumns = Columns.OrNullAtRest(RestingColumns),
        CellGap = CellGap.OrNullAtRest(RestingCellGap),
    };

    /// <summary>The reader of a <see cref="WaffleSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static WaffleSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Waffle(dto.Categories ?? [], dto.Values ?? []);
        s.Colors = dto.Colors?.ToArray();
        if (dto.WaffleRows is { } waffleRows) s.Rows = waffleRows;
        if (dto.WaffleColumns is { } waffleColumns) s.Columns = waffleColumns;
        if (dto.CellGap is { } cellGap) s.CellGap = cellGap;
        return s;
    }

    /// <inheritdoc />
    /// <remarks>One row for each category: its name, its size as given, and the number of cells it takes, which add up to the
    /// cells of the grid.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.ByCategory(Categories, new NumberColumn(Label ?? "value", Values), new NumberColumn("cells", [.. CellCounts().Select(static cells => (double)cells)]));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
