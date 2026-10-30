// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>The root of a streaming indicator whatever it is fed: an <see cref="OhlcvBar"/> (the indicators that read prices and
/// volume), a <see cref="double"/> (the ones that read one price) or a <c>double[]</c> (the ones that read one value of each of
/// several series at a time). A subclass says what one input does in <see cref="Step"/>; this class owns the rest — the count,
/// the warm-up, the output lines, and the way a bar or a price reaches an input.</summary>
/// <remarks>A streaming indicator is held to one rule: after the input at index <c>i</c> its latest value is the last value of
/// the batch indicator over the inputs <c>0..i</c>. The batch indicator is the oracle, and an indicator whose recursion or
/// window has a step of its own shares that step with its batch form so the arithmetic exists once.
/// <para>All the outputs of an input are worked out before any line is written: a step that throws leaves every line, and
/// <see cref="ProcessedCount"/>, as they were.</para></remarks>
/// <typeparam name="TInput">What one step reads.</typeparam>
public abstract class StreamingIndicator<TInput> : IStreamingIndicator
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
    public int ProcessedCount { get; private set; }

    /// <inheritdoc />
    public abstract int WarmupPeriod { get; }

    /// <inheritdoc />
    public bool IsWarmedUp => ProcessedCount >= WarmupPeriod;

    /// <inheritdoc />
    public IReadOnlyList<StreamingLineSeries> OutputSeries => _outputs.Series;

    /// <summary>An indicator with one output line.</summary>
    /// <param name="capacity">How many points the line keeps.</param>
    protected StreamingIndicator(int capacity = 10_000) : this(new StreamingOutputs(capacity)) { }

    /// <summary>An indicator with several output lines; the first is the one it is named and styled by.</summary>
    /// <param name="outputs">The lines.</param>
    protected StreamingIndicator(StreamingLineSeries[] outputs) : this(new StreamingOutputs(outputs)) { }

    private StreamingIndicator(StreamingOutputs outputs)
    {
        _outputs = outputs;
        _row = new double[outputs.Count];
    }

    /// <summary>Takes one input: works out every output of it, then adds one point to every line at the next x.</summary>
    /// <param name="input">The input.</param>
    public void Append(TInput input)
    {
        Array.Fill(_row, double.NaN);
        Step(input, ProcessedCount, _row);
        _outputs.AppendRow(ProcessedCount, _row);
        ProcessedCount++;
    }

    /// <summary>Works out the outputs of one input. Every output starts as NaN; an output a step does not set stays NaN, which is
    /// what a line shows while the indicator has no value yet.</summary>
    /// <param name="input">The input.</param>
    /// <param name="index">The place of the input, from 0.</param>
    /// <param name="outputs">One value for each output line, in the order of <see cref="OutputSeries"/>.</param>
    protected abstract void Step(TInput input, int index, Span<double> outputs);

    /// <summary>How a bar becomes an input: itself for a bar indicator, its close for a price indicator. An indicator of rows
    /// has no way to read a bar and says so.</summary>
    /// <param name="bar">The bar.</param>
    /// <exception cref="NotSupportedException">The input is neither a bar nor a price.</exception>
    protected virtual TInput FromBar(OhlcvBar bar)
    {
        if (typeof(TInput) == typeof(OhlcvBar)) return (TInput)(object)bar;
        if (typeof(TInput) == typeof(double)) return (TInput)(object)bar.Close;
        throw new NotSupportedException($"{GetType().Name} reads a {typeof(TInput).Name}, not a bar: give it the {typeof(TInput).Name} itself.");
    }

    /// <inheritdoc />
    public double GetLatest() => _outputs.Latest();

    /// <inheritdoc />
    /// <remarks>A bar without a volume: the volume is NaN.</remarks>
    public virtual void AppendCandle(OhlcBar bar) => Append(FromBar(OhlcvBar.From(bar)));

    /// <inheritdoc />
    public virtual void AppendBar(OhlcvBar bar) => Append(FromBar(bar));

    /// <summary>A price is a bar whose four prices are that price and that has no volume.</summary>
    void IStreamingIndicator.Append(double price) => Append(FromBar(new OhlcvBar(price, price, price, price)));
}
