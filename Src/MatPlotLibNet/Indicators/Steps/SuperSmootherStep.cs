// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the Ehlers two-pole super smoother: the recursion that <c>SuperSmooth</c> folds over a series and the streaming
/// forms of the super smoother, the roofing filter and the adaptive stochastic take one value at a time. The first two values pass
/// through; after that each output is <c>c1</c> times the mean of the last two inputs, plus <c>c2</c> times the output before and
/// <c>c3</c> times the one before that.</summary>
internal struct SuperSmootherStep
{
    private readonly double _c1;
    private readonly double _c2;
    private readonly double _c3;
    private double _input1;
    private double _output1;
    private double _output2;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">The cut-off period in bars; at least 2.</param>
    internal SuperSmootherStep(int period)
    {
        double a1 = Math.Exp(-1.414 * Math.PI / period);
        double b1 = 2 * a1 * Math.Cos(1.414 * 180.0 / period * Math.PI / 180.0);
        _c2 = b1;
        _c3 = -a1 * a1;
        _c1 = 1 - _c2 - _c3;
    }

    /// <summary>Takes one value and returns the smoothed value.</summary>
    /// <param name="value">The value.</param>
    internal double Next(double value)
    {
        double output = _count < 2
            ? value
            : _c1 * (value + _input1) / 2 + _c2 * _output1 + _c3 * _output2;
        _count++;
        _input1 = value;
        _output2 = _output1;
        _output1 = output;
        return output;
    }
}
