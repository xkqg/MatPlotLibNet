// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a traditional OHLC bar chart with vertical high-low lines and open/close tick marks.</summary>
public sealed class OhlcBarSeries : OhlcSeries
{
    public double TickWidth { get; set; } = 0.3;

    /// <summary>Initializes a new <see cref="OhlcBarSeries"/> with per-bar OHLC price data.</summary>
    /// <param name="open">Opening price for each bar.</param>
    /// <param name="high">High price for each bar.</param>
    /// <param name="low">Low price for each bar.</param>
    /// <param name="close">Closing price for each bar.</param>
    public OhlcBarSeries(double[] open, double[] high, double[] low, double[] close)
        : base(open, high, low, close) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double ohlcXMin = context.XAxisMin ?? -0.5;
        double ohlcXMax = context.XAxisMax ?? (Open.Length - 0.5);
        return new(ohlcXMin, ohlcXMax, Low.Min(), High.Max(), StickyXMin: ohlcXMin, StickyXMax: ohlcXMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "ohlcbar",
        Open = Open, High = High, Low = Low, Close = Close,
        DateLabels = DateLabels, UpColor = UpColor, DownColor = DownColor,
        TickWidth = TickWidth
    };

    /// <summary>Reconstructs an <see cref="OhlcBarSeries"/> from its serialization DTO, including up/down colours and tick width, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static OhlcBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.OhlcBar(dto.Open ?? [], dto.High ?? [], dto.Low ?? [], dto.Close ?? [], dto.DateLabels);
        if (dto.UpColor.HasValue)
        {
            s.UpColor = dto.UpColor.Value;
        }
        if (dto.DownColor.HasValue)
        {
            s.DownColor = dto.DownColor.Value;
        }
        if (dto.TickWidth.HasValue)
        {
            s.TickWidth = dto.TickWidth.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
