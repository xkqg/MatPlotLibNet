// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming SqueezeMomentum: the squeeze momentum over the last <c>period</c> bars: the momentum, and 1 when the bands are inside the channel (the squeeze is on) and 0 when they are not. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="SqueezeMomentum"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingSqueezeMomentum : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _period;
    private readonly double _bbMult;
    private readonly double _kcMult;

    /// <summary>Creates a streaming SqueezeMomentum.</summary>
    /// <param name="period">How many bars the bands and the channel are taken over; at least 2.</param>
    /// <param name="bbMult">The width of the Bollinger band in standard deviations.</param>
    /// <param name="kcMult">The width of the Keltner channel in average true ranges.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingSqueezeMomentum(int period = 20, double bbMult = 2.0, double kcMult = 1.5, int capacity = 10_000)
        : base(period + 2, [
            new StreamingLineSeries(capacity) { Label = $"Squeeze({period}) Momentum" },
            new StreamingLineSeries(capacity) { Label = $"Squeeze({period}) On" },
        ])
    {
        _period = period;
        _bbMult = bbMult;
        _kcMult = kcMult;
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        var result = new SqueezeMomentum(OhlcvBars.Hlc(window.Highs(), window.Lows(), window.Closes()), _period, _bbMult, _kcMult).Compute();
        outputs[0] = result.Momentum.LastOrNaN();
        outputs[1] = result.SqueezeOn.Length > 0 ? (result.SqueezeOn[^1] ? 1 : 0) : double.NaN;
    }
}
