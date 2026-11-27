// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Commodity Channel Index indicator. Measures how far price has deviated from its statistical mean.</summary>
/// <remarks>Values above +100 indicate overbought conditions; below -100 indicate oversold.
/// Level lines at +100 and -100 are added automatically; <see cref="Levels"/> recolours, restyles or removes them.
/// Best placed in a separate subplot.</remarks>
public sealed class Cci : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    private readonly int _period;

    /// <summary>The level lines at +100 (overbought) and -100 (oversold) drawn beside the signal: dashed and in the theme's neutral colour
    /// until changed.</summary>
    public IndicatorLevels Levels { get; } = new(100, -100);

    /// <summary>Creates a new CCI indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">Lookback period (default <see cref="DefaultPeriod"/>).</param>
    public Cci(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfWindow("CCI", period, period - 1);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int len = ValueCount;
        var tp = ComputeTypicalPrice();

        double[] sma = tp.RollingMean(_period);  // length = len

        var result = new double[len];
        for (int i = 0; i < len; i++)
            result[i] = tp.AsSpan(i, _period).ChannelIndex(sma[i], tp[i + _period - 1]);
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        double[] cci = Compute();
        if (cci.Length == 0) return;
        PlotLine(axes, cci);
        Levels.Apply(axes);
    }
}
