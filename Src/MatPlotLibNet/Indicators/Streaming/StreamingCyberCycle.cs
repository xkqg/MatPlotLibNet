// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming CyberCycle: the Ehlers cyber cycle. It is the step the batch <see cref="CyberCycle"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingCyberCycle : StreamingIndicator<double>
{
    private CyberCycleStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming CyberCycle.</summary>
    /// <param name="alpha">The smoothing factor, in (0, 1).</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingCyberCycle(double alpha = CyberCycle.DefaultAlpha, int capacity = StreamingSeries.RestingCapacity)
        : base(CyberCycle.Spec(alpha), capacity)
    {
        _step = new CyberCycleStep(alpha);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input);
    }
}
