// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an OHLC candlestick series for financial chart visualization.</summary>
public sealed class CandlestickSeries : OhlcSeries, ICategoryLabeled, ISeriesKind<CandlestickSeries>
{
    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => DateLabels;

    /// <summary>Width of the body of each candle as a fraction of its category slot; 1 makes neighbouring bodies touch.
    /// Default 0.6.</summary>
    public double BodyWidth { get; set => Change(ref field, value); } = 0.6;

    /// <summary>Creates a new candlestick series from the given OHLC data.</summary>
    public CandlestickSeries(double[] open, double[] high, double[] low, double[] close)
        : base(open, high, low, close) { }

    /// <inheritdoc />
    /// <remarks>From the lowest drawable low to the highest drawable high: a missing price (NaN) and an infinity are left
    /// out.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(context.XAxisMin ?? 0, context.XAxisMax ?? Open.Length, Low.DrawableRange()?.Lo, High.DrawableRange()?.Hi);

    static string ISeriesKind<CandlestickSeries>.Kind => "candlestick";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Open = Open, High = High, Low = Low, Close = Close,
        DateLabels = DateLabels, UpColor = UpColor, DownColor = DownColor,
        BodyWidth = BodyWidth
    };

    /// <summary>The reader of a <see cref="CandlestickSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries up/down colours and date labels.</summary>
    internal static CandlestickSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Candlestick(dto.Open ?? [], dto.High ?? [], dto.Low ?? [], dto.Close ?? [], dto.DateLabels);
        if (dto.UpColor is { } upColor) s.UpColor = upColor;
        if (dto.DownColor is { } downColor) s.DownColor = downColor;
        if (dto.BodyWidth is { } bodyWidth) s.BodyWidth = bodyWidth;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
