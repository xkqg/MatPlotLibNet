// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Ichimoku: the five Ichimoku lines: the conversion line, the base line, the two leading spans (as the batch gives them, not shifted forward) and the lagging line (the close). It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="Ichimoku"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingIchimoku : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _tenkanPeriod;
    private readonly int _kijunPeriod;
    private readonly int _senkouBPeriod;

    /// <summary>Creates a streaming Ichimoku.</summary>
    /// <param name="tenkanPeriod">The window of the conversion line.</param>
    /// <param name="kijunPeriod">The window of the base line.</param>
    /// <param name="senkouBPeriod">The window of the second leading span.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingIchimoku(int tenkanPeriod = 9, int kijunPeriod = 26, int senkouBPeriod = 52, int capacity = 10_000)
        : base(Math.Max(Math.Max(tenkanPeriod, kijunPeriod), senkouBPeriod), [
            new StreamingLineSeries(capacity) { Label = "Tenkan" },
            new StreamingLineSeries(capacity) { Label = "Kijun" },
            new StreamingLineSeries(capacity) { Label = "Senkou A" },
            new StreamingLineSeries(capacity) { Label = "Senkou B" },
            new StreamingLineSeries(capacity) { Label = "Chikou" },
        ])
    {
        _tenkanPeriod = tenkanPeriod;
        _kijunPeriod = kijunPeriod;
        _senkouBPeriod = senkouBPeriod;
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        var result = new Ichimoku(OhlcvBars.Hlc(window.Highs(), window.Lows(), window.Closes()), _tenkanPeriod, _kijunPeriod, _senkouBPeriod).Compute();
        outputs[0] = result.TenkanSen.LastOrNaN();
        outputs[1] = result.KijunSen.LastOrNaN();
        outputs[2] = result.SenkouSpanA.LastOrNaN();
        outputs[3] = result.SenkouSpanB.LastOrNaN();
        outputs[4] = result.ChikouSpan.LastOrNaN();
    }
}
