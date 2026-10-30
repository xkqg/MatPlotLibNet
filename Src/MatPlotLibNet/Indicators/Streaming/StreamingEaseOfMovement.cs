// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming EaseOfMovement: the ease of movement (the midpoint move per unit of volume) averaged over the last <c>period</c> bars. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="EaseOfMovement"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingEaseOfMovement : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _period;
    private readonly double _scale;

    /// <summary>Creates a streaming EaseOfMovement.</summary>
    /// <param name="period">How many bars the movement is averaged over; at least 2.</param>
    /// <param name="scale">The factor the volume is scaled by; above 0.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingEaseOfMovement(int period = 14, double scale = 1_000_000, int capacity = 10_000)
        : base(period + 1, capacity)
    {
        _period = period;
        _scale = scale;
        Label = $"EMV({period})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        outputs[0] = new EaseOfMovement(OhlcvBars.Hlv(window.Highs(), window.Lows(), window.Volumes()), _period, _scale).Compute().Values.LastOrNaN();
    }
}
