// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of a simple moving average: the running sum that the streaming forms take one value at a time (the average of a price,
/// of a MACD line, of a %K) and that <see cref="Numerics.RollingWindowExtensions"/>' <c>RollingMean</c> adds over a whole run, in the same order
/// and so to the same bit. The first mean comes with the <c>period</c>-th value; each value after that enters the sum as it
/// leaves the oldest. Before the first mean the step says NaN.</summary>
internal struct RollingMeanStep
{
    private InputWindow<double> _window;
    private double _sum;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">How many values are averaged; at least 1.</param>
    internal RollingMeanStep(int period) => _window = new InputWindow<double>(period);

    /// <summary>Whether <c>period</c> values have come, so the step says a mean.</summary>
    internal readonly bool IsFull => _window.IsFull;

    /// <summary>The last <c>period</c> values in the order of their places: what a spread about the mean is taken over (the streaming
    /// Bollinger bands), in the order it has always been summed in.</summary>
    internal readonly ReadOnlySpan<double> Window => _window.Slots;

    /// <summary>Takes one value and returns the mean of the last <c>period</c> values, or NaN before that many have come.</summary>
    /// <param name="value">The value.</param>
    internal double Next(double value)
    {
        if (_window.IsFull) _sum += value - _window.Oldest;
        else _sum += value;

        _window.Push(value);
        return _window.IsFull ? _sum / _window.Width : double.NaN;
    }
}
