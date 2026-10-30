// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The two lines of MAMA at one bar.</summary>
/// <param name="Mama">The MESA adaptive moving average.</param>
/// <param name="Fama">The following adaptive moving average.</param>
internal readonly record struct MamaFamaPoint(double Mama, double Fama);

/// <summary>One step of the MESA adaptive moving average: the recursion that the batch <see cref="MamaFama"/> folds over its prices
/// and <see cref="Streaming.StreamingMamaFama"/> takes one price at a time. The first six bars (which the <see cref="HilbertStep"/>
/// only feeds) carry the price; from the seventh each average moves toward the price by a rate that falls with the change of phase
/// between the last two bars, between the slow and the fast limit. Before the seventh bar the step says NaN.</summary>
internal struct MamaFamaStep
{
    private readonly double _fastLimit;
    private readonly double _slowLimit;
    private HilbertStep _hilbert = new();
    private double _mama;
    private double _fama;
    private double _previousPhase;
    private int _count;

    /// <summary>A step with the given limits.</summary>
    /// <param name="fastLimit">The fastest rate, in (0, 1].</param>
    /// <param name="slowLimit">The slowest rate, above 0 and below the fast limit.</param>
    internal MamaFamaStep(double fastLimit, double slowLimit)
    {
        _fastLimit = fastLimit;
        _slowLimit = slowLimit;
    }

    /// <summary>Takes one price and returns both lines, or NaN before the seventh price.</summary>
    /// <param name="price">The price.</param>
    internal MamaFamaPoint Next(double price)
    {
        int i = _count++;
        var row = _hilbert.Next(price);
        double previousPhase = _previousPhase;
        _previousPhase = row.Phase;
        if (i < 6)
        {
            _mama = price;
            _fama = price;
            return new MamaFamaPoint(double.NaN, double.NaN);
        }

        double deltaPhase = previousPhase - row.Phase;
        if (deltaPhase < 1) deltaPhase = 1;
        double alpha = _fastLimit / deltaPhase;
        if (alpha < _slowLimit) alpha = _slowLimit;
        _mama = alpha * price + (1 - alpha) * _mama;
        _fama = 0.5 * alpha * _mama + (1 - 0.5 * alpha) * _fama;
        return new MamaFamaPoint(_mama, _fama);
    }
}
