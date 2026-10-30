// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Exponential Moving Average: the <see cref="EmaStep"/> the batch <see cref="Ema"/> folds over its prices,
/// taken one price at a time. O(1) per append.</summary>
public sealed class StreamingEma : StreamingIndicator
{
    private readonly int _period;
    private EmaStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => _period;

    /// <summary>Creates a streaming EMA indicator.</summary>
    public StreamingEma(int period, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _step = new EmaStep(period);
        Label = $"EMA({period})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => _step.Next(price);
}
