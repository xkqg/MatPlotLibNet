// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the relative strength index with Wilder's smoothing: the recursion that the batch <see cref="Rsi"/> folds
/// over its prices and <see cref="Streaming.StreamingRsi"/> takes one price at a time. The average gain and the average loss start
/// as the mean of the first <c>period</c> changes (so the first value comes with the price after them), and after that each moves
/// toward the new change by <c>1 / period</c>. With no loss at all the index is 100. Before the first value the step says NaN.</summary>
internal struct RsiStep
{
    private readonly int _period;
    private double _averageGain;
    private double _averageLoss;
    private double _previous;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">How many changes the averages are of.</param>
    internal RsiStep(int period) => _period = period;

    /// <summary>Takes one price and returns the index, or NaN before <c>period + 1</c> prices have come.</summary>
    /// <param name="price">The price.</param>
    internal double Next(double price)
    {
        _count++;
        if (_count == 1)
        {
            _previous = price;
            return double.NaN;
        }

        double change = price - _previous;
        _previous = price;

        if (_count <= _period + 1)
        {
            if (change > 0) _averageGain += change;
            else _averageLoss -= change;

            if (_count < _period + 1) return double.NaN;

            _averageGain /= _period;
            _averageLoss /= _period;
        }
        else
        {
            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;
            _averageGain = (_averageGain * (_period - 1) + gain) / _period;
            _averageLoss = (_averageLoss * (_period - 1) + loss) / _period;
        }

        return _averageLoss == 0 ? 100 : 100 - 100 / (1 + _averageGain / _averageLoss);
    }
}
