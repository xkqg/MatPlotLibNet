// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>A streaming indicator whose value depends on the last <see cref="Width"/> inputs and nothing before them: it keeps
/// those inputs and, after each new one, lets the batch indicator say what its value is over them. That is exact by construction
/// (the form says what the batch says over the same bars), costs one batch run over <see cref="Width"/> inputs for each input,
/// and never runs over the whole series. Before <see cref="Width"/> inputs have come, the batch is run over the ones that have, and
/// says NaN for an output it has no value for yet.</summary>
/// <remarks>A subclass names the width and says, in <see cref="Recompute"/>, how the batch indicator is built from a window and
/// which of its results is the output. An indicator that carries a state rather than a window derives from
/// <see cref="StreamingIndicator{TInput}"/> directly.</remarks>
/// <typeparam name="TInput">What one step reads.</typeparam>
public abstract class WindowedStreamingIndicator<TInput> : StreamingIndicator<TInput>
{
    private readonly TInput[] _ring;
    private readonly TInput[] _window;
    private int _count;
    private int _slot;

    /// <summary>How many inputs the value depends on.</summary>
    public int Width { get; }

    /// <inheritdoc />
    public override int WarmupPeriod => Width;

    /// <summary>An indicator with one output line.</summary>
    /// <param name="width">How many inputs the value depends on; at least 1.</param>
    /// <param name="capacity">How many points the line keeps.</param>
    protected WindowedStreamingIndicator(int width, int capacity = 10_000) : base(capacity)
    {
        Width = width;
        _ring = new TInput[width];
        _window = new TInput[width];
    }

    /// <summary>An indicator with several output lines; the first is the one it is named and styled by.</summary>
    /// <param name="width">How many inputs the value depends on; at least 1.</param>
    /// <param name="outputs">The lines.</param>
    protected WindowedStreamingIndicator(int width, StreamingLineSeries[] outputs) : base(outputs)
    {
        Width = width;
        _ring = new TInput[width];
        _window = new TInput[width];
    }

    /// <inheritdoc />
    protected sealed override void Step(TInput input, int index, Span<double> outputs)
    {
        _ring[_slot] = input;
        _slot = (_slot + 1) % Width;
        if (_count < Width) _count++;

        // The window in the order the inputs came: the oldest is where the next one will go once the ring is full.
        int oldest = _count < Width ? 0 : _slot;
        for (int i = 0; i < _count; i++) _window[i] = _ring[(oldest + i) % Width];

        Recompute(_window.AsSpan(0, _count), outputs);
    }

    /// <summary>Says what the batch indicator gives over a window: builds it from the inputs and writes the last value of each of
    /// its results to <paramref name="outputs"/> (NaN where it has none).</summary>
    /// <param name="window">The last inputs, oldest first; fewer than <see cref="Width"/> while the indicator is warming up.</param>
    /// <param name="outputs">One value for each output line, NaN to start with.</param>
    protected abstract void Recompute(ReadOnlySpan<TInput> window, Span<double> outputs);
}
