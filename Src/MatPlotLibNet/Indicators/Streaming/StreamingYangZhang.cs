// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming YangZhang: the Yang-Zhang volatility over the last <c>period</c> bars. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="YangZhang"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingYangZhang : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _period;

    /// <summary>Creates a streaming YangZhang.</summary>
    /// <param name="period">How many bars the volatility is taken over; at least 2; prices above 0.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingYangZhang(int period = 20, int capacity = 10_000)
        : base(period + 1, capacity)
    {
        _period = period;
        Label = $"YZ({period})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        outputs[0] = new YangZhang(OhlcvBars.Ohlc(window.Opens(), window.Highs(), window.Lows(), window.Closes()), _period).Compute().Values.LastOrNaN();
    }
}
