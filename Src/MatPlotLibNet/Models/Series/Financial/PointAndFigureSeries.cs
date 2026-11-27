// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A Point and Figure chart (close only): the price drawn as columns of boxes, Xs where it rose and Os where it fell,
/// with no axis for time. Each box covers <see cref="BoxSize"/> of price; a column grows while the price keeps reaching the
/// next box in its direction and gives way to a column the other way once the price has come back <see cref="Reversal"/>
/// boxes from the last box drawn (3 is the classic). Column <c>k</c> stands in the slot from <c>k</c> to <c>k + 1</c> of the
/// X axis, and the box with index <c>j</c> covers the prices from <c>j * BoxSize</c> up to the next box.</summary>
public sealed class PointAndFigureSeries : ChartSeries, ISeriesKind<PointAndFigureSeries>
{
    /// <summary>The fraction of a box's width and height left clear between its edge and the X or O drawn in it.</summary>
    internal const double MarkInset = 0.12;

    /// <summary>The closing prices, oldest first. A price that is not a finite number is skipped.</summary>
    public double[] Prices { get; }

    /// <summary>The price one box covers.</summary>
    public double BoxSize { get; }

    /// <summary>How many boxes back from the last box drawn the price has to come to start a column the other way; <see cref="RestingReversal"/> unless
    /// said otherwise.</summary>
    public int Reversal { get; }

    /// <summary>The colour of a column of Xs, which marks a rise; green unless said otherwise.</summary>
    public Color UpColor { get; set => Change(ref field, value); } = UpDownColors.Standard.Up;

    /// <summary>The colour of a column of Os, which marks a fall; red unless said otherwise.</summary>
    public Color DownColor { get; set => Change(ref field, value); } = UpDownColors.Standard.Down;

    /// <summary>The width of the stroke an X or an O is drawn with, in pixels.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = LineWidthRule.Line.Resting;

    /// <summary>Initializes a Point and Figure chart of the given closing prices.</summary>
    /// <param name="prices">The closing prices, oldest first.</param>
    /// <param name="boxSize">The price one box covers; a finite number above zero.</param>
    /// <param name="reversal">The boxes a price has to come back to start a column the other way; 1 or more, and <see cref="RestingReversal"/> unless said otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="boxSize"/> is not a finite number above zero, or <paramref name="reversal"/> is below 1.</exception>
    public PointAndFigureSeries(double[] prices, double boxSize, int reversal = RestingReversal)
    {
        Prices = prices;
        BoxSize = boxSize.ThrowIfNotFinite(nameof(boxSize)).ThrowIfNotAbove(0, nameof(boxSize));
        Reversal = reversal.ThrowIfBelow(1, nameof(reversal));
    }

    internal const int RestingReversal = 3;
    private const double UnnamedBoxSize = 1;

    /// <summary>The columns the prices draw, left to right.</summary>
    internal IReadOnlyList<PointAndFigureColumn> Columns() => Prices.PointAndFigureColumns(BoxSize, Reversal);

    /// <inheritdoc />
    /// <remarks>From the first column to the one past the last on X; on Y from the lowest box drawn to the top of the highest.
    /// The axes begin and end on a box, with no margin the boxes would stand in.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var columns = Columns();
        if (columns.Count == 0)
        {
            return default;
        }

        int lowest = columns.Min(c => Math.Min(c.From, c.To)), highest = columns.Max(c => Math.Max(c.From, c.To));
        double yMin = lowest.LevelFrom(0, BoxSize), yMax = (highest + 1).LevelFrom(0, BoxSize);
        double xMax = CategorySlot.Of(columns.Count - 1).Right;
        return DataRangeContribution.Pinned(new(CategorySlot.Of(0).Left, xMax), new(yMin, yMax));
    }

    static string ISeriesKind<PointAndFigureSeries>.Kind => "pointandfigure";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Values = Prices, BoxSize = BoxSize, Reversal = Reversal.OrNullAtRest(RestingReversal),
        UpColor = UpColor, DownColor = DownColor,
        LineWidth = LineWidth.OrNullAtRest(LineWidthRule.Line.Resting),
    };

    /// <summary>The reader of a <see cref="PointAndFigureSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PointAndFigureSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PointAndFigure(dto.Values ?? [], dto.BoxSize ?? UnnamedBoxSize, dto.Reversal ?? RestingReversal);
        if (dto.UpColor is { } upColor) s.UpColor = upColor;

        if (dto.DownColor is { } downColor) s.DownColor = downColor;

        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        return s;
    }

    /// <inheritdoc />
    /// <remarks>One row for each column: its number from 0, <c>X</c> or <c>O</c>, the price of the box it begins at and the
    /// price of the box it ends at.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var columns = Columns();
        var rows = new IReadOnlyList<DataCell>[columns.Count];
        for (int i = 0; i < rows.Length; i++)
        {
            var column = columns[i];
            rows[i] =
            [
                DataCell.FromNumber(i), DataCell.FromText(column.Rising ? "X" : "O"),
                DataCell.FromNumber(column.From.LevelFrom(0, BoxSize)), DataCell.FromNumber(column.To.LevelFrom(0, BoxSize)),
            ];
        }

        return new ChartDataTable(null,
            [ChartDataColumn.Position(DataAxis.X, "column"), new("mark", DataColumnKind.Text), ChartDataColumn.Position(DataAxis.Y, "from"), ChartDataColumn.Position(DataAxis.Y, "to")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
