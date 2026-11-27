// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Richard Arms' Ease of Movement (EMV) — measures how easily price moves a given
/// distance relative to volume. Positive = rises on light volume (accumulation), negative =
/// falls on light volume (distribution). Reference: Arms, R. W. (1975),
/// <i>Volume Cycles in the Stock Market</i>.</summary>
public sealed class EaseOfMovement : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 14;

    /// <summary>The <c>scale</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultScale = 1_000_000;

    private readonly int _period;
    private readonly double _scale;

    /// <summary>Creates a new Ease of Movement indicator.</summary>
    /// <param name="bars">High, Low and Volume; built via <see cref="OhlcvBars.Hlv"/>.</param>
    /// <param name="period">SMA smoothing period for the raw EMV. Default <see cref="DefaultPeriod"/>. Must be ≥ 2.</param>
    /// <param name="scale">Volume-scale divisor — normalises volume to price-move magnitude.
    /// Default 10^6 (suits crypto / intraday). Must be &gt; 0.</param>
    public EaseOfMovement(OhlcvBars bars, int period = DefaultPeriod, double scale = DefaultScale)
        : base(bars, Spec(period, scale))
    {
        _period = period;
        _scale = scale;
    }

    internal static IndicatorSpec Spec(int period, double scale)
    {
        period.ThrowIfBelow(2, nameof(period));
        scale.ThrowIfNotAbove(0, nameof(scale));
        return IndicatorSpec.OfWindow("EMV", period, period);
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        // The one-period EMV of each bar after the first, rolled into a mean over _period.
        return RollingMeanOfPairs(_period, i =>
        {
            double mid = (High[i] + Low[i]) / 2.0;
            double prevMid = (High[i - 1] + Low[i - 1]) / 2.0;
            double move = mid - prevMid;
            double range = High[i] - Low[i];
            // BoxRatio = (Volume / scale) / range. Guard: zero volume or zero range → 0.
            if (Volume[i] == 0 || range == 0) return 0;
            double boxRatio = (Volume[i] / _scale) / range;
            return move / boxRatio;
        });
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
