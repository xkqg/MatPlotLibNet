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
public sealed class WaffleSeries : ChartSeries
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

    /// <summary>The number of rows of cells; 10 unless said otherwise.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int Rows
    {
        get => _rows;
        set => _rows = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(Rows), value, "A waffle chart has at least one row of cells.");
    }

    /// <summary>The number of columns of cells; 10 unless said otherwise.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int Columns
    {
        get => _columns;
        set => _columns = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(Columns), value, "A waffle chart has at least one column of cells.");
    }

    /// <summary>The space between neighbouring cells, as a share of a cell's side: 0.2 (a fifth, pywaffle's default) leaves
    /// a tenth of a side clear on every edge of every cell. 0 makes the cells touch.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0 or not below 1 (the cell would vanish).</exception>
    public double CellGap
    {
        get => _cellGap;
        set => _cellGap = value >= 0 && value < 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(CellGap), value, "The gap between cells is a share of a cell's side, 0 or more and below 1.");
    }

    /// <summary>The colour of each category, by position; <see langword="null"/> (the default) names none. A category past the
    /// end of the array, or every category when it is <see langword="null"/>, wears what the figure's theme hands out at its
    /// own position, as the slices of a pie do.</summary>
    public Color[]? Colors { get; set; }

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
    internal override IReadOnlyList<LegendPart> LegendParts =>
        [.. Categories.Select((name, position) => new LegendPart(name, LegendSwatch.Patch, position)).Where(part => !string.IsNullOrEmpty(part.Label))];

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "waffle",
        Categories = Categories, Values = Values,
        Colors = Colors?.ToList(),
        WaffleRows = Rows.OrNullAtRest(RestingRows),
        WaffleColumns = Columns.OrNullAtRest(RestingColumns),
        CellGap = CellGap.OrNullAtRest(RestingCellGap),
    };

    /// <summary>Reconstructs a <see cref="WaffleSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static WaffleSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Waffle(dto.Categories ?? [], dto.Values ?? []);
        s.Colors = dto.Colors?.ToArray();
        s.Rows = dto.WaffleRows ?? RestingRows;
        s.Columns = dto.WaffleColumns ?? RestingColumns;
        s.CellGap = dto.CellGap ?? RestingCellGap;
        return s;
    }

    /// <inheritdoc />
    /// <remarks>One row for each category: its name, its size as given, and the number of cells it takes, which add up to the
    /// cells of the grid.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var cells = CellCounts();
        int count = Math.Min(Categories.Length, Values.Length);
        var rows = new IReadOnlyList<DataCell>[count];
        for (int i = 0; i < count; i++)
        {
            rows[i] = [DataCell.FromText(Categories[i]), DataCell.FromNumber(Values[i]), DataCell.FromNumber(cells[i])];
        }

        return new ChartDataTable(null, [new("category", DataColumnKind.Text), new(Label ?? "value"), new("cells")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
