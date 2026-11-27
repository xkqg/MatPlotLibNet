// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming TwiggsMoneyFlow: Twiggs money flow: the volume-weighted accumulation of the close in the true range, relative to the volume. It is the step the batch <see cref="TwiggsMoneyFlow"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingTwiggsMoneyFlow : StreamingIndicator<OhlcvBar>
{
    private TwiggsMoneyFlowStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <inheritdoc />
    internal override PaneRange? PinnedRange => TwiggsMoneyFlow.Pane;

    /// <summary>Creates a streaming TwiggsMoneyFlow.</summary>
    /// <param name="period">The period of the smoothing; at least 2.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingTwiggsMoneyFlow(int period = TwiggsMoneyFlow.DefaultPeriod, int capacity = StreamingSeries.RestingCapacity)
        : base(TwiggsMoneyFlow.Spec(period), capacity)
    {
        _step = new TwiggsMoneyFlowStep(period);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input.High, input.Low, input.Close, input.Volume);
    }
}
