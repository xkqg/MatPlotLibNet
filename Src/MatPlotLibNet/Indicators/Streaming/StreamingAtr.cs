// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Average True Range (Wilder's smoothing): the step the batch <see cref="Atr"/> folds over its
/// bars, taken one bar at a time. O(1) per bar. The first bar has no true range, so the first value comes with the bar after
/// <c>period</c> more.</summary>
public sealed class StreamingAtr : StreamingIndicator<OhlcvBar>
{
    private AtrStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming ATR indicator.</summary>
    public StreamingAtr(int period = Atr.DefaultPeriod, int capacity = StreamingSeries.RestingCapacity) : base(Atr.Spec(period), capacity)
    {
        _step = new AtrStep(period);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs) =>
        outputs[0] = _step.Next(input.High, input.Low, input.Close);
}
