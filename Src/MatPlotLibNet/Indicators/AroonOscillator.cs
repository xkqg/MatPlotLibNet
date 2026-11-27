// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Chande's Aroon Oscillator — trend-freshness gauge. Counts bars since the highest
/// high (Aroon Up) and lowest low (Aroon Down) over a rolling window; the oscillator is
/// <c>Up − Down</c> in [−100, +100]. Reference: Chande (1995), S&amp;C 13(9).</summary>
public sealed class AroonOscillator : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 25;

    private readonly int _period;

    /// <summary>Creates a new Aroon Oscillator.</summary>
    /// <param name="bars">High and Low (must satisfy <c>L ≤ H</c>); built via <see cref="OhlcvBars.Hl"/>.</param>
    /// <param name="period">Lookback window. Default <see cref="DefaultPeriod"/>. Must be ≥ 2.</param>
    public AroonOscillator(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        bars.ThrowIfHighBelowLow(nameof(AroonOscillator));
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("Aroon", period, 2, period);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int outLen = ValueCount;
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

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingAroonOscillator"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = new(-100, 100);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
