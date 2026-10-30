// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Abstract base for streaming indicators that are fed a price. Manages output series, warmup, and the
/// candlestick subscription pattern. Subclasses implement <see cref="ComputeNext"/> for O(1) incremental logic. An indicator
/// written today derives from <see cref="StreamingIndicator{TInput}"/>, which also reads a bar with its volume or a row of
/// series.</summary>
public abstract class StreamingIndicator : IStreamingIndicator
{
    private readonly StreamingOutputs _outputs;
    private readonly double[] _row;

    /// <summary>Display label for the indicator line.</summary>
    public string? Label { get => _outputs.Label; set => _outputs.Label = value; }

    /// <summary>Line color.</summary>
    public Color? Color { get => _outputs.Color; set => _outputs.Color = value; }

    /// <summary>Line width in pixels.</summary>
    public double LineWidth { get => _outputs.LineWidth; set => _outputs.LineWidth = value; }

    /// <inheritdoc />
    public int ProcessedCount { get; protected set; }

    /// <inheritdoc />
    public abstract int WarmupPeriod { get; }

    /// <inheritdoc />
    public bool IsWarmedUp => ProcessedCount >= WarmupPeriod;

    /// <inheritdoc />
    public IReadOnlyList<StreamingLineSeries> OutputSeries => _outputs.Series;

    /// <summary>Initializes the indicator with a shared output series capacity.</summary>
    protected StreamingIndicator(int capacity = 10_000) : this(new StreamingOutputs(capacity)) { }

    /// <summary>Initializes with multiple output series (for bands/MACD).</summary>
    protected StreamingIndicator(StreamingLineSeries[] outputs) : this(new StreamingOutputs(outputs)) { }

    private StreamingIndicator(StreamingOutputs outputs)
    {
        _outputs = outputs;
        _row = new double[outputs.Count];
    }

    /// <inheritdoc />
    public void Append(double price)
    {
        ProcessedCount++;
        Array.Fill(_row, double.NaN);
        _row[0] = ComputeNext(price);
        _outputs.AppendRow(ProcessedCount - 1, _row);
    }

    /// <inheritdoc />
    public virtual void AppendCandle(OhlcBar bar) => Append(bar.Close);

    /// <inheritdoc />
    /// <remarks>An indicator that reads the volume overrides this; every other one takes the candle.</remarks>
    public virtual void AppendBar(OhlcvBar bar) => AppendCandle(bar.Ohlc);

    /// <inheritdoc />
    public double GetLatest() => _outputs.Latest();

    /// <summary>Compute the next indicator value from the incoming price. O(1) per call.
    /// Return <see cref="double.NaN"/> during warmup.</summary>
    protected abstract double ComputeNext(double price);

    /// <summary>Sets an output other than the first for the input being computed, from inside <see cref="ComputeNext"/>. Every
    /// output is written together once <see cref="ComputeNext"/> returns, so no line is a point ahead of another. An output that
    /// is not set is NaN for that input.</summary>
    /// <param name="index">The output, from 0 (the first output is the value <see cref="ComputeNext"/> returns).</param>
    /// <param name="value">Its value for this input.</param>
    protected void SetOutput(int index, double value) => _row[index] = value;

    /// <summary>Appends a value to a specific output series (for multi-output indicators).</summary>
    [Obsolete("Use SetOutput, which writes every output of an input together once ComputeNext returns.")]
    protected void AppendToOutput(int index, double x, double value)
    {
        OutputSeries[index].AppendPoint(x, value);
    }
}
