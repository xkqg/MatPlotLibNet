// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming AdaptiveStochastic: the Ehlers adaptive stochastic: a stochastic whose look-back follows the dominant cycle of the typical price. It is the step the batch <see cref="AdaptiveStochastic"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingAdaptiveStochastic : StreamingIndicator<OhlcvBar>
{
    private AdaptiveStochasticStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <inheritdoc />
    internal override PaneRange? PinnedRange => AdaptiveStochastic.Pane;

    /// <summary>Creates a streaming AdaptiveStochastic.</summary>
    /// <param name="smoothingPeriod">The period of the super smoother put on the raw value; 1 leaves it raw.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingAdaptiveStochastic(int smoothingPeriod = AdaptiveStochastic.DefaultSmoothingPeriod, int capacity = StreamingSeries.RestingCapacity)
        : base(AdaptiveStochastic.Spec(smoothingPeriod), capacity)
    {
        _step = new AdaptiveStochasticStep(smoothingPeriod);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        input.ThrowIfHighBelowLow(nameof(AdaptiveStochastic), index);
        outputs[0] = _step.Next(input.High, input.Low, input.Close);
    }
}
