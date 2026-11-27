// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The two lines of the stochastic oscillator at one bar.</summary>
/// <param name="K">The %K line: the raw %K of the last <c>k</c> bars, or its mean over the last <c>smoothK</c> bars when it is smoothed;
/// NaN before it has a value.</param>
/// <param name="D">The %D line: the mean of the last <c>d</c> values of %K; NaN before it has a value.</param>
internal readonly record struct StochasticPoint(double K, double D);

/// <summary>One step of the stochastic oscillator: the recursion that the batch <see cref="Stochastic"/> folds over its bars and
/// <see cref="Streaming.StreamingStochastic"/> takes one bar at a time, so the formula exists once. The raw %K places the close in the
/// range of the last <c>k</c> highs and lows, from 0 at the lowest low to 100 at the highest high, and says 50 for a range of none;
/// the highs and lows are read through <see cref="RollingExtremeStep{TKeep}"/>. %K is the raw one smoothed by a simple mean over
/// <c>smoothK</c> values (the slow stochastic; one leaves it raw, the fast stochastic) and %D is a simple mean of that
/// (<see cref="RollingMeanStep"/>), so the first %K comes with bar <c>k + smoothK - 1</c> and the first %D <c>d - 1</c> bars after it.</summary>
internal struct StochasticStep
{
    private readonly int _kPeriod;
    private readonly bool _smooths;
    private RollingExtremeStep<KeepHighest> _highest;
    private RollingExtremeStep<KeepLowest> _lowest;
    private RollingMeanStep _smoothK;
    private RollingMeanStep _d;
    private int _count;

    /// <summary>A step over the given periods.</summary>
    /// <param name="kPeriod">How many bars the raw %K looks back over; at least 1.</param>
    /// <param name="smoothK">How many raw values %K is the mean of; 1 leaves %K raw.</param>
    /// <param name="dPeriod">How many values of %K %D is the mean of; at least 1.</param>
    internal StochasticStep(int kPeriod, int smoothK, int dPeriod)
    {
        _kPeriod = kPeriod;
        _smooths = smoothK > 1;
        _highest = new RollingExtremeStep<KeepHighest>(kPeriod);
        _lowest = new RollingExtremeStep<KeepLowest>(kPeriod);
        if (_smooths) _smoothK = new RollingMeanStep(smoothK);
        _d = new RollingMeanStep(dPeriod);
    }

    /// <summary>The raw %K: where the close stands between the lowest low and the highest high of the last <c>k</c> bars, from 0 to 100, and 50 when
    /// the range is none. The one formula: the step takes the bars one at a time and the batch <see cref="Stochastic"/> takes the
    /// extremes of every window in one pass first, because the pass over all the bars at once is three times as cheap as this step
    /// run over them (measured), and both read it here.</summary>
    /// <param name="close">The close of the bar.</param>
    /// <param name="lowest">The lowest low of the last <c>k</c> bars.</param>
    /// <param name="highest">The highest high of the last <c>k</c> bars.</param>
    /// <returns>The raw %K.</returns>
    internal static double RawK(double close, double lowest, double highest)
    {
        double range = highest - lowest;
        return range > 0 ? (close - lowest) / range * 100 : 50;
    }

    /// <summary>Takes one bar and returns the two lines, NaN while they have no value yet.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    /// <returns>%K and %D of the bar.</returns>
    internal StochasticPoint Next(double high, double low, double close)
    {
        _count++;
        double highest = _highest.Next(high);
        double lowest = _lowest.Next(low);
        if (_count < _kPeriod) return new StochasticPoint(double.NaN, double.NaN);

        double raw = RawK(close, lowest, highest);
        double k = _smooths ? _smoothK.Next(raw) : raw;
        return double.IsNaN(k) ? new StochasticPoint(double.NaN, double.NaN) : new StochasticPoint(k, _d.Next(k));
    }
}
