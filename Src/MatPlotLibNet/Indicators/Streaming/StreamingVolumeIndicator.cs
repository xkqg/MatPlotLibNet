// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming VolumeIndicator: the volume of the bar just given, which has no memory. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="VolumeIndicator"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingVolumeIndicator : WindowedStreamingIndicator<OhlcvBar>
{

    /// <summary>Creates a streaming VolumeIndicator.</summary>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingVolumeIndicator(int capacity = 10_000)
        : base(1, capacity)
    {
        Label = "Volume";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        outputs[0] = new VolumeIndicator(window.Volumes()).Compute().Values.LastOrNaN();
    }
}
