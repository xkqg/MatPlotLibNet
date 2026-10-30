// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming WaveletEnergyRatio: the Haar wavelet energy of one band over the last <c>window</c> prices, as a share of the energy of all bands. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="WaveletEnergyRatio"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingWaveletEnergyRatio : WindowedStreamingIndicator<double>
{
    private readonly int _window;
    private readonly int _level;

    /// <summary>Creates a streaming WaveletEnergyRatio.</summary>
    /// <param name="window">How many prices the transform is taken over; a power of 2 of at least 4.</param>
    /// <param name="level">The band whose share of the energy is read.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingWaveletEnergyRatio(int window = 64, int level = 0, int capacity = 10_000)
        : base(window, capacity)
    {
        _window = window;
        _level = level;
        Label = $"WER(W={window},L={level})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new WaveletEnergyRatio(window.ToArray(), _window, _level).Compute().Values.LastOrNaN();
    }
}
