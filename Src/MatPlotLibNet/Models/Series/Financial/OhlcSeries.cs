// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Abstract base for OHLC financial series (candlestick and OHLC bar).
/// Stores Open/High/Low/Close arrays, up/down colors, date labels, and provides PriceData.</summary>
public abstract class OhlcSeries : ChartSeries, IPriceSeries
{
    /// <summary>The opening price of each bar.</summary>
    public double[] Open { get; }

    /// <summary>The highest price of each bar.</summary>
    public double[] High { get; }

    /// <summary>The lowest price of each bar.</summary>
    public double[] Low { get; }

    /// <summary>The closing price of each bar.</summary>
    public double[] Close { get; }

    /// <summary>An optional label for each bar, such as its date: it names the bar in the series' data table and, for
    /// candlesticks, labels the category axis. <see langword="null"/> leaves the bars numbered.</summary>
    public string[]? DateLabels { get; set => Change(ref field, value); }

    /// <summary>The colour of the bars that closed at or above their open. Default green.</summary>
    public Color UpColor { get; set => Change(ref field, value); } = UpDownColors.Standard.Up;

    /// <summary>The colour of the bars that closed below their open. Default red.</summary>
    public Color DownColor { get; set => Change(ref field, value); } = UpDownColors.Standard.Down;

    /// <inheritdoc />
    public double[] PriceData => Close;

    /// <summary>Creates a new OHLC series from the given data.</summary>
    protected OhlcSeries(double[] open, double[] high, double[] low, double[] close)
    {
        Open = open;
        High = high;
        Low = low;
        Close = close;
    }

    /// <summary>The columns of a bar's prices, each a position along Y: open, high, low, close.</summary>
    internal static IReadOnlyList<ChartDataColumn> PriceColumns { get; } =
        [ChartDataColumn.Position(DataAxis.Y, "open"), ChartDataColumn.Position(DataAxis.Y, "high"), ChartDataColumn.Position(DataAxis.Y, "low"), ChartDataColumn.Position(DataAxis.Y, "close")];

    /// <inheritdoc />
    /// <remarks>Five columns. The first is the bar's <see cref="DateLabels"/> entry when the series carries
    /// them — that is the only thing a bar knows about its own x position — and its index otherwise.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int count = Math.Min(Math.Min(Open.Length, High.Length), Math.Min(Low.Length, Close.Length));
        bool dated = DateLabels is { Length: > 0 };
        ChartDataColumn[] columns = [dated ? new("date", DataColumnKind.Text) : ChartDataColumn.Position(DataAxis.X), .. PriceColumns];

        var rows = new IReadOnlyList<DataCell>[count];
        for (int i = 0; i < count; i++)
        {
            rows[i] =
            [
                dated ? DataCell.FromText(DateLabels.ElementOr(i, "")) : DataCell.FromNumber(i),
                DataCell.FromNumber(Open[i]), DataCell.FromNumber(High[i]),
                DataCell.FromNumber(Low[i]), DataCell.FromNumber(Close[i]),
            ];
        }

        return new ChartDataTable(null, columns, rows);
    }
}
