// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of on-balance volume: the running sum that the batch <see cref="Obv"/> folds over its bars and
/// <see cref="Streaming.StreamingObv"/> takes one bar at a time, so the arithmetic exists once. The first bar opens the sum with
/// its own volume; after that a close above the one before adds the volume, a close below subtracts it, and an equal close keeps
/// the sum. A volume that is not a number turns the sum into NaN from the first bar that adds or subtracts it, because that is
/// what the arithmetic does.</summary>
internal struct ObvStep
{
    private double _sum;
    private double _previousClose;
    private bool _started;

    /// <summary>Takes one bar and returns the sum so far.</summary>
    /// <param name="close">The close of the bar.</param>
    /// <param name="volume">The volume of the bar.</param>
    internal double Next(double close, double volume)
    {
        if (!_started)
        {
            _started = true;
            _sum = volume;
        }
        else if (close > _previousClose)
        {
            _sum += volume;
        }
        else if (close < _previousClose)
        {
            _sum -= volume;
        }

        _previousClose = close;
        return _sum;
    }
}
