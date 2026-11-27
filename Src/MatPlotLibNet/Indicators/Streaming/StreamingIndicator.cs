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
public abstract class StreamingIndicator<TInput> : IStreamingIndicator, IPinsItsPane
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

    /// <summary>What the batch indicator's <c>Spec</c> factory says of the parameters: the labels and the warm-up.</summary>
    private protected IndicatorSpec Spec { get; }

    /// <inheritdoc />
    public bool IsWarmedUp => ProcessedCount >= WarmupPeriod;

    /// <inheritdoc />
    public IReadOnlyList<StreamingLineSeries> OutputSeries => _outputs.Series;

    /// <summary>The range the indicator's values live in, which the pane it is attached to is pinned to, as its batch indicator pins
    /// its own; <see langword="null"/> (the default) for one whose values have no fixed range.</summary>
    internal virtual PaneRange? PinnedRange => null;

    PaneRange? IPinsItsPane.PinnedRange => PinnedRange;

    /// <summary>An indicator with one output line.</summary>
    /// <param name="capacity">How many points the line keeps.</param>
    protected StreamingIndicator(int capacity = StreamingSeries.RestingCapacity) : this(new StreamingOutputs(capacity)) { }

    /// <summary>An indicator with several output lines; the first is the one it is named and styled by.</summary>
    /// <param name="outputs">The lines.</param>
    protected StreamingIndicator(StreamingLineSeries[] outputs) : this(new StreamingOutputs(outputs)) { }

    /// <summary>An indicator of the library: one output line for each line its batch indicator's spec names, labelled by it.</summary>
    /// <param name="spec">What the batch indicator's <c>Spec</c> factory says of the parameters.</param>
    /// <param name="capacity">How many points each line keeps.</param>
    private protected StreamingIndicator(IndicatorSpec spec, int capacity) : this(new StreamingOutputs(spec.Lines, capacity)) => Spec = spec;

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

    /// <summary>The output lines of a streaming indicator and what every streaming indicator does with them: one point per input on
    /// every line at the same x, the style of the first line as the style of the indicator, and the latest value of the first line.
    /// Both streaming bases hold one, so the way outputs are written is stated once.</summary>
    private sealed class StreamingOutputs
    {
        private readonly StreamingLineSeries[] _series;

        /// <summary>Takes the given lines; the first is the line the indicator is named and styled by.</summary>
        internal StreamingOutputs(StreamingLineSeries[] series) => _series = series;

        /// <summary>One line of the given capacity.</summary>
        internal StreamingOutputs(int capacity) : this([new StreamingLineSeries(capacity)]) { }

        /// <summary>One line of the given capacity for each label, labelled by it: the lines of an indicator, as its
        /// <see cref="IndicatorSpec"/> names them.</summary>
        internal StreamingOutputs(ReadOnlySpan<string> labels, int capacity) : this(Labelled(labels, capacity)) { }

        private static StreamingLineSeries[] Labelled(ReadOnlySpan<string> labels, int capacity)
        {
            var series = new StreamingLineSeries[labels.Length];
            for (int k = 0; k < series.Length; k++) series[k] = new StreamingLineSeries(capacity) { Label = labels[k] };
            return series;
        }

        /// <summary>The lines, in the order the indicator names them.</summary>
        internal IReadOnlyList<StreamingLineSeries> Series => _series;

        /// <summary>How many lines.</summary>
        internal int Count => _series.Length;

        /// <summary>The label of the first line.</summary>
        internal string? Label { get => _series[0].Label; set => _series[0].Label = value; }

        /// <summary>The color of the first line.</summary>
        internal Color? Color { get => _series[0].Color; set => _series[0].Color = value; }

        /// <summary>The width of the first line.</summary>
        internal double LineWidth { get => _series[0].LineWidth; set => _series[0].LineWidth = value; }

        /// <summary>Adds the values of one input, the first to the first line and so on, all at <paramref name="x"/>. The row is
        /// complete before the first line is touched, so no line is ever a point ahead of another.</summary>
        internal void AppendRow(double x, ReadOnlySpan<double> row)
        {
            for (int k = 0; k < _series.Length; k++) _series[k].AppendPoint(x, row[k]);
        }

        /// <summary>The latest value of the first line, or NaN when it has none.</summary>
        internal double Latest()
        {
            var values = _series[0].CreateSnapshot().YData;
            return values.Length > 0 ? values[^1] : double.NaN;
        }
    }
}
