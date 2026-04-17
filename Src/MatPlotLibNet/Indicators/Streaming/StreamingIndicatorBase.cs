// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Abstract base for streaming indicators. Manages output series, warmup, and the
/// candlestick subscription pattern. Subclasses implement <see cref="ComputeNext"/> for O(1) incremental logic.</summary>
public abstract class StreamingIndicatorBase : IStreamingIndicator
{
    private readonly StreamingLineSeries _output;
    private double _xCounter;

    /// <summary>Display label for the indicator line.</summary>
    public string? Label { get => _output.Label; set => _output.Label = value; }

    /// <summary>Line color.</summary>
    public Color? Color { get => _output.Color; set => _output.Color = value; }

    /// <summary>Line width in pixels.</summary>
    public double LineWidth { get => _output.LineWidth; set => _output.LineWidth = value; }

    /// <inheritdoc />
    public int ProcessedCount { get; protected set; }

    /// <inheritdoc />
    public abstract int WarmupPeriod { get; }

    /// <inheritdoc />
    public bool IsWarmedUp => ProcessedCount >= WarmupPeriod;

    /// <inheritdoc />
    public IReadOnlyList<StreamingLineSeries> OutputSeries { get; }

    /// <summary>Initializes the indicator with a shared output series capacity.</summary>
    protected StreamingIndicatorBase(int capacity = 10_000)
    {
        _output = new StreamingLineSeries(capacity);
        OutputSeries = [_output];
    }

    /// <summary>Initializes with multiple output series (for bands/MACD).</summary>
    protected StreamingIndicatorBase(StreamingLineSeries[] outputs)
    {
        _output = outputs[0];
        OutputSeries = outputs;
    }

    /// <inheritdoc />
    public void Append(double price)
    {
        ProcessedCount++;
        double value = ComputeNext(price);
        _output.AppendPoint(_xCounter, value);
        _xCounter++;
    }

    /// <inheritdoc />
    public virtual void AppendCandle(OhlcBar bar) => Append(bar.Close);

    /// <inheritdoc />
    public double GetLatest() => ProcessedCount > 0 && _output.Count > 0
        ? _output.CreateSnapshot().YData[^1]
        : double.NaN;

    /// <summary>Compute the next indicator value from the incoming price. O(1) per call.
    /// Return <see cref="double.NaN"/> during warmup.</summary>
    protected abstract double ComputeNext(double price);

    /// <summary>Appends a value to a specific output series (for multi-output indicators).</summary>
    protected void AppendToOutput(int index, double x, double value)
    {
        ((StreamingLineSeries)OutputSeries[index]).AppendPoint(x, value);
    }
}
