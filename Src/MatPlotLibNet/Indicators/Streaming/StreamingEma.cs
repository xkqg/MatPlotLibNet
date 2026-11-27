// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Exponential Moving Average: the step the batch <see cref="Ema"/> folds over its prices,
/// taken one price at a time. O(1) per append.</summary>
public sealed class StreamingEma : StreamingIndicator<double>
{
    private EmaStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming EMA indicator.</summary>
    public StreamingEma(int period, int capacity = StreamingSeries.RestingCapacity) : base(Ema.Spec(period), capacity)
    {
        _step = new EmaStep(period);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs) => outputs[0] = _step.Next(input);
}
