// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an OHLC candlestick series for financial chart visualization.</summary>
public sealed class CandlestickSeries : ChartSeries, IPriceSeries, ICategoryLabeled
{
    /// <summary>Gets the opening prices.</summary>
    public double[] Open { get; }

    /// <summary>Gets the highest prices.</summary>
    public double[] High { get; }

    /// <summary>Gets the lowest prices.</summary>
    public double[] Low { get; }

    /// <summary>Gets the closing prices.</summary>
    public double[] Close { get; }

    /// <summary>Gets or sets the optional date/category labels for the X axis.</summary>
    public string[]? DateLabels { get; set; }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => DateLabels;

    /// <summary>Gets or sets the color for up (close >= open) candles.</summary>
    public Color UpColor { get; set; } = Color.Green;

    /// <summary>Gets or sets the color for down (close &lt; open) candles.</summary>
    public Color DownColor { get; set; } = Color.Red;

    /// <summary>Gets or sets the width of the candle body as a fraction of the available space.</summary>
    public double BodyWidth { get; set; } = 0.6;

    /// <summary>Creates a new candlestick series from the given OHLC data.</summary>
    public CandlestickSeries(double[] open, double[] high, double[] low, double[] close)
    {
        Open = open;
        High = high;
        Low = low;
        Close = close;
    }

    /// <inheritdoc />
    public double[] PriceData => Close;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(context.XAxisMin ?? -0.5, context.XAxisMax ?? (Open.Length - 0.5), Low.Min(), High.Max());

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "candlestick",
        Open = Open, High = High, Low = Low, Close = Close,
        DateLabels = DateLabels, UpColor = UpColor, DownColor = DownColor,
        BodyWidth = BodyWidth
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
