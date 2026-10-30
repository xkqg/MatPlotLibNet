// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of a simple moving average: the running sum that <see cref="Numerics.VectorMath.RollingMean"/> folds over
/// its values and the streaming forms take one value at a time (the average of a price, of a MACD line, of a %K), so the arithmetic
/// exists once. The first mean comes with the <c>period</c>-th value; each value after that enters the sum as it
/// leaves the oldest. Before the first mean the step says NaN.</summary>
internal struct RollingMeanStep
{
    private readonly double[] _window;
    private double _sum;
    private int _count;
    private int _slot;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">How many values are averaged; at least 1.</param>
    internal RollingMeanStep(int period) => _window = new double[period];

    /// <summary>Takes one value and returns the mean of the last <c>period</c> values, or NaN before that many have come.</summary>
    /// <param name="value">The value.</param>
    internal double Next(double value)
    {
        if (_count < _window.Length)
        {
            _sum += value;
            _count++;
        }
        else
        {
            _sum += value - _window[_slot];
        }

        _window[_slot] = value;
        if (++_slot == _window.Length) _slot = 0;
        return _count < _window.Length ? double.NaN : _sum / _window.Length;
    }
}
