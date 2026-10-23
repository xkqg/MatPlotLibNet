// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming candlestick series that accepts OHLC bars via <see cref="AppendBar(OhlcBar)"/>,
/// backed by ONE <see cref="RingBuffer{T}"/> of <see cref="OhlcBar"/> — a bar is the value this series is a
/// sequence of, and its four prices belong to one tick or to none.
///
/// <para>It used to be four parallel buffers. Each was thread-safe; a bar was not. An append was four separate
/// acts and a snapshot four separate reads, so a reader beside a writer could get a candle whose high came
/// from tick N and whose close came from N-1 — a bar that never traded, drawn as a body. Measured:
/// <c>o=60365, h=60382, l=60380</c> within milliseconds of starting.</para>
///
/// <para>Supports indicator auto-attachment via the <see cref="BarAppended"/> event.</para></summary>
public sealed class StreamingCandlestickSeries : ChartSeries, IStreamingOhlcSeries, IHasColor
{
    private readonly RingBuffer<OhlcBar> _bars;
    private long _version;

    /// <summary>Up-candle (close &gt; open) body color.</summary>
    public Color? Color { get; set; }

    /// <summary>Down-candle body color.</summary>
    public Color? DownColor { get; set; }

    /// <inheritdoc />
    public long Version => Interlocked.Read(ref _version);

    /// <inheritdoc />
    public int Count => _bars.Count;

    /// <inheritdoc />
    public int Capacity { get; }

    /// <inheritdoc />
    public event Action<OhlcBar>? BarAppended;

    /// <summary>Initializes a new streaming candlestick series.</summary>
    /// <param name="capacity">Maximum bars retained. Default 5,000.</param>
    public StreamingCandlestickSeries(int capacity = 5_000)
    {
        Capacity = capacity;
        _bars = new RingBuffer<OhlcBar>(capacity);
    }

    /// <inheritdoc />
    public void AppendBar(double open, double high, double low, double close) =>
        AppendBar(new OhlcBar(open, high, low, close));

    /// <inheritdoc />
    public void AppendBar(OhlcBar bar)
    {
        _bars.Append(bar);
        Interlocked.Increment(ref _version);
        BarAppended?.Invoke(bar);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _bars.Clear();
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public OhlcStreamingSnapshot CreateOhlcSnapshot()
    {
        // ONE read of ONE buffer, then split — the four arrays consumers expect, off a value nothing can
        // change any more.
        long version = Version;
        var bars = _bars.ToArray();
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
        // A missing price (NaN) and an infinity are left out of the extent.
        int count = 0;
        var extent = _bars.Aggregate(StreamingSeries.Box.Empty, (box, bar) => { count++; return box.Extend(bar.Low, bar.High); });
        return count == 0
            ? new(null, null, null, null)
            : new(0, count - 1, extent.X.OrNone()?.Lo, extent.Y.OrNone()?.Hi);
    }

    /// <inheritdoc />
    /// <remarks>Carries the ring buffer's current OHLC snapshot, one read of one buffer (<see cref="CreateOhlcSnapshot"/>),
    /// so a restored stream starts from the bars it was written with, not empty — it had no way back at all before
    /// (the roadmap row "Live series back from JSON").</remarks>
    public override SeriesDto ToSeriesDto()
    {
        var snapshot = CreateOhlcSnapshot();
        return new()
        {
            Type = "streaming_candlestick",
            Open = snapshot.Open,
            High = snapshot.High,
            Low = snapshot.Low,
            Close = snapshot.Close,
            Capacity = Capacity,
            Color = Color,
            DownColor = DownColor,
        };
    }

    /// <summary>Reconstructs a <see cref="StreamingCandlestickSeries"/> at the capacity and the snapshot it was
    /// written with, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StreamingCandlestickSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.AddSeries(new StreamingCandlestickSeries(dto.Capacity ?? 5_000));
        var open = dto.Open ?? [];
        var high = dto.High ?? [];
        var low = dto.Low ?? [];
        var close = dto.Close ?? [];
        for (int i = 0; i < open.Length; i++)
        {
            s.AppendBar(open[i], high[i], low[i], close[i]);
        }

        if (dto.Color.HasValue) s.Color = dto.Color.Value;
        if (dto.DownColor.HasValue) s.DownColor = dto.DownColor.Value;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The buffer's current bars, five columns like any other OHLC series.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var snapshot = CreateOhlcSnapshot();
        return ChartDataTable.FromNumberColumns(
            [new("x", Axis: DataAxis.X), new("open", Axis: DataAxis.Y), new("high", Axis: DataAxis.Y), new("low", Axis: DataAxis.Y), new("close", Axis: DataAxis.Y)],
            [
                Enumerable.Range(0, snapshot.Close.Length).Select(i => (double)i).ToArray(),
                snapshot.Open, snapshot.High, snapshot.Low, snapshot.Close,
            ]);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
