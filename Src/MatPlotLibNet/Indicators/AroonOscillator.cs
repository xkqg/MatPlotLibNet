// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Chande's Aroon Oscillator — trend-freshness gauge. Counts bars since the highest
/// high (Aroon Up) and lowest low (Aroon Down) over a rolling window; the oscillator is
/// <c>Up − Down</c> in [−100, +100]. Reference: Chande (1995), S&amp;C 13(9).</summary>
public sealed class AroonOscillator : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Aroon Oscillator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices (must satisfy <c>L ≤ H</c>).</param>
    /// <param name="period">Lookback window. Default 25. Must be ≥ 2.</param>
    public AroonOscillator(double[] high, double[] low, int period = 25)
        : base([], high, low, high, [])
    {
        if (high.Length != low.Length)
            throw new ArgumentException(
                $"high ({high.Length}) and low ({low.Length}) must have equal length.", nameof(low));
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        for (int i = 0; i < high.Length; i++)
        {
            if (high[i] < low[i])
                throw new ArgumentException(
                    $"AroonOscillator requires H >= L; bar {i} has H={high[i]} < L={low[i]}.");
        }
        _period = period;
        Label = $"Aroon({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n <= _period) return Array.Empty<double>();

        int outLen = n - _period;
        var result = new double[outLen];

        for (int w = 0; w < outLen; w++)
        {
            int end = _period + w;       // inclusive; "today" of this output
            int start = end - _period;   // inclusive; oldest bar in window

            // Tie-breaking: most recent bar wins → iterate end→start and ">" (strict).
            int highIdx = end;
            int lowIdx = end;
            double hh = High[end];
            double ll = Low[end];
            for (int i = end - 1; i >= start; i--)
            {
                if (High[i] > hh) { hh = High[i]; highIdx = i; }
                if (Low[i] < ll)  { ll = Low[i];  lowIdx  = i; }
            }

            int barsSinceHigh = end - highIdx;
            int barsSinceLow = end - lowIdx;

            double up = 100.0 * (_period - barsSinceHigh) / _period;
            double down = 100.0 * (_period - barsSinceLow) / _period;
            result[w] = up - down;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: _period);
        axes.YAxis.Min = -100;
        axes.YAxis.Max = 100;
    }
}
