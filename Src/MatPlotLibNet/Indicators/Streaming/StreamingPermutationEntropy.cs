// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming PermutationEntropy: the permutation entropy over the last <c>window</c> prices. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="PermutationEntropy"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingPermutationEntropy : WindowedStreamingIndicator<double>
{
    private readonly int _order;
    private readonly int _window;

    /// <summary>Creates a streaming PermutationEntropy.</summary>
    /// <param name="order">The length of the ordinal patterns, from 2 to 7.</param>
    /// <param name="window">How many prices the entropy is taken over; above the order.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingPermutationEntropy(int order = 4, int window = 100, int capacity = 10_000)
        : base(window, capacity)
    {
        _order = order;
        _window = window;
        Label = $"PE(d={order},W={window})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new PermutationEntropy(window.ToArray(), _order, _window).Compute().Values.LastOrNaN();
    }
}
