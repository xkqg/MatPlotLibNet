// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming candlestick series that accepts OHLC bars via <see cref="AppendBar"/>,
/// backed by four parallel ring buffers. Supports indicator auto-attachment via the
/// <see cref="BarAppended"/> event.</summary>
public sealed class StreamingCandlestickSeries : ChartSeries, IStreamingOhlcSeries, IHasColor
{
    private readonly DoubleRingBuffer _openBuffer;
    private readonly DoubleRingBuffer _highBuffer;
    private readonly DoubleRingBuffer _lowBuffer;
    private readonly DoubleRingBuffer _closeBuffer;
    private long _version;

    /// <summary>Up-candle (close &gt; open) body color.</summary>
    public Color? Color { get; set; }

    /// <summary>Down-candle body color.</summary>
    public Color? DownColor { get; set; }

    /// <inheritdoc />
    public long Version => Interlocked.Read(ref _version);

    /// <inheritdoc />
    public int Count => _openBuffer.Count;

    /// <inheritdoc />
    public int Capacity { get; }

    /// <inheritdoc />
    public event Action<OhlcBar>? BarAppended;

    /// <summary>Initializes a new streaming candlestick series.</summary>
    /// <param name="capacity">Maximum bars retained. Default 5,000.</param>
    public StreamingCandlestickSeries(int capacity = 5_000)
    {
        Capacity = capacity;
        _openBuffer = new DoubleRingBuffer(capacity);
        _highBuffer = new DoubleRingBuffer(capacity);
        _lowBuffer = new DoubleRingBuffer(capacity);
        _closeBuffer = new DoubleRingBuffer(capacity);
    }

    /// <inheritdoc />
    public void AppendBar(double open, double high, double low, double close)
    {
        _openBuffer.Append(open);
        _highBuffer.Append(high);
        _lowBuffer.Append(low);
        _closeBuffer.Append(close);
        Interlocked.Increment(ref _version);
        BarAppended?.Invoke(new OhlcBar(open, high, low, close));
    }

    /// <inheritdoc />
    public void AppendBar(OhlcBar bar) => AppendBar(bar.Open, bar.High, bar.Low, bar.Close);

    /// <inheritdoc />
    public void Clear()
    {
        _openBuffer.Clear();
        _highBuffer.Clear();
        _lowBuffer.Clear();
        _closeBuffer.Clear();
        Interlocked.Increment(ref _version);
    }

    /// <inheritdoc />
    public OhlcStreamingSnapshot CreateOhlcSnapshot()
    {
        return new OhlcStreamingSnapshot(
            _openBuffer.ToArray(),
            _highBuffer.ToArray(),
            _lowBuffer.ToArray(),
            _closeBuffer.ToArray(),
            Version);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        int count = Count;
        if (count == 0) return new(null, null, null, null);
        return new(0, count - 1, _lowBuffer.Min, _highBuffer.Max);
    }

    /// <inheritdoc />
    public override Serialization.SeriesDto ToSeriesDto() => new()
    {
        Type = "streaming_candlestick",
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
