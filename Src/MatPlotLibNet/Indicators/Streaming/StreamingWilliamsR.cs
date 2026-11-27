// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Williams %R. It is the step the batch <see cref="WilliamsR"/> folds over its bars, taken one
/// candle at a time, so after each candle it says what the batch says over everything so far; the highest high and the lowest low of
/// the last <c>period</c> candles come at O(1) per candle.</summary>
public sealed class StreamingWilliamsR : StreamingIndicator<OhlcvBar>
{
    private WilliamsRStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <inheritdoc />
    internal override PaneRange? PinnedRange => WilliamsR.Pane;

    /// <summary>Creates a streaming Williams %R indicator.</summary>
    /// <param name="period">The number of bars the lowest low and the highest high are taken over.</param>
    /// <param name="capacity">The number of points the output series keeps.</param>
    public StreamingWilliamsR(int period = WilliamsR.DefaultPeriod, int capacity = StreamingSeries.RestingCapacity) : base(WilliamsR.Spec(period), capacity)
    {
        _step = new WilliamsRStep(period);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs) =>
        outputs[0] = _step.Next(input.High, input.Low, input.Close);
}
