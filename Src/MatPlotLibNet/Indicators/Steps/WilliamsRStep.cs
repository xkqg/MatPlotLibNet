// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of Williams %R: the recursion that the batch <see cref="WilliamsR"/> folds over its bars and
/// <see cref="Streaming.StreamingWilliamsR"/> takes one bar at a time, so the formula exists once. It places the close in the range of
/// the last <c>period</c> highs and lows (<see cref="RollingExtremeStep{TKeep}"/>), from 0 at the highest high to -100 at the lowest
/// low, and says -50 for a range of none. Before <c>period</c> bars have come the step says NaN.</summary>
internal struct WilliamsRStep : IHlcStep<double>
{
    private readonly int _period;
    private RollingExtremeStep<KeepHighest> _highest;
    private RollingExtremeStep<KeepLowest> _lowest;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">How many bars the range looks back over; at least 1.</param>
    internal WilliamsRStep(int period)
    {
        _period = period;
        _highest = new RollingExtremeStep<KeepHighest>(period);
        _lowest = new RollingExtremeStep<KeepLowest>(period);
    }

    /// <summary>Takes one bar and returns %R, or NaN before <c>period</c> bars have come.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    /// <returns>%R of the bar.</returns>
    public double Next(double high, double low, double close)
    {
        _count++;
        double highest = _highest.Next(high);
        double lowest = _lowest.Next(low);
        if (_count < _period) return double.NaN;

        double range = highest - lowest;
        return range == 0 ? -50.0 : (highest - close) / range * -100.0;
    }
}
