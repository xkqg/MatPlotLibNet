// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming VwapZScore: the z-score of the close against the volume-weighted average price of the last <c>window</c> bars. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="VwapZScore"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingVwapZScore : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _window;

    /// <summary>Creates a streaming VwapZScore.</summary>
    /// <param name="window">The window of each VWAP and of the deviations; at least 2.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingVwapZScore(int window = 20, int capacity = 10_000)
        : base(2 * window - 1, capacity)
    {
        _window = window;
        Label = $"VwapZ({window})";
    }

    /// <inheritdoc />
    /// <remarks>The first value comes with the <c>window</c>-th bar, from a deviation window of the bars there are; the value is the full one from the bar after <c>2 * window - 2</c> more.</remarks>
    public override int WarmupPeriod => _window;

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        outputs[0] = new VwapZScore(OhlcvBars.Cv(window.Closes(), window.Volumes()), _window).Compute().Values.LastOrNaN();
    }
}
