// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The two lines of the Klinger volume oscillator at one bar.</summary>
/// <param name="Kvo">The oscillator: the fast average of the volume force less the slow one.</param>
/// <param name="Signal">The average of the oscillator over the signal period.</param>
internal readonly record struct KlingerPoint(double Kvo, double Signal);

/// <summary>One step of the Klinger volume oscillator: the recursion that the batch <see cref="KlingerVolumeOscillator"/> folds over
/// its bars and <see cref="Streaming.StreamingKlingerVolumeOscillator"/> takes one bar at a time. A bar's trend is whether its
/// typical price rose, fell or held against the bar before; the cumulative measure restarts when the trend changes and otherwise
/// adds the range; the volume force is the volume times how far the range falls short of the cumulative measure, signed by the
/// trend. The oscillator is the difference of two <see cref="SeededEmaStep"/> averages of the volume force, and the signal is a
/// third of the oscillator. The first bar only gives the bar before, so the step says NaN for it.</summary>
internal struct KlingerStep
{
    private SeededEmaStep _fast;
    private SeededEmaStep _slow;
    private SeededEmaStep _signal;
    private double _previousTypical;
    private double _previousRange;
    private double _cumulative;
    private int _previousTrend;
    private int _count;

    /// <summary>A step with the given periods.</summary>
    /// <param name="fastPeriod">The period of the fast average; at least 2.</param>
    /// <param name="slowPeriod">The period of the slow average; above the fast one.</param>
    /// <param name="signalPeriod">The period of the signal average; at least 1.</param>
    internal KlingerStep(int fastPeriod, int slowPeriod, int signalPeriod)
    {
        _fast = new SeededEmaStep(2.0 / (fastPeriod + 1));
        _slow = new SeededEmaStep(2.0 / (slowPeriod + 1));
        _signal = new SeededEmaStep(2.0 / (signalPeriod + 1));
    }

    /// <summary>Takes one bar and returns the oscillator and its signal, NaN for the first bar.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    /// <param name="volume">The volume of the bar.</param>
    internal KlingerPoint Next(double high, double low, double close, double volume)
    {
        double typical = (high + low + close) / 3.0;
        double range = high - low;
        if (_count++ == 0)
        {
            _previousTypical = typical;
            _previousRange = range;
            return new KlingerPoint(double.NaN, double.NaN);
        }

        int trend = typical > _previousTypical ? 1 : (typical < _previousTypical ? -1 : 0);
        _cumulative = (_count == 2 || trend != _previousTrend) ? _previousRange + range : _cumulative + range;
        double force = _cumulative > 0
            ? volume * Math.Abs(2.0 * range / _cumulative - 1.0) * trend * 100.0
            : 0.0;
        _previousTypical = typical;
        _previousRange = range;
        _previousTrend = trend;

        double kvo = _fast.Next(force) - _slow.Next(force);
        return new KlingerPoint(kvo, _signal.Next(kvo));
    }
}
