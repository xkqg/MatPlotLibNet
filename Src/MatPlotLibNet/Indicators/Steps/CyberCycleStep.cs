// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the Ehlers cyber cycle: the recursion that the batch <see cref="CyberCycle"/> folds over its prices and
/// <see cref="Streaming.StreamingCyberCycle"/> takes one price at a time. The price is smoothed by a four-bar weighted mean (the
/// first three bars pass through), the cycle starts at 0 and from the fourth bar is a second difference of the smoothed price
/// plus the two cycle values before it, weighted. Before the fourth bar the step says NaN.</summary>
internal struct CyberCycleStep
{
    private readonly double _squareHalf;
    private readonly double _twoOneMinusAlpha;
    private readonly double _squareOneMinusAlpha;
    private double _price1, _price2, _price3;
    private double _smooth1, _smooth2;
    private double _cycle1, _cycle2;
    private int _count;

    /// <summary>A step with the given smoothing.</summary>
    /// <param name="alpha">The smoothing factor, in (0, 1).</param>
    internal CyberCycleStep(double alpha)
    {
        double oneMinusHalfAlpha = 1 - 0.5 * alpha;
        _squareHalf = oneMinusHalfAlpha * oneMinusHalfAlpha;
        _twoOneMinusAlpha = 2 * (1 - alpha);
        _squareOneMinusAlpha = (1 - alpha) * (1 - alpha);
    }

    /// <summary>Takes one price and returns the cycle, or NaN before the fourth price.</summary>
    /// <param name="price">The price.</param>
    internal double Next(double price)
    {
        int i = _count++;
        double smooth = i >= 3
            ? (price + 2 * _price1 + 2 * _price2 + _price3) / 6.0
            : price;
        double cycle = i >= 3
            ? _squareHalf * (smooth - 2 * _smooth1 + _smooth2)
              + _twoOneMinusAlpha * _cycle1
              - _squareOneMinusAlpha * _cycle2
            : 0;
        _price3 = _price2;
        _price2 = _price1;
        _price1 = price;
        _smooth2 = _smooth1;
        _smooth1 = smooth;
        _cycle2 = _cycle1;
        _cycle1 = cycle;
        return i >= 3 ? cycle : double.NaN;
    }
}
