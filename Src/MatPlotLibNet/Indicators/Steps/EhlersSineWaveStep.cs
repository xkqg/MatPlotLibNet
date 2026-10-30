// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The three outputs of the Ehlers sine wave at one bar.</summary>
/// <param name="SineWave">The sine of the phase of the dominant cycle.</param>
/// <param name="LeadSine">The sine of the phase 45 degrees ahead.</param>
/// <param name="IsCyclic">Whether the phase moves at about the rate the cycle period says (the market is in a cycle), and not otherwise (a trend).</param>
internal readonly record struct SineWavePoint(double SineWave, double LeadSine, bool IsCyclic);

/// <summary>One step of the Ehlers sine wave: the recursion that the batch <see cref="EhlersSineWave"/> folds over its prices and
/// <see cref="Streaming.StreamingEhlersSineWave"/> takes one price at a time. From the seventh bar (the first six only feed the
/// <see cref="HilbertStep"/>) the sine wave is read off the phase of the dominant cycle, and the bar is in a cycle when the phase moves
/// within 50 degrees of 360 over the period. Before the seventh bar the step says NaN and not cyclic.</summary>
internal struct EhlersSineWaveStep
{
    private const double CyclicToleranceDegrees = 50.0;
    private const double FortyFiveRad = 45.0 * Math.PI / 180.0;

    private HilbertStep _hilbert = new();
    private double _previousPhase;
    private int _count;

    /// <summary>A step before any price.</summary>
    public EhlersSineWaveStep()
    {
    }

    /// <summary>Takes one price and returns the sine wave, or NaN before the seventh price.</summary>
    /// <param name="price">The price.</param>
    internal SineWavePoint Next(double price)
    {
        int i = _count++;
        var row = _hilbert.Next(price);
        double previousPhase = _previousPhase;
        _previousPhase = row.Phase;
        if (i < 6) return new SineWavePoint(double.NaN, double.NaN, false);

        double phaseRad = row.Phase * Math.PI / 180.0;
        double expectedRate = 360.0 / row.Period;
        double actualRate = Math.Abs(row.Phase - previousPhase);
        return new SineWavePoint(
            Math.Sin(phaseRad),
            Math.Sin(phaseRad + FortyFiveRad),
            Math.Abs(actualRate - expectedRate) < CyclicToleranceDegrees);
    }
}
