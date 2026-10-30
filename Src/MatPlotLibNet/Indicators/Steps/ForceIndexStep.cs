// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the force index: the recursion that the batch <see cref="ForceIndex"/> folds over its bars and
/// <see cref="Streaming.StreamingForceIndex"/> takes one bar at a time. The raw force of a bar is its volume times the change of the
/// close from the bar before; with a period above 1 it is smoothed by an exponential average seeded with the first raw force.
/// The first bar has no bar before it, so the step says NaN for it.</summary>
internal struct ForceIndexStep
{
    private readonly int _period;
    private SeededEmaStep _average;
    private double _previousClose;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">The smoothing period; 1 leaves the raw force.</param>
    internal ForceIndexStep(int period)
    {
        _period = period;
        _average = new SeededEmaStep(2.0 / (period + 1));
    }

    /// <summary>Takes one bar and returns the force index, or NaN for the first bar.</summary>
    /// <param name="close">The close of the bar.</param>
    /// <param name="volume">The volume of the bar.</param>
    internal double Next(double close, double volume)
    {
        _count++;
        if (_count == 1)
        {
            _previousClose = close;
            return double.NaN;
        }

        double raw = volume * (close - _previousClose);
        _previousClose = close;
        if (_period == 1) return raw;

        return _average.Next(raw);
    }
}
