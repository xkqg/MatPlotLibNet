// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming YangZhangVolRatio: the ratio of the short Yang-Zhang volatility to the long one. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="YangZhangVolRatio"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingYangZhangVolRatio : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _shortWindow;
    private readonly int _longWindow;

    /// <summary>Creates a streaming YangZhangVolRatio.</summary>
    /// <param name="shortWindow">The window of the short volatility; at least 2.</param>
    /// <param name="longWindow">The window of the long volatility; above the short one.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingYangZhangVolRatio(int shortWindow = 20, int longWindow = 60, int capacity = 10_000)
        : base(longWindow + 1, capacity)
    {
        _shortWindow = shortWindow;
        _longWindow = longWindow;
        Label = $"YZRatio({shortWindow}/{longWindow})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        outputs[0] = new YangZhangVolRatio(OhlcvBars.Ohlc(window.Opens(), window.Highs(), window.Lows(), window.Closes()), _shortWindow, _longWindow).Compute().Values.LastOrNaN();
    }
}
