// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Alexander Elder's Force Index — volume-weighted price change, optionally
/// EMA-smoothed. Positive = buyers in control, negative = sellers. Reference: Elder (1993),
/// <i>Trading for a Living</i>, §7.</summary>
public sealed class ForceIndex : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Force Index indicator.</summary>
    /// <param name="bars">Close and Volume; built via <see cref="OhlcvBars.Cv"/>.</param>
    /// <param name="period">EMA smoothing period (default 13). Must be ≥ 1. Period 1 = no smoothing.</param>
    public ForceIndex(OhlcvBars bars, int period = 13)
        : base(bars)
    {
        if (period < 1)
            throw new ArgumentException($"period must be >= 1 (got {period}).", nameof(period));
        _period = period;
        Label = $"Force({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < 2) return Array.Empty<double>();
        var result = new double[n - 1];
        var step = new ForceIndexStep(_period);
        for (int i = 0; i < n; i++)
        {
            double value = step.Next(Close[i], Volume[i]);
            if (i >= 1) result[i - 1] = value;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 1);
}
