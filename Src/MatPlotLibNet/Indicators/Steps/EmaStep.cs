// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of an exponential moving average: the recursion that the batch <see cref="Ema"/> folds over its prices and
/// <see cref="Streaming.StreamingEma"/> takes one price at a time, so the arithmetic exists once. The first value is the plain mean of
/// the first <c>period</c> prices; after that each value moves toward the price by <c>2 / (period + 1)</c>. Before the first
/// value the step says NaN.</summary>
internal struct EmaStep
{
    private readonly int _period;
    private readonly double _multiplier;
    private double _value;
    private double _sum;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">The period; 2 / (period + 1) is the weight of a new price.</param>
    internal EmaStep(int period)
    {
        _period = period;
        _multiplier = 2.0 / (period + 1);
    }

    /// <summary>Takes one price and returns the average so far, or NaN before the first <c>period</c> prices have come.</summary>
    /// <param name="price">The price.</param>
    internal double Next(double price)
    {
        _count++;
        if (_count < _period)
        {
            _sum += price;
            return double.NaN;
        }

        if (_count == _period)
        {
            _sum += price;
            _value = _sum / _period;
            return _value;
        }

        _value = (price - _value) * _multiplier + _value;
        return _value;
    }
}
