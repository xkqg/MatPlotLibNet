// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Simple Moving Average: the step the batch <see cref="Sma"/> folds over its prices,
/// taken one price at a time. O(1) per append via a running sum.</summary>
public sealed class StreamingSma : StreamingIndicator<double>
{
    private RollingMeanStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming SMA indicator.</summary>
    public StreamingSma(int period, int capacity = StreamingSeries.RestingCapacity) : base(Sma.Spec(period), capacity)
    {
        _step = new RollingMeanStep(period);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs) => outputs[0] = _step.Next(input);
}
