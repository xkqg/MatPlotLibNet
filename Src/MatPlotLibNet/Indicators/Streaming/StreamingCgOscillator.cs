// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming CgOscillator: the centre-of-gravity oscillator over the last <c>period</c> prices. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="CgOscillator"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingCgOscillator : WindowedStreamingIndicator<double>
{
    private readonly int _period;

    /// <summary>Creates a streaming CgOscillator.</summary>
    /// <param name="period">How many prices the centre of gravity is taken over; at least 2.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingCgOscillator(int period = 10, int capacity = 10_000)
        : base(period, capacity)
    {
        _period = period;
        Label = $"CG({period})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new CgOscillator(window.ToArray(), _period).Compute().Values.LastOrNaN();
    }
}
