// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the Ehlers adaptive stochastic: the recursion that the batch <see cref="AdaptiveStochastic"/> folds over its
/// bars and <see cref="Streaming.StreamingAdaptiveStochastic"/> takes one bar at a time. The look-back of the stochastic is half the
/// dominant cycle of the typical price (found by the <see cref="HilbertStep"/>), at least 6 bars; the raw value is where the close
/// sits between the lowest low and the highest high of that look-back (50 when they are equal), and with a smoothing period of 2 or
/// more it passes through a <see cref="SuperSmootherStep"/>. The first six bars only feed the discriminator, so the step says NaN for them.</summary>
internal struct AdaptiveStochasticStep : IHlcStep<double>
{
    private const int MinCycleBars = 6;
    private const int History = 50;

    private readonly int _smoothingPeriod;
    private HilbertStep _hilbert = new();
    private SuperSmootherStep _smoother;
    private InputWindow<double> _highs = new(History);
    private InputWindow<double> _lows = new(History);
    private int _count;

    /// <summary>A step with the given smoothing.</summary>
    /// <param name="smoothingPeriod">The period of the super smoother; below 2 leaves the raw value.</param>
    internal AdaptiveStochasticStep(int smoothingPeriod)
    {
        _smoothingPeriod = smoothingPeriod;
        _smoother = smoothingPeriod < 2 ? default : new SuperSmootherStep(smoothingPeriod);
    }

    /// <summary>Takes one bar and returns the adaptive stochastic in [0, 100], or NaN before the seventh bar.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    public double Next(double high, double low, double close)
    {
        int t = _count++;
        var row = _hilbert.Next(PriceSources.TypicalPrice(high, low, close));
        _highs.Push(high);
        _lows.Push(low);
        if (t < 6) return double.NaN;

        int cycleBars = (int)Math.Round(row.Period / 2.0);
        if (cycleBars < MinCycleBars) cycleBars = MinCycleBars;
        int oldest = t - Math.Max(0, t - cycleBars + 1);   // how many bars back the look-back starts
        double hh = _highs.Back(oldest), ll = _lows.Back(oldest);
        for (int back = oldest - 1; back >= 0; back--)
        {
            if (_highs.Back(back) > hh) hh = _highs.Back(back);
            if (_lows.Back(back) < ll) ll = _lows.Back(back);
        }

        double range = hh - ll;
        double raw = range == 0 ? 50.0 : 100.0 * (close - ll) / range;
        return _smoothingPeriod < 2 ? raw : _smoother.Next(raw);
    }
}
