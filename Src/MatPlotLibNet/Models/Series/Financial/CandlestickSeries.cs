// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an OHLC candlestick series for financial chart visualization.</summary>
public sealed class CandlestickSeries : OhlcSeries, ICategoryLabeled
{
    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => DateLabels;

    /// <summary>Gets or sets the width of the candle body as a fraction of the available space.</summary>
    public double BodyWidth { get; set; } = 0.6;

    /// <summary>Creates a new candlestick series from the given OHLC data.</summary>
    public CandlestickSeries(double[] open, double[] high, double[] low, double[] close)
        : base(open, high, low, close) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(context.XAxisMin ?? 0, context.XAxisMax ?? Open.Length, Low.Min(), High.Max());

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
