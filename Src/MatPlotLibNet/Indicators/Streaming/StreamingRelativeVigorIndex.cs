// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming RelativeVigorIndex: the relative vigor index and its signal line over the last <c>period</c> bars. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="RelativeVigorIndex"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingRelativeVigorIndex : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _period;

    /// <summary>Creates a streaming RelativeVigorIndex.</summary>
    /// <param name="period">How many bars the index is averaged over; at least 2.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingRelativeVigorIndex(int period = 10, int capacity = 10_000)
        : base(period + 6, [
            new StreamingLineSeries(capacity) { Label = $"RVI({period})" },
            new StreamingLineSeries(capacity) { Label = $"RVI({period}) Signal" },
        ])
    {
        _period = period;
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        var result = new RelativeVigorIndex(OhlcvBars.Ohlc(window.Opens(), window.Highs(), window.Lows(), window.Closes()), _period).Compute();
        outputs[0] = result.Rvi.LastOrNaN();
        outputs[1] = result.Signal.LastOrNaN();
    }
}
