// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of an exponential average that starts at the first value (<c>adjust=False</c>): the first output is the first
/// input, and each output after it is <c>alpha</c> times the input plus <c>1 - alpha</c> times the output before. It is the
/// average inside the force index, Twiggs money flow and the Klinger oscillator, which differ only in <c>alpha</c>; the
/// <see cref="EmaStep"/> is the other kind, started with the mean of the first <c>period</c> values.</summary>
internal struct SeededEmaStep
{
    private readonly double _alpha;
    private readonly double _oneMinusAlpha;
    private double _value;
    private bool _seeded;

    /// <summary>A step with the given weight.</summary>
    /// <param name="alpha">The weight of a new input, in (0, 1].</param>
    internal SeededEmaStep(double alpha)
    {
        _alpha = alpha;
        _oneMinusAlpha = 1.0 - alpha;
    }

    /// <summary>Takes one input and returns the average so far.</summary>
    /// <param name="value">The input.</param>
    internal double Next(double value)
    {
        if (_seeded)
        {
            _value = _alpha * value + _oneMinusAlpha * _value;
        }
        else
        {
            _seeded = true;
            _value = value;
        }

        return _value;
    }
}
