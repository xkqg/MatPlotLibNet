// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Colin Twiggs' Money Flow indicator — the true-range-based refinement of Chaikin
/// Money Flow. Output bounded in <c>[-1, 1]</c>: positive = accumulation, negative =
/// distribution. Uses Wilder-style smoothing (<c>α = 1/period</c>) on both the accumulation-
/// distribution numerator and the volume denominator. Reference: Twiggs, C. (2002–2004),
/// <i>Twiggs Money Flow</i>, Incredible Charts documentation.</summary>
public sealed class TwiggsMoneyFlow : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Twiggs Money Flow indicator.</summary>
    /// <param name="bars">High, Low, Close and Volume; built via <see cref="OhlcvBars.Hlcv"/>.</param>
    /// <param name="period">Wilder smoothing period. Default 21. Must be ≥ 2.</param>
    public TwiggsMoneyFlow(OhlcvBars bars, int period = 21)
        : base(bars)
    {
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        _period = period;
        Label = $"TMF({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < 2) return Array.Empty<double>();

        var result = new double[n - 1];
        var step = new TwiggsMoneyFlowStep(_period);
        for (int i = 0; i < n; i++)
        {
            double value = step.Next(High[i], Low[i], Close[i], Volume[i]);
            if (i >= 1) result[i - 1] = value;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 1);
        axes.YAxis.Min = -1;
        axes.YAxis.Max = 1;
    }
}
