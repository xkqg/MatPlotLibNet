// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the average true range with Wilder's smoothing: the recursion that the batch <see cref="Atr"/> folds over
/// its bars and <see cref="Streaming.StreamingAtr"/> takes one bar at a time. The first bar has no true range (it has no close
/// before it), so the first value is the mean of the true ranges of the next <c>period</c> bars and comes with the last of them; after
/// that each value moves toward the new true range by <c>1 / period</c>. Before the first value the step says NaN.</summary>
internal struct AtrStep
{
    private readonly int _period;
    private double _average;
    private double _previousClose;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">How many true ranges the average is of.</param>
    internal AtrStep(int period) => _period = period;

    /// <summary>Takes one bar and returns the average true range, or NaN before <c>period + 1</c> bars have come.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    internal double Next(double high, double low, double close)
    {
        _count++;
        if (_count == 1)
        {
            _previousClose = close;
            return double.NaN;
        }

        double trueRange = PriceSources.TrueRange(high, low, _previousClose);
        _previousClose = close;

        if (_count <= _period + 1)
        {
            _average += trueRange;
            if (_count < _period + 1) return double.NaN;

            _average /= _period;
            return _average;
        }

        _average = (_average * (_period - 1) + trueRange) / _period;
        return _average;
    }
}
