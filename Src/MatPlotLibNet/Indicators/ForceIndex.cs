// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Alexander Elder's Force Index — volume-weighted price change, optionally
/// EMA-smoothed. Positive = buyers in control, negative = sellers. Reference: Elder (1993),
/// <i>Trading for a Living</i>, §7.</summary>
public sealed class ForceIndex : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 13;

    private readonly int _period;

    /// <summary>Creates a new Force Index indicator.</summary>
    /// <param name="bars">Close and Volume; built via <see cref="OhlcvBars.Cv"/>.</param>
    /// <param name="period">EMA smoothing period (default <see cref="DefaultPeriod"/>). Must be ≥ 1. Period 1 = no smoothing.</param>
    public ForceIndex(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("Force", period, 1, 1);

    /// <inheritdoc />
    public override SignalResult Compute() => FoldCloseVolume(new ForceIndexStep(_period));

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
