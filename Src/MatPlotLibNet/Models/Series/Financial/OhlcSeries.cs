// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Abstract base for OHLC financial series (candlestick and OHLC bar).
/// Stores Open/High/Low/Close arrays, up/down colors, date labels, and provides PriceData.</summary>
public abstract class OhlcSeries : ChartSeries, IPriceSeries
{
    public double[] Open { get; }

    public double[] High { get; }

    public double[] Low { get; }

    public double[] Close { get; }

    public string[]? DateLabels { get; set; }

    public Color UpColor { get; set; } = Colors.Green;

    public Color DownColor { get; set; } = Colors.Red;

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

    /// <inheritdoc />
    /// <remarks>Five columns. The first is the bar's <see cref="DateLabels"/> entry when the series carries
    /// them — that is the only thing a bar knows about its own x position — and its index otherwise.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int count = Math.Min(Math.Min(Open.Length, High.Length), Math.Min(Low.Length, Close.Length));
        bool dated = DateLabels is { Length: > 0 };
        var columns = new ChartDataColumn[]
        {
            dated ? new("date", DataColumnKind.Text) : new("x", Axis: DataAxis.X),
            new("open", Axis: DataAxis.Y), new("high", Axis: DataAxis.Y), new("low", Axis: DataAxis.Y), new("close", Axis: DataAxis.Y),
        };

        var rows = new IReadOnlyList<DataCell>[count];
        for (int i = 0; i < count; i++)
        {
            rows[i] =
            [
                dated ? DataCell.FromText(i < DateLabels!.Length ? DateLabels[i] : "") : DataCell.FromNumber(i),
                DataCell.FromNumber(Open[i]), DataCell.FromNumber(High[i]),
                DataCell.FromNumber(Low[i]), DataCell.FromNumber(Close[i]),
            ];
        }

        return new ChartDataTable(null, columns, rows);
    }
}
