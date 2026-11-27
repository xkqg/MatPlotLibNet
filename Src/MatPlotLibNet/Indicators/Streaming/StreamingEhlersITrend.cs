// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming EhlersITrend: the Ehlers instantaneous trendline. It is the step the batch <see cref="EhlersITrend"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingEhlersITrend : StreamingIndicator<double>
{
    private EhlersITrendStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming EhlersITrend.</summary>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingEhlersITrend(int capacity = StreamingSeries.RestingCapacity)
        : base(EhlersITrend.Spec(), capacity)
    {
        _step = new EhlersITrendStep();
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input);
    }
}
