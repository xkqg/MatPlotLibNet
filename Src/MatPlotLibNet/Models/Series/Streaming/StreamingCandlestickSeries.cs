// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming candlestick series that accepts OHLC bars via <see cref="AppendBar(OhlcBar)"/>,
/// backed by ONE ring of <see cref="OhlcBar"/> — a bar is the value this series is a
/// sequence of, and its four prices belong to one tick or to none.
///
/// <para>It used to be four parallel buffers. Each was thread-safe; a bar was not. An append was four separate
/// acts and a snapshot four separate reads, so a reader beside a writer could get a candle whose high came
/// from tick N and whose close came from N-1 — a bar that never traded, drawn as a body. Measured:
/// <c>o=60365, h=60382, l=60380</c> within milliseconds of starting.</para>
///
/// <para>Supports indicator auto-attachment via the <see cref="BarAppended"/> event.</para></summary>
public sealed class StreamingCandlestickSeries : ChartSeries, IStreamingOhlcSeries, IVersionedSeries, IHasColor, ISeriesKind<StreamingCandlestickSeries>
{
    private readonly StreamRing<OhlcBar> _bars;

    /// <summary>Up-candle (close &gt; open) body color.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Down-candle body color.</summary>
    public Color? DownColor { get; set => Change(ref field, value); }

    /// <inheritdoc cref="IStreamingOhlcSeries.Version" />
    public long Version => _bars.Version;

    /// <inheritdoc />
    public int Count => _bars.Count;

    /// <inheritdoc />
    public int Capacity => _bars.Capacity;

    /// <inheritdoc />
    internal override long LatestChange => ChangeClock.Latest(base.LatestChange, _bars.LatestChange);

    /// <inheritdoc />
    public event Action<OhlcBar>? BarAppended;

    /// <summary>Fired after each bar is appended, carrying it with its volume: the volume the bar came with, or NaN when it
    /// came without one. It is the event an indicator that reads the volume is attached to; the series itself keeps the
    /// prices only, so a volume reaches the indicators attached while the bar arrives and is not kept, written or drawn.</summary>
    public event Action<OhlcvBar>? OhlcvBarAppended;

    /// <summary>How many bars a streaming candlestick series keeps when it is told no capacity.</summary>
    internal const int RestingCapacity = 5_000;

    /// <summary>Initializes a new streaming candlestick series.</summary>
    /// <param name="capacity">Maximum bars retained. Default <see cref="RestingCapacity"/>.</param>
    public StreamingCandlestickSeries(int capacity = RestingCapacity) => _bars = new StreamRing<OhlcBar>(capacity);

    /// <inheritdoc />
    public void AppendBar(double open, double high, double low, double close) =>
        AppendBar(new OhlcBar(open, high, low, close));

    /// <inheritdoc />
    /// <remarks>The bar has no volume (NaN) for <see cref="OhlcvBarAppended"/>.</remarks>
    public void AppendBar(OhlcBar bar) => AppendBar(OhlcvBar.From(bar));

    /// <summary>Appends a bar with its volume. The prices are kept and go to <see cref="BarAppended"/>; the bar whole goes to
    /// <see cref="OhlcvBarAppended"/>. Every other way in ends here.</summary>
    /// <param name="bar">The bar; its volume is NaN when it has none.</param>
    public void AppendBar(OhlcvBar bar)
    {
        _bars.Append(bar.Ohlc);
        BarAppended?.Invoke(bar.Ohlc);
        OhlcvBarAppended?.Invoke(bar);
    }

    /// <inheritdoc />
    public void Clear() => _bars.Clear();

    /// <inheritdoc />
    public OhlcStreamingSnapshot CreateOhlcSnapshot()
    {
        // ONE read of ONE buffer, then split — the four arrays consumers expect, off a value nothing can
        // change any more.
        var (bars, version) = _bars.Read();
        var open = new double[bars.Length];
        var high = new double[bars.Length];
        var low = new double[bars.Length];
        var close = new double[bars.Length];
        for (int i = 0; i < bars.Length; i++)
        {
            open[i] = bars[i].Open;
            high[i] = bars[i].High;
            low[i] = bars[i].Low;
            close[i] = bars[i].Close;
        }

        return new OhlcStreamingSnapshot(open, high, low, close, version);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // The lowest low and the highest high of ONE state, found by walking: read separately they can come
        // from either side of an append, and a candle chart would be scaled to a range no bar ever occupied.
        // A missing price (NaN) and an infinity are left out of the extent. The bars are counted in the same walk, so the X
        // extent describes the same moment as the prices.
        // The lows are folded along the box's first side and the highs along its second.
        var prices = _bars.Aggregate(StreamingSeries.Box.Empty, static (box, bar) => box.Extend(bar.Low, bar.High));
        return prices.Count == 0
            ? default
            : new(0, prices.Count - 1, prices.X.OrNone()?.Lo, prices.Y.OrNone()?.Hi);
    }

    static string ISeriesKind<StreamingCandlestickSeries>.Kind => "streaming_candlestick";

    /// <inheritdoc />
    /// <remarks>Carries the ring buffer's current OHLC snapshot, one read of one buffer (<see cref="CreateOhlcSnapshot"/>),
    /// so a restored stream starts from the bars it was written with, not empty — it had no way back at all before
    /// (the roadmap row "Live series back from JSON").</remarks>
    public override SeriesDto ToSeriesDto()
    {
        var snapshot = CreateOhlcSnapshot();
        return new()
        {
            Type = this.WireKind(),
            Open = snapshot.Open,
            High = snapshot.High,
            Low = snapshot.Low,
            Close = snapshot.Close,
            Capacity = Capacity,
            Color = Color,
            DownColor = DownColor,
        };
    }

    /// <summary>The reader of a <see cref="StreamingCandlestickSeries"/> that <see cref="SeriesRegistry"/> takes; it restores the capacity and the snapshot it was written with.</summary>
    internal static StreamingCandlestickSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.AddSeries(new StreamingCandlestickSeries(dto.Capacity ?? RestingCapacity));
        var open = dto.Open ?? [];
        var high = dto.High ?? [];
        var low = dto.Low ?? [];
        var close = dto.Close ?? [];
        for (int i = 0; i < open.Length; i++)
        {
            s.AppendBar(open[i], high[i], low[i], close[i]);
        }

        if (dto.Color is { } color) s.Color = color;
        if (dto.DownColor is { } downColor) s.DownColor = downColor;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The buffer's current bars, five columns like any other OHLC series.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var snapshot = CreateOhlcSnapshot();
        return ChartDataTable.FromNumberColumns(
            [ChartDataColumn.Position(DataAxis.X), .. OhlcSeries.PriceColumns],
            [
                Enumerable.Range(0, snapshot.Close.Length).Select(i => (double)i).ToArray(),
                snapshot.Open, snapshot.High, snapshot.Low, snapshot.Close,
            ]);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
