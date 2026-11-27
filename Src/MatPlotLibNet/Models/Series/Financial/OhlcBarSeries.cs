// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a traditional OHLC bar chart with vertical high-low lines and open/close tick marks.</summary>
public sealed class OhlcBarSeries : OhlcSeries, ISeriesKind<OhlcBarSeries>, IWidthGiven
{
    /// <inheritdoc />
    /// <remarks>2: an OHLC bar is three line segments, a <c>LineCollection</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <summary>How far the open tick reaches to the left and the close tick to the right of each bar, in units of 20
    /// pixels. Default 0.3, which is 6 pixels.</summary>
    public double TickWidth { get; set => Change(ref field, value); } = 0.3;

    /// <summary>The width of the bar's lines (the high-low line and the two ticks), in pixels, read and drawn as a line's
    /// (<see cref="LineSeries.LineWidth"/>), as the lines of a <c>vlines</c> and an <c>hlines</c> take <c>lines.linewidth</c>.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>How wide the bar's lines are drawn: as a collection of lines (a <c>vlines</c> and an <c>hlines</c>, which take
    /// <c>lines.linewidth</c> and never the cycler's), a line's width at rest.</summary>
    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.LineCollection;

    /// <summary>The width the series was given; <see langword="null"/> when it was given none, and its rule decides.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

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
        // From the lowest drawable low to the highest drawable high: a missing price (NaN) and an infinity are left out.
        return new(ohlcXMin, ohlcXMax, Low.DrawableRange()?.Lo, High.DrawableRange()?.Hi, StickyXMin: ohlcXMin, StickyXMax: ohlcXMax);
    }

    static string ISeriesKind<OhlcBarSeries>.Kind => "ohlcbar";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Open = Open, High = High, Low = Low, Close = Close,
        DateLabels = DateLabels, UpColor = UpColor, DownColor = DownColor,
        TickWidth = TickWidth,
        LineWidth = GivenLineWidth,
    };

    /// <summary>The reader of a <see cref="OhlcBarSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries up/down colours, tick width and the width given to its lines.</summary>
    internal static OhlcBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.OhlcBar(dto.Open ?? [], dto.High ?? [], dto.Low ?? [], dto.Close ?? [], dto.DateLabels);
        if (dto.UpColor is { } upColor) s.UpColor = upColor;
        if (dto.DownColor is { } downColor) s.DownColor = downColor;
        if (dto.TickWidth is { } tickWidth) s.TickWidth = tickWidth;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
