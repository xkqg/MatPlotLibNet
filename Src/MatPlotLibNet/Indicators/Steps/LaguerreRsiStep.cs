// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the Laguerre RSI: the recursion that the batch <see cref="LaguerreRsi"/> folds over its prices and
/// <see cref="Streaming.StreamingLaguerreRsi"/> takes one price at a time. The four Laguerre stages all start at the first price;
/// from the second price each stage follows the one before it, and the value is the share of the stage-to-stage rises in all the
/// stage-to-stage moves (0 when nothing moved). Before the second price the step says NaN.</summary>
internal struct LaguerreRsiStep
{
    private readonly double _alpha;
    private readonly double _oneMinusAlpha;
    private double _l0, _l1, _l2, _l3;
    private int _count;

    /// <summary>A step with the given smoothing.</summary>
    /// <param name="alpha">The smoothing factor, in (0, 1).</param>
    internal LaguerreRsiStep(double alpha)
    {
        _alpha = alpha;
        _oneMinusAlpha = 1 - alpha;
    }

    /// <summary>Takes one price and returns the index in [0, 1], or NaN for the first price.</summary>
    /// <param name="price">The price.</param>
    internal double Next(double price)
    {
        if (_count++ == 0)
        {
            _l0 = _l1 = _l2 = _l3 = price;
            return double.NaN;
        }

        double prevL0 = _l0, prevL1 = _l1, prevL2 = _l2, prevL3 = _l3;
        _l0 = _alpha * price + _oneMinusAlpha * prevL0;
        _l1 = -_oneMinusAlpha * _l0 + prevL0 + _oneMinusAlpha * prevL1;
        _l2 = -_oneMinusAlpha * _l1 + prevL1 + _oneMinusAlpha * prevL2;
        _l3 = -_oneMinusAlpha * _l2 + prevL2 + _oneMinusAlpha * prevL3;

        double cu = 0, cd = 0;
        if (_l0 >= _l1) cu += _l0 - _l1; else cd += _l1 - _l0;
        if (_l1 >= _l2) cu += _l1 - _l2; else cd += _l2 - _l1;
        if (_l2 >= _l3) cu += _l2 - _l3; else cd += _l3 - _l2;
        double sum = cu + cd;
        return sum > 0 ? cu / sum : 0;
    }
}
