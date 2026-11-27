// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The outputs of the parabolic SAR at one bar.</summary>
/// <param name="Sar">The stop and reverse level; NaN for the first bar.</param>
/// <param name="IsLong">Whether the trend is up (the SAR is below the price).</param>
internal readonly record struct ParabolicSarPoint(double Sar, bool IsLong);

/// <summary>One step of Wilder's parabolic SAR as TA-Lib computes it: the state machine that the batch <see cref="ParabolicSar"/> folds
/// over its bars and <see cref="Streaming.StreamingParabolicSar"/> takes one bar at a time. The first bar only seeds the trend with
/// the second, so the step says NaN for it. The trend starts down when the second bar's minus directional movement is positive (its
/// low fell further than its high rose), else up; the second bar's SAR is the first bar's low (up) or high (down), and the extreme
/// point the second bar's high (up) or low (down). Each bar is first checked against the SAR worked out for it: a low at or below
/// an up-trend's SAR (a high at or above a down-trend's) reverses the trend, and the bar's SAR becomes the extreme point of the trend
/// that ended, kept outside the range of the bar and the one before. Otherwise the bar keeps its SAR, and its high (low) beyond the
/// extreme point moves the extreme point and grows the acceleration factor by the step, up to the maximum. The SAR of the next bar
/// is then this one's moved toward the extreme point by the factor, never into the range of this bar or the one before.</summary>
internal struct ParabolicSarStep
{
    private readonly double _step;
    private readonly double _max;
    private double _previousHigh, _previousLow;
    private double _sar;
    private double _acceleration;
    private double _extreme;
    private bool _isLong;
    private int _count;

    /// <summary>A step with the given acceleration.</summary>
    /// <param name="step">The acceleration factor at the start and the amount it grows by.</param>
    /// <param name="max">The largest acceleration factor.</param>
    internal ParabolicSarStep(double step, double max)
    {
        // TA-Lib caps the starting factor and its growth at the maximum when the step is the larger.
        _step = Math.Min(step, max);
        _max = max;
    }

    /// <summary>Takes one bar and returns the SAR and the trend, NaN for the first bar.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <returns>The SAR and the trend of the bar.</returns>
    internal ParabolicSarPoint Next(double high, double low)
    {
        int i = _count++;
        if (i == 0)
        {
            _previousHigh = high;
            _previousLow = low;
            return new ParabolicSarPoint(double.NaN, false);
        }

        if (i == 1)
        {
            // TA-Lib starts from the minus directional movement of the second bar, read against the first.
            double up = high - _previousHigh, down = _previousLow - low;
            _isLong = !(down > 0 && down > up);
            _sar = _isLong ? _previousLow : _previousHigh;
            _extreme = _isLong ? high : low;
            _acceleration = _step;
            // The range the second bar's SAR is held out of is the second bar alone, as TA-Lib reads it.
            _previousHigh = high;
            _previousLow = low;
        }

        double sar;
        if (_isLong)
        {
            if (low <= _sar)
            {
                _isLong = false;
                sar = Math.Max(_extreme, Math.Max(_previousHigh, high));
                _acceleration = _step;
                _extreme = low;
                _sar = Math.Max(sar + _acceleration * (_extreme - sar), Math.Max(_previousHigh, high));
            }
            else
            {
                sar = _sar;
                if (high > _extreme)
                {
                    _extreme = high;
                    _acceleration = Math.Min(_acceleration + _step, _max);
                }

                _sar = Math.Min(sar + _acceleration * (_extreme - sar), Math.Min(_previousLow, low));
            }
        }
        else
        {
            if (high >= _sar)
            {
                _isLong = true;
                sar = Math.Min(_extreme, Math.Min(_previousLow, low));
                _acceleration = _step;
                _extreme = high;
                _sar = Math.Min(sar + _acceleration * (_extreme - sar), Math.Min(_previousLow, low));
            }
            else
            {
                sar = _sar;
                if (low < _extreme)
                {
                    _extreme = low;
                    _acceleration = Math.Min(_acceleration + _step, _max);
                }

                _sar = Math.Max(sar + _acceleration * (_extreme - sar), Math.Max(_previousHigh, high));
            }
        }

        _previousHigh = high;
        _previousLow = low;
        return new ParabolicSarPoint(sar, _isLong);
    }
}
