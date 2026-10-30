// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming KaufmanEfficiencyRatio: Kaufman's efficiency ratio over the last <c>period</c> price changes. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="KaufmanEfficiencyRatio"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingKaufmanEfficiencyRatio : WindowedStreamingIndicator<double>
{
    private readonly int _period;

    /// <summary>Creates a streaming KaufmanEfficiencyRatio.</summary>
    /// <param name="period">How many price changes the ratio is taken over; at least 1.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingKaufmanEfficiencyRatio(int period = 10, int capacity = 10_000)
        : base(period + 1, capacity)
    {
        _period = period;
        Label = $"ER({period})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new KaufmanEfficiencyRatio(window.ToArray(), _period).Compute().Values.LastOrNaN();
    }
}
