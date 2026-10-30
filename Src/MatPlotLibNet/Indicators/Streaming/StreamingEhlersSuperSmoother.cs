// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming EhlersSuperSmoother: the Ehlers two-pole super smoother. It is the <see cref="SuperSmootherStep"/> the batch <see cref="EhlersSuperSmoother"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingEhlersSuperSmoother : StreamingIndicator<double>
{
    private SuperSmootherStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 1;

    /// <summary>Creates a streaming EhlersSuperSmoother.</summary>
    /// <param name="period">The cut-off period in bars; at least 2.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingEhlersSuperSmoother(int period = 10, int capacity = 10_000)
        : base(capacity)
    {
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        _step = new SuperSmootherStep(period);
        Label = $"SS({period})";
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input);
    }
}
