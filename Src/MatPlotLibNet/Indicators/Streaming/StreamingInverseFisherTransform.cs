// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming InverseFisherTransform: the inverse Fisher transform of the value just given, which has no memory. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="InverseFisherTransform"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingInverseFisherTransform : WindowedStreamingIndicator<double>
{
    private readonly double _scale;

    /// <summary>Creates a streaming InverseFisherTransform.</summary>
    /// <param name="scale">The factor the value is scaled by before the transform; above 0.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingInverseFisherTransform(double scale = 1.0, int capacity = 10_000)
        : base(1, capacity)
    {
        _scale = scale;
        Label = $"IFT(scale={scale:0.##})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new InverseFisherTransform(window.ToArray(), _scale).Compute().Values.LastOrNaN();
    }
}
