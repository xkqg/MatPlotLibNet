// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Exponential Moving Average. O(1) per append: α * price + (1-α) * prev.</summary>
public sealed class StreamingEma : StreamingIndicatorBase
{
    private readonly int _period;
    private readonly double _multiplier;
    private double _ema;
    private double _smaSum;

    /// <inheritdoc />
    public override int WarmupPeriod => _period;

    /// <summary>Creates a streaming EMA indicator.</summary>
    public StreamingEma(int period, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _multiplier = 2.0 / (period + 1);
        Label = $"EMA({period})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        if (ProcessedCount <= _period)
        {
            _smaSum += price;
            if (ProcessedCount == _period)
            {
                _ema = _smaSum / _period;
                return _ema;
            }
            return double.NaN;
        }

        _ema = (price - _ema) * _multiplier + _ema;
        return _ema;
    }
}
