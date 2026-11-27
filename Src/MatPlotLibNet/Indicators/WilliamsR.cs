// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Williams %R momentum indicator. Measures overbought/oversold conditions on a -100 to 0 scale.</summary>
/// <remarks>Values near 0 indicate overbought conditions; values near -100 indicate oversold.
/// Level lines at -20 (overbought) and -80 (oversold) are added automatically; <see cref="Levels"/> recolours, restyles or removes
/// them. Best placed in a separate subplot.</remarks>
public sealed class WilliamsR : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 14;

    private readonly int _period;

    /// <summary>The level lines at -20 (overbought) and -80 (oversold) drawn beside the signal: dashed and in the theme's neutral
    /// colour until changed.</summary>
    public IndicatorLevels Levels { get; } = new(-20, -80);

    /// <summary>Creates a new Williams %R indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">Lookback period (default <see cref="DefaultPeriod"/>).</param>
    public WilliamsR(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfWindow("%R", period, period - 1);

    /// <inheritdoc />
    public override SignalResult Compute() => FoldHlc(new WilliamsRStep(_period));

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingWilliamsR"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = new(-100, 0);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        Pane.Pin(axes);
        double[] wr = Compute();
        if (wr.Length == 0) return;
        PlotLine(axes, wr);
        Levels.Apply(axes);
    }
}
