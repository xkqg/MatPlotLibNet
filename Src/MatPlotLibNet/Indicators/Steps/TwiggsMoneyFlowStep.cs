// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of Twiggs money flow: the recursion that the batch <see cref="TwiggsMoneyFlow"/> folds over its bars and
/// <see cref="Streaming.StreamingTwiggsMoneyFlow"/> takes one bar at a time. The accumulation of a bar is its volume times where the
/// close sits in the true range (from the true low to the true high, using the close before); the accumulation and the volume are
/// each averaged by Wilder's exponential average (seeded with the first value, weight <c>1 / period</c>), and the money flow is
/// their ratio (0 when there is no volume). The first bar has no close before it, so the step says NaN for it.</summary>
internal struct TwiggsMoneyFlowStep
{
    private SeededEmaStep _averageAccumulation;
    private SeededEmaStep _averageVolume;
    private double _previousClose;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">The smoothing period; at least 2.</param>
    internal TwiggsMoneyFlowStep(int period)
    {
        double alpha = 1.0 / period;
        _averageAccumulation = new SeededEmaStep(alpha);
        _averageVolume = new SeededEmaStep(alpha);
    }

    /// <summary>Takes one bar and returns the money flow in [-1, 1], or NaN for the first bar.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    /// <param name="volume">The volume of the bar.</param>
    internal double Next(double high, double low, double close, double volume)
    {
        _count++;
        if (_count == 1)
        {
            _previousClose = close;
            return double.NaN;
        }

        double pc = _previousClose;
        _previousClose = close;
        double th = Math.Max(high, pc);
        double tl = Math.Min(low, pc);
        double tr = th - tl;
        double accumulation = tr > 0 ? (2.0 * close - th - tl) / tr * volume : 0.0;

        double averageAccumulation = _averageAccumulation.Next(accumulation);
        double averageVolume = _averageVolume.Next(volume);
        return averageVolume > 0 ? averageAccumulation / averageVolume : 0.0;
    }
}
