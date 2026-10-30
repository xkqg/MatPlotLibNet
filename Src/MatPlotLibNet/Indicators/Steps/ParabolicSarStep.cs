// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The outputs of the parabolic SAR at one bar.</summary>
/// <param name="Sar">The stop and reverse level; NaN before the second bar.</param>
/// <param name="IsLong">Whether the trend is up (the SAR is below the price).</param>
internal readonly record struct ParabolicSarPoint(double Sar, bool IsLong);

/// <summary>One step of the parabolic SAR: the state machine that the batch <see cref="ParabolicSar"/> folds over its bars and
/// <see cref="Streaming.StreamingParabolicSar"/> takes one bar at a time. The trend starts up when the second high is not below the
/// first; the SAR then moves toward the extreme point by an acceleration factor that grows each time the extreme point is beaten, up
/// to the maximum, and never past the lows (or highs) of the two bars before; when the price crosses it the trend reverses, the
/// SAR jumps to the extreme point and the factor starts again. The first bar only seeds the trend (it is read together with the
/// second), so the step says NaN for it.</summary>
internal struct ParabolicSarStep
{
    private readonly double _step;
    private readonly double _max;
    private double _high1, _high2;
    private double _low1, _low2;
    private double _sar;
    private double _acceleration;
    private double _extreme;
    private bool _trending;
    private ParabolicSarPoint _first;
    private int _count;

    /// <summary>A step with the given acceleration.</summary>
    /// <param name="step">The acceleration factor at the start and the amount it grows by.</param>
    /// <param name="max">The largest acceleration factor.</param>
    internal ParabolicSarStep(double step, double max)
    {
        _step = step;
        _max = max;
    }

    /// <summary>The first bar, which the step can only say once it has read the second: the SAR at the first low when the trend starts up,
    /// at the first high when it starts down. Meaningful after the second bar.</summary>
    internal readonly ParabolicSarPoint First => _first;

    /// <summary>Takes one bar and returns the SAR and the trend, NaN for the first bar.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    internal ParabolicSarPoint Next(double high, double low)
    {
        int i = _count++;
        if (i == 0)
        {
            _high1 = high;
            _low1 = low;
            return new ParabolicSarPoint(double.NaN, false);
        }

        if (i == 1)
        {
            _trending = high >= _high1;
            _acceleration = _step;
            _extreme = _trending ? _high1 : _low1;
            _sar = _trending ? _low1 : _high1;
            _first = new ParabolicSarPoint(_sar, _trending);
        }

        double prevSar = _sar;
        if (_trending)
        {
            if (high > _extreme)
            {
                _extreme = high;
                _acceleration = Math.Min(_acceleration + _step, _max);
            }

            double newSar = prevSar + _acceleration * (_extreme - prevSar);
            if (i >= 2) newSar = Math.Min(newSar, Math.Min(_low1, _low2));
            else newSar = Math.Min(newSar, _low1);
            if (newSar >= low)
            {
                _trending = false;
                _acceleration = _step;
                _extreme = low;
                _sar = _extreme;
            }
            else
            {
                _sar = newSar;
            }
        }
        else
        {
            if (low < _extreme)
            {
                _extreme = low;
                _acceleration = Math.Min(_acceleration + _step, _max);
            }

            double newSar = prevSar + _acceleration * (_extreme - prevSar);
            if (i >= 2) newSar = Math.Max(newSar, Math.Max(_high1, _high2));
            else newSar = Math.Max(newSar, _high1);
            if (newSar <= high)
            {
                _trending = true;
                _acceleration = _step;
                _extreme = high;
                _sar = _extreme;
            }
            else
            {
                _sar = newSar;
            }
        }

        _high2 = _high1;
        _high1 = high;
        _low2 = _low1;
        _low1 = low;
        return new ParabolicSarPoint(_sar, _trending);
    }
}
