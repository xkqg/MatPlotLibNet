// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Colin Twiggs' Money Flow indicator — the true-range-based refinement of Chaikin
/// Money Flow. Output bounded in <c>[-1, 1]</c>: positive = accumulation, negative =
/// distribution. Uses Wilder-style smoothing (<c>α = 1/period</c>) on both the accumulation-
/// distribution numerator and the volume denominator. Reference: Twiggs, C. (2002–2004),
/// <i>Twiggs Money Flow</i>, Incredible Charts documentation.</summary>
public sealed class TwiggsMoneyFlow : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 21;

    private readonly int _period;

    /// <summary>Creates a new Twiggs Money Flow indicator.</summary>
    /// <param name="bars">High, Low, Close and Volume; built via <see cref="OhlcvBars.Hlcv"/>.</param>
    /// <param name="period">Wilder smoothing period. Default <see cref="DefaultPeriod"/>. Must be ≥ 2.</param>
    public TwiggsMoneyFlow(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("TMF", period, 2, 1);

    /// <inheritdoc />
    public override SignalResult Compute() => FoldHlcv(new TwiggsMoneyFlowStep(_period));

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingTwiggsMoneyFlow"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Signed;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
