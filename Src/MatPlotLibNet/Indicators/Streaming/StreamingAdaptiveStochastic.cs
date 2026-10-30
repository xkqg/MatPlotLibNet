// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming AdaptiveStochastic: the Ehlers adaptive stochastic: a stochastic whose look-back follows the dominant cycle of the typical price. It is the <see cref="AdaptiveStochasticStep"/> the batch <see cref="AdaptiveStochastic"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingAdaptiveStochastic : StreamingIndicator<OhlcvBar>
{
    private AdaptiveStochasticStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 7;

    /// <summary>Creates a streaming AdaptiveStochastic.</summary>
    /// <param name="smoothingPeriod">The period of the super smoother put on the raw value; 1 leaves it raw.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingAdaptiveStochastic(int smoothingPeriod = 3, int capacity = 10_000)
        : base(capacity)
    {
        if (smoothingPeriod < 1)
            throw new ArgumentException($"smoothingPeriod must be >= 1 (got {smoothingPeriod}).", nameof(smoothingPeriod));
        _step = new AdaptiveStochasticStep(smoothingPeriod);
        Label = $"AdaptStoch({smoothingPeriod})";
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        if (input.High < input.Low)
            throw new ArgumentException($"AdaptiveStochastic requires H >= L; bar {index} has H={input.High} < L={input.Low}.");
        outputs[0] = _step.Next(input.High, input.Low, input.Close);
    }
}
