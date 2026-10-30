// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the Ehlers two-pole high-pass filter: the recursion that <c>HighPass</c> folds over a series and the streaming
/// forms of the decycler and the roofing filter take one value at a time. The first two outputs are 0, so a constant input settles to
/// 0; after that each output is <c>coefX</c> times the second difference of the input plus the two outputs before it, weighted.</summary>
internal struct HighPassStep
{
    private readonly double _coefX;
    private readonly double _coefHp1;
    private readonly double _coefHp2;
    private double _input1;
    private double _input2;
    private double _output1;
    private double _output2;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">The cut-off period in bars; at least 2.</param>
    internal HighPassStep(int period)
    {
        double theta = 0.707 * 360.0 / period * Math.PI / 180.0;
        double cosT = Math.Cos(theta);
        double sinT = Math.Sin(theta);
        double alpha = (cosT + sinT - 1) / cosT;
        double omA = 1 - alpha;
        _coefX = (1 - alpha / 2) * (1 - alpha / 2);
        _coefHp1 = 2 * omA;
        _coefHp2 = -(omA * omA);
    }

    /// <summary>Takes one value and returns the high-passed value.</summary>
    /// <param name="value">The value.</param>
    internal double Next(double value)
    {
        double output = _count < 2
            ? 0
            : _coefX * (value - 2 * _input1 + _input2) + _coefHp1 * _output1 + _coefHp2 * _output2;
        _count++;
        _input2 = _input1;
        _input1 = value;
        _output2 = _output1;
        _output1 = output;
        return output;
    }
}
