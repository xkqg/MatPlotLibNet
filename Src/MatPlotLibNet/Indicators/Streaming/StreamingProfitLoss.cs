// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming ProfitLoss: the profit or loss just given, which has no memory. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="ProfitLoss"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingProfitLoss : WindowedStreamingIndicator<double>
{

    /// <summary>Creates a streaming ProfitLoss.</summary>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingProfitLoss(int capacity = 10_000)
        : base(1, capacity)
    {
        Label = "P&L";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new ProfitLoss(window.ToArray()).Compute().Values.LastOrNaN();
    }
}
