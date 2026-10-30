// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Relative Strength Index (Wilder's smoothing): the <see cref="RsiStep"/> the batch <see cref="Rsi"/> folds over
/// its prices, taken one price at a time. O(1) per append.</summary>
public sealed class StreamingRsi : StreamingIndicator
{
    private readonly int _period;
    private RsiStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => _period + 1; // need period+1 prices to get period changes

    /// <summary>Creates a streaming RSI indicator.</summary>
    public StreamingRsi(int period = 14, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _step = new RsiStep(period);
        Label = $"RSI({period})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => _step.Next(price);
}
