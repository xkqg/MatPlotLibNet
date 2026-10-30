// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming AroonOscillator: the Aroon oscillator (Aroon Up less Aroon Down) over the last <c>period</c> bars. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="AroonOscillator"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingAroonOscillator : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _period;

    /// <summary>Creates a streaming AroonOscillator.</summary>
    /// <param name="period">How many bars back the highest high and the lowest low are looked for; at least 2.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingAroonOscillator(int period = 25, int capacity = 10_000)
        : base(period + 1, capacity)
    {
        _period = period;
        Label = $"Aroon({period})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        outputs[0] = new AroonOscillator(OhlcvBars.Hl(window.Highs(), window.Lows()), _period).Compute().Values.LastOrNaN();
    }
}
