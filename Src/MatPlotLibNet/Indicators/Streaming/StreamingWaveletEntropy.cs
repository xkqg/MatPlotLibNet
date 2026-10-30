// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming WaveletEntropy: the Shannon entropy of the Haar wavelet band energies over the last <c>window</c> prices. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="WaveletEntropy"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingWaveletEntropy : WindowedStreamingIndicator<double>
{
    private readonly int _window;

    /// <summary>Creates a streaming WaveletEntropy.</summary>
    /// <param name="window">How many prices the transform is taken over; a power of 2 of at least 4.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingWaveletEntropy(int window = 64, int capacity = 10_000)
        : base(window, capacity)
    {
        _window = window;
        Label = $"WEnt(W={window})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new WaveletEntropy(window.ToArray(), _window).Compute().Values.LastOrNaN();
    }
}
