// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The three outputs of the cumulative sum control chart at one price.</summary>
/// <param name="Signal">1 when the upward sum crossed the threshold at this price, -1 when the downward sum did, 0 otherwise.</param>
/// <param name="SPos">The upward sum after this price (0 right after a signal).</param>
/// <param name="SNeg">The downward sum after this price (0 right after a signal).</param>
internal readonly record struct CusumPoint(double Signal, double SPos, double SNeg);

/// <summary>One step of the CUSUM change detector: the state machine that the batch <see cref="Cusum"/> folds over its prices and
/// <see cref="Streaming.StreamingCusum"/> takes one price at a time. Each log return, less the drift, is added to an upward sum that
/// is never below 0, and, plus the drift, to a downward sum that is never above 0; when one crosses the threshold the step signals
/// and that sum starts again at 0. The first price only gives the price before, so the step says NaN for it.</summary>
internal struct CusumStep
{
    private readonly double _threshold;
    private readonly double _drift;
    private double _previous;
    private double _positive;
    private double _negative;
    private int _count;

    /// <summary>A step with the given threshold and drift.</summary>
    /// <param name="threshold">The level a sum must cross to signal; above 0.</param>
    /// <param name="drift">The allowance taken off each log return.</param>
    internal CusumStep(double threshold, double drift)
    {
        _threshold = threshold;
        _drift = drift;
    }

    /// <summary>Takes one price (above 0) and returns the signal and the two sums, NaN for the first price.</summary>
    /// <param name="price">The price.</param>
    internal CusumPoint Next(double price)
    {
        if (_count++ == 0)
        {
            _previous = price;
            return new CusumPoint(double.NaN, double.NaN, double.NaN);
        }

        double y = Math.Log(price / _previous);
        _previous = price;
        _positive = Math.Max(0, _positive + y - _drift);
        _negative = Math.Min(0, _negative + y + _drift);

        double signal = 0;
        if (_positive > _threshold)
        {
            signal = 1;
            _positive = 0;
        }
        else if (_negative < -_threshold)
        {
            signal = -1;
            _negative = 0;
        }

        return new CusumPoint(signal, _positive, _negative);
    }
}
