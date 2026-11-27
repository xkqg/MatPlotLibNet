// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Bocpd: Bayesian online change-point detection: the probability that the price just given starts a new regime. It is the step the batch <see cref="Bocpd"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingBocpd : StreamingIndicator<double>
{
    private BocpdStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <inheritdoc />
    internal override PaneRange? PinnedRange => Bocpd.Pane;

    /// <summary>Creates a streaming Bocpd.</summary>
    /// <param name="hazard">The prior probability of a change at any price, in (0, 1).</param>
    /// <param name="priorVariance">The variance added to the variance of every run; above 0.</param>
    /// <param name="maxRunLength">The longest run length kept; at least 1.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingBocpd(double hazard = Bocpd.DefaultHazard, double priorVariance = Bocpd.DefaultPriorVariance, int maxRunLength = Bocpd.DefaultMaxRunLength, int capacity = StreamingSeries.RestingCapacity)
        : base(Bocpd.Spec(hazard, priorVariance, maxRunLength), capacity)
    {
        _step = new BocpdStep(hazard, priorVariance, maxRunLength);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input);
    }
}
