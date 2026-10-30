// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Simple Moving Average: the <see cref="RollingMeanStep"/> the batch <see cref="Sma"/> folds over its prices,
/// taken one price at a time. O(1) per append via a running sum.</summary>
public sealed class StreamingSma : StreamingIndicator
{
    private readonly int _period;
    private RollingMeanStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => _period;

    /// <summary>Creates a streaming SMA indicator.</summary>
    public StreamingSma(int period, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _step = new RollingMeanStep(period);
        Label = $"SMA({period})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => _step.Next(price);
}
