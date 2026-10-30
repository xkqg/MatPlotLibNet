// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The outputs of the Supertrend at one bar.</summary>
/// <param name="Line">The stop line: the lower band while the trend is up, the upper band while it is down; NaN before the first value.</param>
/// <param name="Direction">1 for an up trend, -1 for a down trend, 0 before the first value.</param>
/// <param name="Flipped">Whether the trend changed direction at this bar.</param>
internal readonly record struct SupertrendPoint(double Line, int Direction, bool Flipped);

/// <summary>One step of the Supertrend: the state machine that the batch <see cref="Supertrend"/> folds over its bars and
/// <see cref="Streaming.StreamingSupertrend"/> takes one bar at a time. Bands are put the multiplier times the <see cref="AtrStep"/>
/// above and below the midpoint of the bar; a band only moves toward the price, unless the close before broke through it; the
/// trend turns up when the close is above the upper band of the bar before and down when it is below the lower one. The first value comes with
/// the first average true range, so the step says no value (a 0 direction) before it.</summary>
internal struct SupertrendStep
{
    private readonly double _multiplier;
    private AtrStep _atr;
    private double _previousClose;
    private double _finalUpper;
    private double _finalLower;
    private int _direction = 1;
    private int _emitted;

    /// <summary>A step with the given period and multiplier.</summary>
    /// <param name="period">The period of the average true range.</param>
    /// <param name="multiplier">How many average true ranges the bands lie from the midpoint; above 0.</param>
    internal SupertrendStep(int period, double multiplier)
    {
        _multiplier = multiplier;
        _atr = new AtrStep(period);
    }

    /// <summary>Takes one bar and returns the stop line, the direction and whether it flipped.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    internal SupertrendPoint Next(double high, double low, double close)
    {
        double atr = _atr.Next(high, low, close);
        double previousClose = _previousClose;
        _previousClose = close;
        if (double.IsNaN(atr)) return new SupertrendPoint(double.NaN, 0, false);

        double mid = (high + low) / 2.0;
        double basicUpper = mid + _multiplier * atr;
        double basicLower = mid - _multiplier * atr;
        double finalUpper;
        double finalLower;
        int direction;
        bool flipped;
        if (_emitted++ == 0)
        {
            finalUpper = basicUpper;
            finalLower = basicLower;
            direction = +1;
            flipped = false;
        }
        else
        {
            finalUpper = (basicUpper < _finalUpper || previousClose > _finalUpper) ? basicUpper : _finalUpper;
            finalLower = (basicLower > _finalLower || previousClose < _finalLower) ? basicLower : _finalLower;
            if (close > _finalUpper) direction = +1;
            else if (close < _finalLower) direction = -1;
            else direction = _direction;
            flipped = direction != _direction;
        }

        _finalUpper = finalUpper;
        _finalLower = finalLower;
        _direction = direction;
        return new SupertrendPoint(direction > 0 ? finalLower : finalUpper, direction, flipped);
    }
}
