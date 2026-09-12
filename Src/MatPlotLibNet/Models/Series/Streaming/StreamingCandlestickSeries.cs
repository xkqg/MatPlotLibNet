// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
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
        var extent = _bars.Aggregate(StreamingSeries.Box.Empty, static (box, bar) => box.Extend(bar.Low, bar.High));
        return extent.IsEmpty
            ? new(null, null, null, null)
            : new(0, Count - 1, extent.XMin, extent.YMax);
    }

    /// <inheritdoc />
    public override Serialization.SeriesDto ToSeriesDto() => new()
    {
        Type = "streaming_candlestick",
        Label = Label
    };


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
