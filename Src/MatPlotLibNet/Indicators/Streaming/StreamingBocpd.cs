// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Bocpd: Bayesian online change-point detection: the probability that the price just given starts a new regime. It is the <see cref="BocpdStep"/> the batch <see cref="Bocpd"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingBocpd : StreamingIndicator<double>
{
    private BocpdStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 2;

    /// <summary>Creates a streaming Bocpd.</summary>
    /// <param name="hazard">The prior probability of a change at any price, in (0, 1).</param>
    /// <param name="priorVariance">The variance added to the variance of every run; above 0.</param>
    /// <param name="maxRunLength">The longest run length kept; at least 1.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingBocpd(double hazard = 0.01, double priorVariance = 1.0, int maxRunLength = 500, int capacity = 10_000)
        : base(capacity)
    {
        if (hazard <= 0 || hazard >= 1)
            throw new ArgumentException($"hazard must be in (0, 1) (got {hazard}).", nameof(hazard));
        if (priorVariance <= 0)
            throw new ArgumentException($"priorVariance must be > 0 (got {priorVariance}).", nameof(priorVariance));
        if (maxRunLength < 1)
            throw new ArgumentException($"maxRunLength must be >= 1 (got {maxRunLength}).", nameof(maxRunLength));
        _step = new BocpdStep(hazard, priorVariance, maxRunLength);
        Label = $"BOCPD(h={hazard.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input);
    }
}
