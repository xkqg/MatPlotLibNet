// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Roc: the rate of change against the price <c>lookback</c> prices back. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="Roc"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingRoc : WindowedStreamingIndicator<double>
{
    private readonly int _lookback;

    /// <summary>Creates a streaming Roc.</summary>
    /// <param name="lookback">How many prices back the price is compared with.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingRoc(int lookback, int capacity = StreamingSeries.RestingCapacity)
        : base(Roc.Spec(lookback), capacity)
    {
        _lookback = lookback;
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new Roc(window.ToArray(), _lookback).Compute().Values.LastOrNaN();
    }
}
